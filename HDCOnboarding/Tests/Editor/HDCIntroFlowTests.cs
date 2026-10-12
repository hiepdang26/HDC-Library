using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using HDC.Ads;
using HDC.Ads.Infrastructure;
using HDC.Ads.Tests;
using HDC.Onboarding.Editor;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace HDC.Onboarding.Tests
{
    public class HDCIntroFlowTests : HDCPlayModeTest
    {
        private const string Ads = @"{ ""selectedAdCoreName"": ""core"",
  ""forceAdChannel"": { ""isEnabled"": true, ""positionConfigs"": [ { ""positionName"": ""native_intro_full"", ""canShow"": true, ""cappingTime"": 0 } ] },
  ""popupChannel"": { ""isEnabled"": true, ""positionConfigs"": [
     { ""isEnabled"": true, ""positionName"": ""native_intro1"" }, { ""isEnabled"": true, ""positionName"": ""native_intro2"" } ] } }";

        private const string Core = @"{
  ""forceAdLayoutConfig"": { ""layoutGroups"": [ { ""groupName"": ""cls"", ""layouts"": [ { ""layout"": ""fs_single_cls_01"", ""layoutTime"": 5 } ] } ] },
  ""forceAdGroups"": [ { ""mediationPriority"": 1, ""groupName"": ""native_intro_full"", ""positionNames"": [ ""native_intro_full"" ],
      ""nativeAdUnit"": { ""id"": ""native-ok"", ""layoutGroupName"": ""cls"" } } ],
  ""popupGroups"": [
    { ""groupName"": ""native_intro1"", ""positionNames"": [ ""native_intro1"" ], ""nativeAdUnit"": { ""id"": ""popup-1-ok"", ""layout"": ""popup_single_manual_01"", ""timeShow"": 30 } },
    { ""groupName"": ""native_intro2"", ""positionNames"": [ ""native_intro2"" ], ""nativeAdUnit"": { ""id"": ""popup-2-ok"", ""layout"": ""popup_single_manual_01"", ""timeShow"": 30 } } ] }";

        private const string Pages = @"{ ""nextSceneName"": """", ""Configs"": [
    { ""IntroType"": 1, ""AdConfig"": { ""PreloadType"": 0, ""GroupName"": ""native_intro1"" }, ""TimeDelayPreload"": 0, ""IntroInterval"": 1, ""IntroDescription"": ""One"" },
    { ""IntroType"": 0, ""AdConfig"": { ""PreloadType"": 0, ""GroupName"": ""native_intro2"" }, ""TimeDelayPreload"": 0, ""IntroInterval"": 1, ""IntroDescription"": ""Two"" },
    { ""IntroType"": 5, ""AdConfig"": { ""PreloadType"": 1, ""GroupName"": ""native_intro_full"" }, ""TimeDelayPreload"": 0 } ] }";

        private const string OldProfilePage = @"{ ""nextSceneName"": """", ""Configs"": [
    { ""IntroType"": 3, ""AdConfig"": { ""PreloadType"": 0, ""GroupName"": ""native_intro1"" }, ""TimeDelayPreload"": 0 },
    { ""IntroType"": 5, ""AdConfig"": { ""PreloadType"": 1, ""GroupName"": ""native_intro_full"" }, ""TimeDelayPreload"": 0 } ],
  ""Profile"": { ""isEnable"": true, ""PopupConfig"": ""native_intro1"" } }";

        private HDCOnboardingPrefs prefs;

        [SetUp]
        public void CleanPrefs() => prefs = new HDCOnboardingPrefs();

        [TearDown]
        public void RestorePrefs() => prefs.Dispose();

        [UnityTest]
        public IEnumerator TheIntroWalksItsPagesWithTheirAds()
        {
            yield return new EnterPlayMode();
            var tracked = new List<string>();
            var events = new List<string>();
            yield return Start(Pages, "native_intro1", tracked, events);

            yield return Continue("AL View");
            Assert.IsTrue(events.Contains("popup Shown pu_native_intro1"), string.Join(", ", events));
            yield return WaitFor(() => events.Contains("popup Closed pu_native_intro1"), 2f);
            yield return Continue("AR View");
            Assert.IsTrue(events.Contains("popup Shown pu_native_intro2"), "the next page's ad was preloaded: " + string.Join(", ", events));
            yield return WaitFor(() => tracked.Contains("7_end_intro"), 10f);

            Assert.IsTrue(events.Contains("fullscreen Shown fa_native_intro_full"), string.Join(", ", events));
            HDCSplashFlowTests.AssertInOrder(tracked, "7_start_intro", "7_start_intro_0", "7_start_intro_1", "7_start_intro_2", "7_end_intro");
            Assert.AreEqual(1, PlayerPrefs.GetInt("intro_0"));
            Assert.AreEqual(1, PlayerPrefs.GetInt("intro_2"));
            Assert.IsTrue(HDCIntro.IsFinished);
            yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator APageThatNoViewShowsIsSkipped()
        {
            yield return new EnterPlayMode();
            var tracked = new List<string>();
            var events = new List<string>();
            LogAssert.Expect(LogType.Error, new Regex("page type 3, which no view shows: skipped"));
            yield return Start(OldProfilePage, "native_intro1", tracked, events);

            yield return WaitFor(() => tracked.Contains("7_end_intro"), 10f);

            HDCSplashFlowTests.AssertInOrder(tracked, "7_start_intro", "7_start_intro_0", "7_start_intro_1", "7_end_intro");
            Assert.AreEqual(1, PlayerPrefs.GetInt("intro_0"));
            yield return WaitFor(() => events.Contains("fullscreen Loaded fa_native_intro_full"), 3f);
            yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator APopupWaitsForTheFullScreenAdOnScreen()
        {
            yield return new EnterPlayMode();
            var events = new List<string>();
            HDCAdsSdk.AdEvent += adEvent => events.Add(adEvent.format + " " + adEvent.type + " " + adEvent.id);
            bool ready = false;
            HDCAds.Initialize(Ads, Core, () => ready = true);
            yield return WaitFor(() => ready, 10f);
            HDCAds.Popup.Initialize("native_intro1");
            yield return WaitFor(() => HDCOnboardingAds.IsPopupReady("native_intro1"), 5f);

            var canvas = new GameObject("Canvas", typeof(Canvas)).GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var area = new GameObject("Area", typeof(RectTransform)).GetComponent<RectTransform>();
            area.SetParent(canvas.transform, false);
            area.anchorMin = Vector2.zero;
            area.anchorMax = new Vector2(0.5f, 1f);
            area.offsetMin = area.offsetMax = Vector2.zero;
            HDCOnboardingAds.Context.SetOverlayAd("splash_naf", true);
            var runner = new GameObject("Runner").AddComponent<HDCSplashLoadingView>();
            runner.StartCoroutine(HDCOnboardingAds.ShowPopupUntilShown("native_intro1", area, new HDCWaitToken()));

            yield return new WaitForSecondsRealtime(1.5f);
            CollectionAssert.DoesNotContain(events, "popup Shown pu_native_intro1", "no popup over a full-screen ad");
            HDCOnboardingAds.Context.SetOverlayAd("splash_naf", false);
            yield return WaitFor(() => events.Contains("popup Shown pu_native_intro1"), 3f);
            yield return new ExitPlayMode();
        }

        private static IEnumerator Start(string introConfig, string preloaded, List<string> tracked, List<string> events)
        {
            HDCOnboardingTracking.Tracked += tracked.Add;
            HDCAdsSdk.AdEvent += adEvent => events.Add(adEvent.format + " " + adEvent.type + " " + adEvent.id);
            HDCCustomConfig.Declare(new Dictionary<string, string>
            {
                { HDCOnboardingKeys.OnboardingConfig, "{\"intro_config\":" + introConfig + "}" },
            });
            bool ready = false;
            HDCAds.Initialize(Ads, Core, () => ready = true);
            yield return WaitFor(() => ready, 10f);
            HDCAds.Popup.Initialize(preloaded);
            LogAssert.Expect(LogType.Error, new Regex("intro_config names no nextSceneName"));
            AsyncOperation loading = EditorSceneManager.LoadSceneAsyncInPlayMode(HDCOnboardingPaths.IntroScene, new LoadSceneParameters(LoadSceneMode.Single));
            while (!loading.isDone)
                yield return null;
        }

        private static IEnumerator Continue(string viewName)
        {
            Transform view = Find(viewName);
            Button button = view.GetComponentsInChildren<Button>(true).First(candidate => candidate.name == "SaveButton" || candidate.name == "ContinueButton");
            yield return WaitFor(() => view.gameObject.activeInHierarchy && button.gameObject.activeInHierarchy, 5f);
            button.onClick.Invoke();
            yield return WaitFor(() => !view.gameObject.activeInHierarchy || view.Find("Loading")?.gameObject.activeSelf == true, 10f);
        }

        private static Transform Find(string name) =>
            SceneManager.GetActiveScene().GetRootGameObjects().Select(root => root.transform.Find("Canvas/" + name) ?? FindIn(root.transform, name))
                .First(found => found != null);

        private static Transform FindIn(Transform root, string name) =>
            root.GetComponentsInChildren<Transform>(true).FirstOrDefault(child => child.name == name);
    }
}
