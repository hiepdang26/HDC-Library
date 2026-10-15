using System;
using System.Collections.Generic;
using HDC.Ads.Domain;
using HDC.Ads.Ports;

namespace HDC.Ads.Logic
{
    internal sealed class HDCConsentGate : IAdsConsent
    {
        internal const float UpdateTimeoutSeconds = 10f;

        internal const string WaitingForConfigStep = "cs_wait_config";
        internal const string ConsentSkippedStep = "cs_skip";
        internal const string UpdateStartedStep = "cs_u_s";
        internal const string UpdateAnsweredStep = "cs_u_e";
        internal const string UpdateMissedStep = "cs_u_m";
        internal const string FormShownStep = "cs_f_s";
        internal const string FormClosedStep = "cs_f_e";
        internal const string ConsentAcceptedStep = "cs_accept";
        internal const string ConsentPartialStep = "cs_partial";
        internal const string ConsentDeniedStep = "cs_denied";
        internal const string AdsAllowedStep = "cs_ok";
        internal const string AdsRefusedStep = "cs_no";
        internal const string TrackingSkippedStep = "att_skip";
        internal const string TrackingKnownStep = "att_known";
        internal const string TrackingNotAllowedStep = "att_purpose_no";
        internal const string TrackingAskedStep = "att_s";
        internal const string TrackingAuthorizedStep = "att_auth";
        internal const string TrackingDeniedStep = "att_denied";
        internal const string TrackingRestrictedStep = "att_restricted";

        private static readonly int[] TrackingPurposes = { 1, 3, 4 };

        private readonly List<string> steps = new List<string>();

        private readonly IConsentProvider consent;
        private readonly ITrackingAuthorization tracking;
        private readonly IKeyValueStore store;
        private readonly IClock clock;
        private readonly IMainThread mainThread;
        private readonly IAdsLog log;
        private readonly Func<bool> testDevice;
        private Action whenDone;
        private float updateStartedAt;

        internal HDCConsentGate(IConsentProvider consent, ITrackingAuthorization tracking, IKeyValueStore store, IClock clock, IMainThread mainThread,
            IAdsLog log, Func<bool> testDevice)
        {
            this.consent = consent;
            this.tracking = tracking;
            this.store = store;
            this.clock = clock;
            this.mainThread = mainThread;
            this.log = log;
            this.testDevice = testDevice;
        }

        internal enum Step
        {
            NotStarted,
            WaitingForConfig,
            Updating,
            ShowingForm,
            AskingTracking,
            Done,
        }

        public bool IsDone => Current == Step.Done;

        public bool CanRequestAds { get; private set; }

        public HDCConsentStatus Status => ConsentSkipped ? HDCConsentStatus.Obtained : consent.Status;

        public HDCTrackingStatus TrackingStatus =>
            TogglesKnown && !Toggles.Att && tracking.Status != HDCTrackingStatus.Unsupported ? HDCTrackingStatus.Authorized : tracking.Status;

        public bool PrivacyOptionsRequired => consent.PrivacyOptionsRequired;

        public event Action Done;

        internal event Action<string> Stepped;

        internal IReadOnlyList<string> Steps => steps;

        internal Step Current { get; private set; }

        internal string LastError { get; private set; }

        internal bool DebugInEea { get; private set; }

        internal bool TogglesKnown { get; private set; }

        internal HDCPrivacyToggles Toggles { get; private set; } = new HDCPrivacyToggles(true, true, "not read yet");

        internal bool ConsentSkipped { get; private set; }

        public void ShowPrivacyOptions(Action<string> onClosed = null) =>
            consent.ShowPrivacyOptions(error =>
            {
                if (error != null)
                    log.Warning("consent: privacy options failed: " + error);
                CanRequestAds = IsDone && (ConsentSkipped || consent.CanRequestAds);
                HDCCallbacks.Run(() => onClosed?.Invoke(error), log);
            });

        internal void Start()
        {
            if (Current != Step.NotStarted)
                return;

            if (!TogglesKnown && store.TryGetInt(HDCAdNames.PrivacyTogglesKey, out int saved) && HDCPrivacyToggles.TryRead(saved, out HDCPrivacyToggles last))
                UseToggles(last);
            if (!TogglesKnown)
            {
                Current = Step.WaitingForConfig;
                log.Info("consent: first launch, waiting for ads_config to know toggleConsent and toggleAtt");
                Track(WaitingForConfigStep);
                return;
            }

            Begin();
        }

        internal void UseConfig(HDCAdsConfig config)
        {
            HDCPrivacyToggles current = HDCPrivacyToggles.Of(config ?? new HDCAdsConfig(), HDCPrivacyToggles.ThisSession);
            store.SetInt(HDCAdNames.PrivacyTogglesKey, current.Saved);
            store.Save();
            if (TogglesKnown)
            {
                if (!current.SameAs(Toggles))
                    log.Info($"consent: ads_config now has toggleConsent={current.Consent} toggleAtt={current.Att}; this launch already used {Toggles}, the new values apply from the next launch");
                return;
            }

            UseToggles(current);
            if (Current == Step.WaitingForConfig)
                Begin();
        }

        private void UseToggles(HDCPrivacyToggles toggles)
        {
            Toggles = toggles;
            TogglesKnown = true;
            log.Info("consent: " + toggles);
        }

