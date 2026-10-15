using System.Collections.Generic;
using HDC.Ads.Domain;
using HDC.Ads.Logic;
using HDC.Ads.Ports;
using NUnit.Framework;

namespace HDC.Ads.Tests
{
    public class HDCConsentTests
    {
        private const string Core = @"{ ""appResumeChannel"": { ""isEnabled"": true, ""resumeAdType"": 0,
    ""forceAdUnit"": { ""admobUnit"": { ""id"": ""resume-inter"" } } } }";

        [Test]
        public void TheSdkStartsOnlyOnceTheConsentServiceAnswers()
        {
            var ads = new HDCFakeAds();
            ads.Consent.AnswersAtOnce = false;
            bool initialized = false;

            ads.Context.Initialize("{}", Core, () => initialized = true);

            Assert.AreEqual(1, ads.Consent.Updates);
            Assert.IsFalse(ads.Context.Consent.IsDone);
            Assert.AreEqual(0, ads.Sdk.Initializations, "no ad SDK before the consent answer");
            Assert.IsTrue(string.IsNullOrEmpty(ads.Context.CoreConfig.appResumeChannel?.forceAdUnit?.admobUnit?.id), "no ad config, so nothing can load meanwhile");

            ads.Consent.AnswerUpdate();
            Assert.IsTrue(ads.Context.Consent.IsDone);
            Assert.IsTrue(ads.Context.Consent.CanRequestAds);
            Assert.AreEqual(1, ads.Sdk.Initializations);
            Assert.AreEqual(0, ads.Consent.FormsShown, "consent already allows ads, so no form");
            ads.Sdk.BecomeReady();
            Assert.IsTrue(initialized);
            Assert.AreEqual("resume-inter", ads.AdMob.Ad(HDCAdUse.AppResume).AdUnitId);
        }

        [Test]
        public void TheFormShowsWhenConsentIsNeededAndTheAdsWaitForIt()
        {
            var ads = new HDCFakeAds();
            ads.Consent.Allows = false;

            ads.Context.Initialize("{}", Core, null);

            Assert.AreEqual(1, ads.Consent.FormsShown);
            Assert.AreEqual(HDCConsentGate.Step.ShowingForm, ads.Context.Consent.Current);
            Assert.AreEqual(0, ads.Sdk.Initializations, "the ads wait while the form is on screen");

            ads.Consent.CloseForm(true);
            Assert.IsTrue(ads.Context.Consent.CanRequestAds);
            Assert.AreEqual(1, ads.Sdk.Initializations);
        }

        [Test]
        public void ARefusedConsentKeepsEveryAdOffAndEndsTheAppLaunch()
        {
            var ads = new HDCFakeAds();
            ads.Consent.Allows = false;
            ads.Tracking.Status = HDCTrackingStatus.NotDetermined;
            bool initialized = false;
            bool notAllowed = false;
            ads.Context.AdsNotAllowed += () => notAllowed = true;

            ads.Context.Initialize("{}", Core, () => initialized = true);
            ads.Consent.CloseForm(false);
            ads.Sdk.BecomeReady();
            ads.Channels.AppResume.Initialize();
            ads.MainThread.Pause(true);
            ads.MainThread.Pause(false);

            Assert.IsTrue(ads.Context.Consent.IsDone);
            Assert.IsFalse(ads.Context.Consent.CanRequestAds);
            Assert.IsTrue(notAllowed);
            Assert.AreEqual(0, ads.Sdk.Initializations, "the ad SDK never starts");
            Assert.IsFalse(ads.Context.IsInitialized);
            Assert.IsFalse(initialized);
            Assert.IsEmpty(ads.AdMob.Ads, "no ad is made, so none can load");
            Assert.IsEmpty(ads.Native.Ads);
            Assert.AreEqual(0, ads.Tracking.Requests, "no tracking question when ads may not be requested");
            Assert.IsTrue(ads.Channels.AppLaunch.IsCompleted, "the app launch does not wait for ads that never come");
        }

        [Test]
        public void TrackingIsAskedAfterConsentAndBeforeTheSdk()
        {
            var ads = new HDCFakeAds();
            ads.Tracking.Status = HDCTrackingStatus.NotDetermined;

            ads.Context.Initialize("{}", Core, null);

            Assert.AreEqual(1, ads.Tracking.Requests);
            Assert.AreEqual(HDCConsentGate.Step.AskingTracking, ads.Context.Consent.Current);
            Assert.AreEqual(0, ads.Sdk.Initializations, "the SDK waits for the tracking answer");

            ads.Tracking.Answer(HDCTrackingStatus.Denied);
            Assert.AreEqual(1, ads.Sdk.Initializations, "any answer lets the ads start");
            Assert.AreEqual(HDCTrackingStatus.Denied, ads.Context.Consent.TrackingStatus);
        }

        [TestCase(HDCTrackingStatus.Unsupported, "", 0)]
        [TestCase(HDCTrackingStatus.Authorized, "", 0)]
        [TestCase(HDCTrackingStatus.Denied, "", 0)]
        [TestCase(HDCTrackingStatus.NotDetermined, "", 1)]
        [TestCase(HDCTrackingStatus.NotDetermined, "1111111111", 1)]
        [TestCase(HDCTrackingStatus.NotDetermined, "1101111111", 0)]
        [TestCase(HDCTrackingStatus.NotDetermined, "0111111111", 0)]
        [TestCase(HDCTrackingStatus.NotDetermined, "11", 0)]
        public void TrackingIsAskedOnceAndOnlyWithPersonalizedAdsConsent(HDCTrackingStatus status, string purposes, int requests)
        {
            var ads = new HDCFakeAds();
            ads.Tracking.Status = status;
            ads.Consent.PurposeConsents = purposes;

            ads.Context.Initialize("{}", Core, null);

            Assert.AreEqual(requests, ads.Tracking.Requests);
            Assert.AreEqual(requests == 0 ? 1 : 0, ads.Sdk.Initializations);
        }

        [Test]
        public void AnUnansweredConsentServiceGivesUpAfterTenSecondsAndALateAnswerIsIgnored()
        {
            var ads = new HDCFakeAds();
            ads.Consent.AnswersAtOnce = false;
            ads.Context.Initialize("{}", Core, null);

            ads.Wait(HDCConsentGate.UpdateTimeoutSeconds - 1f);
            Assert.IsFalse(ads.Context.Consent.IsDone);
            ads.Wait(1.5f);
            Assert.IsTrue(ads.Context.Consent.IsDone, "the consent saved on the device decides");
            Assert.AreEqual(1, ads.Sdk.Initializations);

            ads.Consent.Allows = false;
            ads.Consent.AnswerUpdate();
            Assert.AreEqual(0, ads.Consent.FormsShown, "a late answer brings no form");
            Assert.AreEqual(1, ads.Sdk.Initializations);
        }

        [TestCase(true, 1)]
        [TestCase(false, 0)]
        public void AFailedUpdateUsesTheSavedConsentWithoutAForm(bool saved, int initializations)
        {
            var ads = new HDCFakeAds();
            ads.Consent.UpdateError = "2 · no internet";
            ads.Consent.Allows = saved;

            ads.Context.Initialize("{}", Core, null);

            Assert.IsTrue(ads.Context.Consent.IsDone);
            Assert.AreEqual(saved, ads.Context.Consent.CanRequestAds);
            Assert.AreEqual(0, ads.Consent.FormsShown);
            Assert.AreEqual(initializations, ads.Sdk.Initializations);
            Assert.AreEqual("2 · no internet", ads.Context.Consent.LastError);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void AGoogleTestDeviceAsksAsIfItWereInTheEea(bool testDevice)
        {
            var ads = new HDCFakeAds();
            if (testDevice)
                ads.Testing.EnableTestDevice();

            ads.Context.Initialize("{}", Core, null);

            Assert.AreEqual(testDevice, ads.Consent.AskedInEea);
        }

        [Test]
        public void TheConsentStartsOnceWhetherTheSetupOrInitializeAsksFirst()
        {
            var ads = new HDCFakeAds();
            ads.Consent.AnswersAtOnce = false;
            ads.Context.Consent.Start();
            ads.Context.Consent.Start();
            ads.Context.Initialize("{}", Core, null);

            Assert.AreEqual(1, ads.Consent.Updates);
            ads.Consent.AnswerUpdate();
            Assert.AreEqual(1, ads.Sdk.Initializations);
        }

        [Test]
        public void ThePrivacyOptionsFormReopensFromThePublicApi()
        {
            var ads = new HDCFakeAds();
            ads.Consent.PrivacyOptionsRequired = true;
            ads.Context.Initialize("{}", Core, null);
            IAdsConsent consent = ads.Context.Consent;
            var closed = new List<string>();

            Assert.IsTrue(consent.PrivacyOptionsRequired);
            consent.ShowPrivacyOptions(closed.Add);

            Assert.AreEqual(1, ads.Consent.PrivacyFormsShown);
            CollectionAssert.AreEqual(new string[] { null }, closed);
        }

        [Test]
        public void TogglesMissingFromAdsConfigAskAsBefore()
        {
            var ads = new HDCFakeAds();
            ads.Tracking.Status = HDCTrackingStatus.NotDetermined;

            ads.Context.Initialize("{}", Core, null);

            Assert.AreEqual(1, ads.Consent.Updates);
            Assert.AreEqual(1, ads.Tracking.Requests);
            Assert.IsTrue(ads.Context.Consent.Toggles.Consent);
            Assert.IsTrue(ads.Context.Consent.Toggles.Att);
        }

        [Test]
        public void WithToggleConsentOffNoFormShowsAndTheAdsAreAllowed()
        {
            var ads = new HDCFakeAds();
            ads.Consent.Allows = false;

            ads.Context.Initialize(@"{ ""toggleConsent"": false }", Core, null);

            Assert.AreEqual(0, ads.Consent.Updates, "the consent service is not even asked");
            Assert.AreEqual(0, ads.Consent.FormsShown);
            Assert.IsTrue(ads.Context.Consent.ConsentSkipped);
            Assert.IsTrue(ads.Context.Consent.CanRequestAds, "counted as accepted");
            Assert.AreEqual(HDCConsentStatus.Obtained, ads.Context.Consent.Status, "reported as accepted");
            Assert.AreEqual(1, ads.Sdk.Initializations);
        }

        [Test]
        public void WithToggleConsentOffTheAttIsStillAskedWhateverThePurposes()
        {
            var ads = new HDCFakeAds();
            ads.Tracking.Status = HDCTrackingStatus.NotDetermined;
            ads.Consent.PurposeConsents = "0000000000";

            ads.Context.Initialize(@"{ ""toggleConsent"": false }", Core, null);

            Assert.AreEqual(1, ads.Tracking.Requests, "no consent was asked, so no purpose can refuse tracking");
            Assert.AreEqual(0, ads.Sdk.Initializations, "the SDK waits for the ATT answer as before");
            ads.Tracking.Answer(HDCTrackingStatus.Authorized);
            Assert.AreEqual(1, ads.Sdk.Initializations);
        }

        [TestCase(HDCTrackingStatus.NotDetermined, HDCTrackingStatus.Authorized)]
        [TestCase(HDCTrackingStatus.Denied, HDCTrackingStatus.Authorized)]
        [TestCase(HDCTrackingStatus.Unsupported, HDCTrackingStatus.Unsupported)]
        public void WithToggleAttOffTrackingCountsAsAcceptedWhereAttExists(HDCTrackingStatus device, HDCTrackingStatus reported)
        {
            var ads = new HDCFakeAds();
            ads.Tracking.Status = device;

            ads.Context.Initialize(@"{ ""toggleAtt"": false }", Core, null);

            Assert.AreEqual(reported, ads.Context.Consent.TrackingStatus);
            Assert.AreEqual(0, ads.Tracking.Requests);
        }

        [Test]
        public void WithToggleAttOffTrackingIsNeverAsked()
        {
            var ads = new HDCFakeAds();
            ads.Tracking.Status = HDCTrackingStatus.NotDetermined;

            ads.Context.Initialize(@"{ ""toggleAtt"": false }", Core, null);

            Assert.AreEqual(1, ads.Consent.Updates, "consent is still asked");
            Assert.AreEqual(0, ads.Tracking.Requests);
            Assert.AreEqual(HDCTrackingStatus.Authorized, ads.Context.Consent.TrackingStatus, "counted as accepted");
            Assert.AreEqual(1, ads.Sdk.Initializations);
        }

        [Test]
        public void WithBothTogglesOffNothingIsAskedAndTheAdsStartAtOnce()
        {
            var ads = new HDCFakeAds();
            ads.Consent.Allows = false;
            ads.Tracking.Status = HDCTrackingStatus.NotDetermined;

            ads.Context.Initialize(@"{ ""toggleConsent"": false, ""toggleAtt"": false }", Core, null);

            Assert.IsTrue(ads.Context.Consent.IsDone);
            Assert.AreEqual(0, ads.Consent.Updates + ads.Consent.FormsShown + ads.Tracking.Requests);
            Assert.IsTrue(ads.Context.Consent.CanRequestAds);
            Assert.AreEqual(1, ads.Sdk.Initializations);
        }

        [Test]
        public void TheFirstLaunchWaitsForAdsConfigWhenTheSetupAsksFirst()
        {
            var ads = new HDCFakeAds();
            ads.Context.Consent.Start();

            Assert.AreEqual(HDCConsentGate.Step.WaitingForConfig, ads.Context.Consent.Current);
            Assert.AreEqual(0, ads.Consent.Updates, "nothing is asked before the toggles are known");

            ads.Context.Initialize(@"{ ""toggleConsent"": false }", Core, null);

            Assert.AreEqual(0, ads.Consent.Updates);
            Assert.IsTrue(ads.Context.Consent.IsDone);
            Assert.AreEqual(HDCPrivacyToggles.ThisSession, ads.Context.Consent.Toggles.Source);
            Assert.AreEqual(1, ads.Sdk.Initializations);
        }

        [Test]
        public void ALaterLaunchUsesTheTogglesSavedLastTimeWithoutWaiting()
        {
            var store = new HDCFakeStore();
            new HDCFakeAds(store).Context.Initialize(@"{ ""toggleConsent"": false, ""toggleAtt"": false }", Core, null);

            var next = new HDCFakeAds(store);
            next.Consent.Allows = false;
            next.Context.Consent.Start();

            Assert.IsTrue(next.Context.Consent.IsDone, "the saved toggles decide at once");
            Assert.AreEqual(0, next.Consent.Updates);
            Assert.AreEqual(HDCPrivacyToggles.LastSession, next.Context.Consent.Toggles.Source);

            next.Context.Initialize("{}", Core, null);
            Assert.AreEqual(0, next.Consent.Updates, "this launch keeps the toggles it started with");
            Assert.IsTrue(next.Context.Consent.CanRequestAds);
            Assert.AreEqual(1, next.Sdk.Initializations);
            Assert.IsTrue(HDCPrivacyToggles.TryRead(store.Values[HDCAdNames.PrivacyTogglesKey], out HDCPrivacyToggles saved));
            Assert.IsTrue(saved.Consent && saved.Att, "the new ads_config is saved for the launch after");
        }

        [Test]
        public void EachConsentAndAttStepIsReportedForTracking()
        {
            var ads = new HDCFakeAds();
            ads.Consent.Allows = false;
            ads.Tracking.Status = HDCTrackingStatus.NotDetermined;
            var steps = new List<string>();
            ads.Context.Consent.Stepped += steps.Add;

            ads.Context.Initialize("{}", Core, null);
            ads.Consent.CloseForm(true);
            ads.Tracking.Answer(HDCTrackingStatus.Authorized);

            CollectionAssert.AreEqual(new[] { "cs_u_s", "cs_u_e", "cs_f_s", "cs_f_e", "cs_ok", "att_s", "att_auth" }, steps);
            CollectionAssert.AreEqual(steps, ads.Context.Consent.Steps, "kept for a listener that comes late");
        }

        [TestCase(@"{ ""toggleConsent"": false, ""toggleAtt"": false }", HDCTrackingStatus.NotDetermined, "cs_skip,cs_accept,cs_ok,att_skip,att_auth")]
        [TestCase("{}", HDCTrackingStatus.Denied, "cs_u_s,cs_u_e,cs_ok,att_known")]
        [TestCase("{}", HDCTrackingStatus.Unsupported, "cs_u_s,cs_u_e,cs_ok")]
        public void TheStepsShowWhatWasAskedAndWhatWasSkipped(string adsConfig, HDCTrackingStatus tracking, string expected)
        {
            var ads = new HDCFakeAds();
            ads.Tracking.Status = tracking;

            ads.Context.Initialize(adsConfig, Core, null);

            Assert.AreEqual(expected, string.Join(",", ads.Context.Consent.Steps));
        }

        [Test]
        public void RefusedFailedNotAllowedAndWaitingStepsAreReportedToo()
        {
            var refused = new HDCFakeAds();
            refused.Consent.Allows = false;
            refused.Context.Initialize("{}", Core, null);
            refused.Consent.CloseForm(false);
            Assert.AreEqual("cs_u_s,cs_u_e,cs_f_s,cs_f_e,cs_no", string.Join(",", refused.Context.Consent.Steps));

            var failed = new HDCFakeAds();
            failed.Consent.UpdateError = "2 · no internet";
            failed.Context.Initialize("{}", Core, null);
            Assert.AreEqual("cs_u_s,cs_u_m,cs_ok", string.Join(",", failed.Context.Consent.Steps));

            var notPersonal = new HDCFakeAds();
            notPersonal.Tracking.Status = HDCTrackingStatus.NotDetermined;
            notPersonal.Consent.PurposeConsents = "0111111111";
            notPersonal.Context.Initialize("{}", Core, null);
            Assert.AreEqual("cs_u_s,cs_u_e,cs_ok,att_purpose_no", string.Join(",", notPersonal.Context.Consent.Steps));

            var firstLaunch = new HDCFakeAds();
            firstLaunch.Context.Consent.Start();
            firstLaunch.Context.Initialize("{}", Core, null);
            StringAssert.StartsWith("cs_wait_config,cs_u_s", string.Join(",", firstLaunch.Context.Consent.Steps));
        }

        [TestCase("", null)]
        [TestCase("1111111111", "cs_accept")]
        [TestCase("1011111111", "cs_accept")]
        [TestCase("1101111111", "cs_partial")]
        [TestCase("1", "cs_partial")]
        [TestCase("0000000000", "cs_denied")]
        [TestCase("0111111111", "cs_denied")]
        public void TheConsentAnswerComesFromTheTcfPurposes(string purposes, string step) =>
            Assert.AreEqual(step, HDCConsentGate.ConsentAnswerStep(purposes));

        [TestCase("1111111111", "cs_accept")]
        [TestCase("0000000000", "cs_denied")]
        [TestCase("1100000000", "cs_partial")]
        public void TheAnswerOnTheFormIsReportedRightAfterItCloses(string purposes, string answer)
        {
            var ads = new HDCFakeAds();
            ads.Consent.Allows = false;
            ads.Consent.PurposeConsents = purposes;

            ads.Context.Initialize("{}", Core, null);
            ads.Consent.CloseForm(true);

            Assert.AreEqual("cs_u_s,cs_u_e,cs_f_s,cs_f_e," + answer + ",cs_ok", string.Join(",", ads.Context.Consent.Steps),
                "a refusal on the form still lets Google show limited ads, so cs_ok alone cannot count refusals");
        }

        [TestCase(HDCTrackingStatus.Authorized, "att_auth")]
        [TestCase(HDCTrackingStatus.Denied, "att_denied")]
        [TestCase(HDCTrackingStatus.Restricted, "att_restricted")]
        [TestCase(HDCTrackingStatus.NotDetermined, null)]
        public void EachAttAnswerHasItsStep(HDCTrackingStatus answer, string step) =>
            Assert.AreEqual(step, HDCConsentGate.AnswerStep(answer));

        [Test]
        public void DoneFiresOnceTheQuestionsAreAnswered()
        {
            var ads = new HDCFakeAds();
            ads.Consent.AnswersAtOnce = false;
            int done = 0;
            ads.Context.Consent.Done += () => done++;
            ads.Context.Initialize("{}", Core, null);

            Assert.AreEqual(0, done);
            ads.Consent.AnswerUpdate();
            Assert.AreEqual(1, done);
        }
    }
}
