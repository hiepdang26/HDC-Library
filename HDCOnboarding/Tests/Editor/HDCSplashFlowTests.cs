using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HDC.Ads;
using HDC.Ads.Composition;
using HDC.Ads.Diagnostics;
using HDC.Ads.Domain;
using HDC.Ads.Infrastructure;
using HDC.Ads.Ports;
using HDC.Ads.Tests;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace HDC.Onboarding.Tests
{
    public class HDCSplashFlowTests : HDCPlayModeTest
    {
        private const string Ads = @"{ ""selectedAdCoreName"": ""core"",
  ""popupChannel"": { ""isEnabled"": true, ""positionConfigs"": [ { ""isEnabled"": true, ""positionName"": ""native_splash"" } ] } }";

        private const string Core = @"{
  ""forceAdLayoutConfig"": { ""layoutGroups"": [ { ""groupName"": ""cls"", ""layouts"": [ { ""layout"": ""fs_single_cls_01"", ""layoutTime"": 5 } ] } ] },
  ""popupGroups"": [ { ""groupName"": ""native_splash"", ""positionNames"": [ ""native_splash"" ],
      ""nativeAdUnit"": { ""id"": ""popup-ok"", ""layout"": ""popup_single_manual_01"", ""timeShow"": 3 } } ] }";

        private const string Splash = @"{ ""loadingTime"": 1, ""nativeSplashGroup"": ""native_splash"", ""nativeSplashShowTime"": 0.5,
  ""nativeSplashLoadTimeOut"": 5, ""launchChannel"": { ""launchAdType"": 0, ""forceAdUnit"": { ""admobUnit"": { ""id"": ""inter-ok"" } } },
  ""interLoadAdTimeout"": 5, ""delayShowInterstitials"": 0.1,
  ""nativeAfterInterId"": ""naf-ok"", ""nativeAfterLayoutGroup"": ""cls"", ""firstOpenNextScene"": """", ""nextScene"": """" }";

        private HDCOnboardingPrefs prefs;

        [SetUp]
        public void CleanPrefs() => prefs = new HDCOnboardingPrefs();

        [TearDown]
        public void RestorePrefs() => prefs.Dispose();

        [UnityTest]
        public IEnumerator TheSplashLoadsAndShowsItsAdsInTheOldOrder()
        {
            yield return new EnterPlayMode();
            var tracked = new List<string>();
            var events = new List<string>();
            var revenues = new List<HDCAdRevenue>();
            Start(Splash, tracked, events, revenues);

            yield return WaitFor(() => tracked.Contains("6_end_splash"), 30f);

            AssertInOrder(tracked, "4_admob_start", "4_admob_end", "1_firebase_end", "3_admob_start", "3_admob_end", "4_remote_start", "4_remote_end",
                "5_load_ad_start", "5_na_l_s", "5_na_l_s_loaded", "5_na_l_e", "5_na_s_s", "5_na_s_e", "5_load_ad_end", "5_naf_l_s", "5_it_s_s",
                "5_it_s_s_closed", "5_it_s_e", "6_end_splash");
            AssertInOrder(tracked, "5_it_l_s", "5_it_l_s_loaded", "5_it_l_e", "5_load_ad_end");
            AssertInOrder(tracked, "4_admob_start", "4_cs_u_s", "4_cs_u_e", "4_cs_ok", "4_admob_end");
            AssertInOrder(tracked, "5_it_s_s", "5_naf_l_s_loaded", "5_naf_l_e", "5_naf_s_s", "5_naf_s_e", "5_it_s_e");
            CollectionAssert.DoesNotContain(tracked, "2_internet_start");
            Assert.AreEqual(1, tracked.Count(value => value == "5_naf_s_s"), "the native after the interstitial shows once");
            Assert.Less(events.IndexOf("fullscreen Shown splash_naf"), events.IndexOf("interstitial Closed splash_it"),
                "a native loaded during the interstitial shows under it, like the old system: " + string.Join(", ", events));
            Assert.AreEqual(0, events.Count(e => e.StartsWith("fullscreen ShowFailed")), string.Join(", ", events));
            Assert.IsTrue(revenues.Any(r => r.Channel == HDCAdChannel.AppLaunch && r.Position == "splash" && r.Format == HDCAdFormat.Interstitial));
            Assert.IsTrue(revenues.Any(r => r.Channel == HDCAdChannel.AppLaunch && r.Position == "splash" && r.Format == HDCAdFormat.Fullscreen));
            Assert.IsTrue(revenues.Any(r => r.Channel == HDCAdChannel.Popup && r.Position == "native_splash"), "the native splash shows in its area");
            Assert.IsTrue(HDCSplash.IsFinished);
            Assert.IsTrue(HDCAds.AppLaunch.IgnoreAds, "the splash shows the launch ads instead of the app launch channel");
            Assert.IsTrue(HDCAds.AppLaunch.IsCompleted, "app launch completes with the splash");
            yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator ANativeLoadedBeforeTheInterstitialShowsUnderItRightAway()
        {
            yield return new EnterPlayMode();
            var tracked = new List<string>();
            var events = new List<string>();
            var revenues = new List<HDCAdRevenue>();
            Start(Splash.Replace(@"""delayShowInterstitials"": 0.1", @"""delayShowInterstitials"": 1.5"), tracked, events, revenues);

            yield return WaitFor(() => tracked.Contains("6_end_splash"), 30f);

            AssertInOrder(tracked, "5_naf_l_e", "5_it_s_s", "5_naf_s_s", "5_it_s_s_closed", "5_it_s_e", "6_end_splash");
            int interstitialShown = events.IndexOf("interstitial Shown splash_it");
            int nativeShown = events.IndexOf("fullscreen Shown splash_naf");
            int interstitialClosed = events.IndexOf("interstitial Closed splash_it");
            Assert.Less(interstitialShown, nativeShown, string.Join(", ", events));
            Assert.Less(nativeShown, interstitialClosed, "the native goes under the interstitial as it shows: " + string.Join(", ", events));
            Assert.AreEqual(1, tracked.Count(value => value == "5_naf_s_s"), "the native after the interstitial shows once");
            Assert.AreEqual(0, events.Count(e => e.StartsWith("fullscreen ShowFailed")), string.Join(", ", events));
            yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator TheSplashSkipsItsAdsWhenConsentRefusesThem()
        {
            yield return new EnterPlayMode();
            HDCAdsRuntime.Use(HDCAdsRuntime.CreateDefault(new RefusingConsent()));
            var tracked = new List<string>();
            var events = new List<string>();
            var revenues = new List<HDCAdRevenue>();
            Start(Splash, tracked, events, revenues);

            yield return WaitFor(() => tracked.Contains("6_end_splash"), 30f);

            AssertInOrder(tracked, "4_admob_start", "4_admob_end", "1_firebase_end", "3_admob_start", "3_admob_end", "4_remote_start", "4_remote_end",
                "5_load_ad_skip_consent", "6_end_splash");
            CollectionAssert.DoesNotContain(tracked, "5_load_ad_start");
            AssertInOrder(tracked, "4_admob_start", "4_cs_u_s", "4_cs_u_e", "4_cs_f_s", "4_cs_f_e", "4_cs_no", "4_admob_end", "5_load_ad_skip_consent");
            Assert.IsEmpty(events, "the ad SDK never starts and no ad loads");
            Assert.IsFalse(HDCAds.IsInitialized);
            Assert.IsTrue(HDCAds.Consent.IsDone);
            Assert.IsFalse(HDCAds.Consent.CanRequestAds);
            Assert.IsTrue(HDCSplash.IsFinished);
            Assert.IsTrue(HDCAds.AppLaunch.IsCompleted);
            yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator AFailedInterstitialLeavesTheNativeToShowAlone()
        {
            yield return new EnterPlayMode();
            var tracked = new List<string>();
            var events = new List<string>();
            Start(Splash.Replace("inter-ok", "inter-fail").Replace(@"""nativeSplashGroup"": ""native_splash""", @"""nativeSplashGroup"": """""),
                tracked, events, new List<HDCAdRevenue>());

            yield return WaitFor(() => tracked.Contains("6_end_splash"), 30f);

            AssertInOrder(tracked, "5_load_ad_start", "5_it_l_s", "5_it_l_s_failed", "5_it_l_m", "5_load_ad_end", "5_naf_l_s", "5_naf_l_e",
                "5_naf_s_s", "5_naf_s_e", "6_end_splash");
            CollectionAssert.DoesNotContain(tracked, "5_it_s_s");
            CollectionAssert.DoesNotContain(tracked, "5_na_l_s");
            yield return new WaitForSecondsRealtime(3f);
            Assert.AreEqual(1, HDCAdsTracker.Find(HDCAdFormat.Interstitial, "splash_it")?.Requests, "a failed splash interstitial is not loaded again");
            yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator ANativeLaunchAdShowsWhereTheInterstitialWas()
        {
            yield return new EnterPlayMode();
            var tracked = new List<string>();
            var events = new List<string>();
            var revenues = new List<HDCAdRevenue>();
            Start(Splash.Replace(@"""launchAdType"": 0, ""forceAdUnit"": { ""admobUnit"": { ""id"": ""inter-ok"" } }",
                    @"""launchAdType"": 1, ""forceAdUnit"": { ""nativeAdUnit"": { ""id"": ""launch-ok"", ""layoutGroupName"": ""cls"" } }"),
                tracked, events, revenues);

            yield return WaitFor(() => tracked.Contains("6_end_splash"), 40f);

            AssertInOrder(tracked, "5_it_l_s", "5_it_l_s_loaded", "5_it_l_e", "5_load_ad_end", "5_it_s_s", "5_it_s_s_closed", "5_it_s_e", "6_end_splash");
            Assert.IsFalse(events.Any(e => e.StartsWith(HDCAdFormat.Interstitial)), "no interstitial: " + string.Join(", ", events));
            Assert.Greater(events.IndexOf("fullscreen Shown splash_naf"), events.IndexOf("fullscreen Closed splash_launch_native"),
                "the native after the launch ad shows once it closed: " + string.Join(", ", events));
            Assert.IsTrue(revenues.Any(r => r.Channel == HDCAdChannel.AppLaunch && r.Position == "splash" && r.Format == HDCAdFormat.Fullscreen));
            Assert.IsFalse(HDCOnboardingAds.Context.HasOverlayAd, "the launch ad and the native after clear their overlay once closed");
            yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator RemovedAdsSkipTheSplashAds()
        {
            yield return new EnterPlayMode();
            var tracked = new List<string>();
            var events = new List<string>();
            HDCAds.SetAdsRemoved(true);
            Start(Splash, tracked, events, new List<HDCAdRevenue>());

            yield return WaitFor(() => tracked.Contains("6_end_splash"), 30f);

            AssertInOrder(tracked, "4_remote_end", "6_end_splash");
            CollectionAssert.DoesNotContain(tracked, "5_load_ad_start");
            Assert.IsFalse(events.Any(e => e.Contains("splash_")), string.Join(", ", events));
            HDCAds.SetAdsRemoved(false);
            yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator TheLoadingBarFillsFromZeroWithoutJumping()
        {
            yield return new EnterPlayMode();
            var tracked = new List<string>();
            var progress = new List<float>();
            var progressAt = new Dictionary<string, float>();
            HDCOnboardingTracking.Tracked += name => progressAt[name] = progress.Count > 0 ? progress[progress.Count - 1] : 0f;
            HDCSplash splash = Start(Splash.Replace("\"loadingTime\": 1", "\"loadingTime\": 0"), tracked, new List<string>(), new List<HDCAdRevenue>());
            splash.ProgressChanged.AddListener(progress.Add);

            yield return WaitFor(() => tracked.Contains("6_end_splash"), 30f);

            Assert.Less(progress[0], 0.05f, "the bar starts at 0%");
            for (int i = 1; i < progress.Count; i++)
                Assert.GreaterOrEqual(progress[i], progress[i - 1], "the bar never goes back");
            Assert.Less(progressAt["5_load_ad_end"], 0.5f, "a config without loadingTime keeps filling slowly instead of jumping to 90%");
            Assert.AreEqual(1f, progress[progress.Count - 1], 1e-4f);
            yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator TheLoadingViewStartsAtZero()
        {
            yield return new EnterPlayMode();
            var root = new GameObject("Loading");
            root.SetActive(false);
            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            fill.type = Image.Type.Filled;
            fill.fillAmount = 1f;
            var view = new SerializedObject(root.AddComponent<HDCSplashLoadingView>());
            view.FindProperty("fill").objectReferenceValue = fill;
            view.ApplyModifiedPropertiesWithoutUndo();

            root.SetActive(true);

            Assert.AreEqual(0f, fill.fillAmount);
            root.GetComponent<HDCSplashLoadingView>().UpdateProgress(0.5f);
            Assert.AreEqual(0.5f, fill.fillAmount);
            yield return new ExitPlayMode();
        }

        private static HDCSplash Start(string splashConfig, List<string> tracked, List<string> events, List<HDCAdRevenue> revenues)
        {
            HDCOnboardingTracking.Tracked += tracked.Add;
            HDCAdsSdk.AdEvent += adEvent => events.Add(adEvent.format + " " + adEvent.type + " " + adEvent.id);
            HDCCustomConfig.Declare(new Dictionary<string, string>
            {
                { HDCOnboardingKeys.OnboardingConfig, "{\"splash_config\":" + splashConfig + "}" },
            });
            HDCAds.Initialize(Ads, Core);
            HDCAds.Revenue += revenues.Add;

            var canvas = new GameObject("Canvas", typeof(Canvas)).GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var area = new GameObject("Native Splash Area", typeof(RectTransform)).GetComponent<RectTransform>();
            area.SetParent(canvas.transform, false);
            area.anchorMin = Vector2.zero;
            area.anchorMax = new Vector2(1f, 0.25f);
            area.offsetMin = area.offsetMax = Vector2.zero;

            var splash = new GameObject("HDCSplash");
            splash.SetActive(false);
            var component = splash.AddComponent<HDCSplash>();
            var serialized = new SerializedObject(component);
            serialized.FindProperty("checkInternet").boolValue = false;
            serialized.FindProperty("loadToNextScene").boolValue = false;
            serialized.FindProperty("nativeSplashArea").objectReferenceValue = area;
            serialized.FindProperty("nativeSplashAdMobArea").objectReferenceValue = area;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            splash.SetActive(true);
            return component;
        }

        private sealed class RefusingConsent : IConsentProvider
        {
            public bool CanRequestAds => false;

            public HDCConsentStatus Status => HDCConsentStatus.Required;

            public bool PrivacyOptionsRequired => true;

            public string PurposeConsents => string.Empty;

            public void Update(bool debugInEea, System.Action<string> onDone) => onDone(null);

            public void ShowFormIfRequired(System.Action<string> onDone) => onDone(null);

            public void ShowPrivacyOptions(System.Action<string> onDone) => onDone(null);

            public void Reset()
            {
            }
        }

        internal static void AssertInOrder(List<string> tracked, params string[] expected)
        {
            int last = -1;
            foreach (string value in expected)
            {
                int index = tracked.IndexOf(value);
                Assert.Greater(index, last, $"{value} after the step before it in: {string.Join(", ", tracked)}");
                last = index;
            }
        }
    }
}