        private void Begin()
        {
            if (!Toggles.Consent)
            {
                ConsentSkipped = true;
                log.Info("consent: not asked (ads_config.toggleConsent=false), counted as accepted");
                Track(ConsentSkippedStep);
                Track(ConsentAcceptedStep);
                AfterConsent();
                return;
            }

            Current = Step.Updating;
            DebugInEea = testDevice();
            log.Info("consent: asking the consent service" + (DebugInEea ? " as a test device in the EEA" : string.Empty));
            updateStartedAt = clock.RealTime;
            mainThread.Ticked += WatchUpdate;
            Track(UpdateStartedStep);
            consent.Update(DebugInEea, OnUpdated);
        }

        internal void WhenDone(Action callback)
        {
            if (callback == null)
                return;
            if (IsDone)
                HDCCallbacks.Run(callback, log);
            else
                whenDone += callback;
        }

        internal void Reset()
        {
            consent.Reset();
            log.Info("consent: reset; the consent questions come back at the next start");
        }

        internal static bool PurposesAllowTracking(string purposeConsents)
        {
            if (string.IsNullOrEmpty(purposeConsents))
                return true;
            foreach (int purpose in TrackingPurposes)
            {
                if (purposeConsents.Length < purpose || purposeConsents[purpose - 1] != '1')
                    return false;
            }

            return true;
        }

        private void WatchUpdate()
        {
            if (Current != Step.Updating)
            {
                mainThread.Ticked -= WatchUpdate;
                return;
            }

            if (clock.RealTime - updateStartedAt < UpdateTimeoutSeconds)
                return;
            mainThread.Ticked -= WatchUpdate;
            LastError = $"no answer from the consent service in {UpdateTimeoutSeconds:0} s";
            log.Warning("consent: " + LastError + "; the consent saved on this device decides");
            Track(UpdateMissedStep);
            AfterConsent();
        }

        private void OnUpdated(string error)
        {
            if (Current != Step.Updating)
                return;
            mainThread.Ticked -= WatchUpdate;
            if (error != null)
            {
                LastError = error;
                log.Warning("consent: update failed: " + error + "; the consent saved on this device decides");
                Track(UpdateMissedStep);
                AfterConsent();
                return;
            }

            Track(UpdateAnsweredStep);
            if (consent.CanRequestAds)
            {
                AfterConsent();
                return;
            }

            Current = Step.ShowingForm;
            log.Info("consent: showing the consent form");
            Track(FormShownStep);
            consent.ShowFormIfRequired(OnFormClosed);
        }

        private void OnFormClosed(string error)
        {
            if (Current != Step.ShowingForm)
                return;
            if (error != null)
            {
                LastError = error;
                log.Warning("consent: form failed: " + error);
            }

            Track(FormClosedStep);
            string answer = ConsentAnswerStep(consent.PurposeConsents);
            if (answer != null)
                Track(answer);
            AfterConsent();
        }

        private void AfterConsent()
        {
            CanRequestAds = ConsentSkipped || consent.CanRequestAds;
            log.Info(ConsentSkipped ? "consent: skipped, can request ads: True" : $"consent: {consent.Status}, can request ads: {CanRequestAds}");
            if (!CanRequestAds)
            {
                log.Warning("consent: ads may not be requested, so tracking is not asked and the ads stay off");
                Track(AdsRefusedStep);
                Finish();
                return;
            }

            Track(AdsAllowedStep);
            HDCTrackingStatus status = tracking.Status;
            if (status == HDCTrackingStatus.Unsupported)
            {
                Finish();
                return;
            }

            if (!Toggles.Att)
            {
                log.Info("tracking: not asked (ads_config.toggleAtt=false), counted as accepted");
                Track(TrackingSkippedStep);
                Track(TrackingAuthorizedStep);
                Finish();
                return;
            }

            if (status != HDCTrackingStatus.NotDetermined)
            {
                Track(TrackingKnownStep);
                Finish();
                return;
            }

            if (!ConsentSkipped && !PurposesAllowTracking(consent.PurposeConsents))
            {
                log.Info("tracking: not asked, the consent did not allow personalized ads");
                Track(TrackingNotAllowedStep);
                Finish();
                return;
            }

            Current = Step.AskingTracking;
            log.Info("tracking: asking for permission");
            Track(TrackingAskedStep);
            tracking.Request(OnTrackingAnswered);
        }

        private void OnTrackingAnswered(HDCTrackingStatus status)
        {
            if (Current != Step.AskingTracking)
                return;
            log.Info("tracking: " + status);
            string answer = AnswerStep(status);
            if (answer != null)
                Track(answer);
            Finish();
        }

        internal static string ConsentAnswerStep(string purposeConsents)
        {
            if (string.IsNullOrEmpty(purposeConsents))
                return null;
            if (purposeConsents[0] != '1')
                return ConsentDeniedStep;
            return PurposesAllowTracking(purposeConsents) ? ConsentAcceptedStep : ConsentPartialStep;
        }

        internal static string AnswerStep(HDCTrackingStatus status)
        {
            switch (status)
            {
                case HDCTrackingStatus.Authorized:
                    return TrackingAuthorizedStep;
                case HDCTrackingStatus.Denied:
                    return TrackingDeniedStep;
                case HDCTrackingStatus.Restricted:
                    return TrackingRestrictedStep;
                default:
                    return null;
            }
        }

        private void Track(string step)
        {
            steps.Add(step);
            Action<string> handlers = Stepped;
            if (handlers != null)
                HDCCallbacks.Run(() => handlers(step), log);
        }

        private void Finish()
        {
            Current = Step.Done;
            Action callbacks = whenDone;
            whenDone = null;
            if (callbacks != null)
            {
                foreach (Action callback in callbacks.GetInvocationList())
                    HDCCallbacks.Run(callback, log);
            }

            HDCCallbacks.Run(Done, log);
        }
    }
}
