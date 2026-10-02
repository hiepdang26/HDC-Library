using System;
using HDC.Ads.Domain;
using UnityEngine;

namespace HDC.Ads.Logic
{
    /// <summary>The app launch channel behind <see cref="IAppLaunchAds"/>.</summary>
    internal sealed partial class HDCAppLaunchAds : IAppLaunchAds, IAdChannel
    {
        // Completes anyway if the ad never reports closing.
        private const float CloseFallbackSeconds = 15f;
        private const float ReadyCheckInterval = 0.25f;

        private readonly HDCAdsContext context;
        private readonly HDCForceAds forceAds;
        private bool startRequested;
        private bool clockStarted;
        private float clockStart;
        private bool ticking;
        private bool showing;
        private float showStart;
        private float nextReadyCheck;
        private HDCFullscreenGroup group;

        internal HDCAppLaunchAds(HDCAdsContext context, HDCForceAds forceAds)
        {
            this.context = context;
            this.forceAds = forceAds;
        }

        /// <summary>Right before the launch ad shows, or right before completing without one.</summary>
        public event Action BeforeShow;

        /// <summary>The launch is done: the ad closed, failed, timed out, or could not show.</summary>
        public event Action Completed;

        public bool IgnoreAds { get; set; }

        public bool IsCompleted { get; private set; }

        public bool IsBeforeShowRaised { get; private set; }

        public bool CanShow => !IsDisabled && !IgnoreAds && clockStarted && !TimedOut && MinimumWaitPassed && (Group()?.IsReady ?? false);

        private HDCAdsConfig.AppLaunchChannel Channel => context.Config.appLaunchChannel ?? new HDCAdsConfig.AppLaunchChannel();

        private bool IsDisabled => !Channel.isEnabled || context.IsAdsRemoved;

        private float Elapsed => clockStarted ? context.Clock.UnscaledTime - clockStart : 0f;

        private float MinimumWait => Channel.minWaitSeconds > 0 ? Channel.minWaitSeconds : 5f;

        private float Timeout =>
            Channel.timeoutSeconds <= 0 || Channel.timeoutSeconds < MinimumWait ? MinimumWait + 5f : Channel.timeoutSeconds;

        private bool MinimumWaitPassed => clockStarted && Elapsed >= MinimumWait;

        private bool TimedOut => clockStarted && Elapsed >= Timeout;

        /// <summary>
        /// Starts the launch clock and loading, when the channel does not on its own (autoInit off). Called
        /// before the SDK is ready, it starts once the SDK is.
        /// </summary>
        public void Initialize()
        {
            if (!context.IsInitialized)
            {
                startRequested = true;
                return;
            }

            if (!Channel.autoInit)
                Start();
        }

        string IAdChannel.Key => "AL";

        string IAdChannel.Title => "AppLaunch";

        void IAdChannel.OnSdkInitialized()
        {
            if (Channel.autoInit || startRequested)
                Start();
        }

        void IAdChannel.InitializeAll() => Initialize();

        // The launch ad shows once, at launch: nothing stays on screen to hide.
        void IAdChannel.OnAdsRemoved()
        {
        }

        private void Start()
        {
            if (clockStarted)
                return;

            clockStarted = true;
            clockStart = context.Clock.UnscaledTime;
            if (!IsDisabled)
                Group()?.Initialize();
            if (!ticking)
            {
                ticking = true;
                context.MainThread.Ticked += Tick;
            }
        }

        private void Tick()
        {
            if (IsCompleted)
                return;

            if (showing)
            {
                if (context.Clock.UnscaledTime - showStart >= CloseFallbackSeconds)
                    Complete("the ad did not report closing");
                return;
            }

            // Nothing to wait for.
            if (IsDisabled || IgnoreAds || Group() == null || Group().IsEmpty)
            {
                Complete("no launch ad");
                return;
            }

            if (!MinimumWaitPassed)
                return;
            if (TimedOut)
            {
                Complete("timed out");
                return;
            }

            if (context.Clock.UnscaledTime < nextReadyCheck)
                return;
            nextReadyCheck = context.Clock.UnscaledTime + ReadyCheckInterval;
            if (!Group().IsReady)
                return;

            RaiseBeforeShow();
            showing = true;
            showStart = context.Clock.UnscaledTime;
            bool forceAd = context.CoreConfig.comebackChannel?.launchAdType == 0;
            bool shown = Group().Show(
                HDCAdChannel.AppLaunch,
                "",
                null,
                () =>
                {
                    if (forceAd)
                        forceAds.CountImpression("app_launch");
                },
                _ => Complete("closed"));
            if (!shown)
                Complete("show failed");
        }

        private void Complete(string reason)
        {
            if (IsCompleted)
                return;

            context.Log.Info("app launch complete: " + reason);
            RaiseBeforeShow();
            IsCompleted = true;
            showing = false;
            if (ticking)
            {
                ticking = false;
                context.MainThread.Ticked -= Tick;
            }

            HDCCallbacks.Run(Completed);
        }

        private void RaiseBeforeShow()
        {
            if (IsBeforeShowRaised)
                return;
            IsBeforeShowRaised = true;
            HDCCallbacks.Run(BeforeShow);
        }

        private HDCFullscreenGroup Group()
        {
            if (group != null)
                return group;

            HDCAdCoreConfig.Comeback comeback = context.CoreConfig.comebackChannel ?? new HDCAdCoreConfig.Comeback();
            group = comeback.launchAdType == 0 ? context.Groups.ForceAdGroup(comeback.launchForceAdGroupName) : context.Groups.AppOpenGroup();
            return group;
        }
    }
}
