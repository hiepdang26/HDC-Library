using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using HDC.Ads;
using HDC.Ads.Diagnostics;
using HDC.Ads.Domain;
using HDC.Ads.Infrastructure;
using HDC.Ads.Tests;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace HDC.Onboarding.Tests
{
    public class HDCNativeBannerTests : HDCPlayModeTest
    {
        private const string Ads = @"{ ""selectedAdCoreName"": ""core"",
  ""popupChannel"": { ""isEnabled"": true, ""positionConfigs"": [ { ""isEnabled"": true, ""positionName"": ""nb_a"" },
    { ""isEnabled"": true, ""positionName"": ""nb_b"" }, { ""isEnabled"": true, ""positionName"": ""nb_fail"" } ] } }";

        private const string Core = @"{ ""popupGroups"": [
  { ""groupName"": ""nb_a"", ""positionNames"": [ ""nb_a"" ], ""nativeAdUnit"": { ""id"": ""popup-ok-a"", ""layout"": ""popup_single_manual_01"", ""timeShow"": 3 } },
  { ""groupName"": ""nb_b"", ""positionNames"": [ ""nb_b"" ], ""nativeAdUnit"": { ""id"": ""popup-ok-b"", ""layout"": ""popup_single_manual_01"", ""timeShow"": 3 } },
  { ""groupName"": ""nb_off"", ""positionNames"": [ ""nb_off"" ], ""nativeAdUnit"": { ""id"": ""popup-ok-off"", ""layout"": ""popup_single_manual_01"", ""timeShow"": 3 } },
  { ""groupName"": ""nb_fail"", ""positionNames"": [ ""nb_fail"" ], ""nativeAdUnit"": { ""id"": ""popup-fail"", ""layout"": ""popup_single_manual_01"", ""timeShow"": 3 } } ] }";

        private const string OldConfig =
            "{\n  \"activeSceneName\": \"SampleScene\",\n  \"bannerLoops\": [\n    {\n      \"isEnabled\": true,\n      \"groupName\": \"native_banner\",\n" +
            "      \"timeOut\": 30,\n      \"adShowCount\": 2,\n      \"defaultTimeConfig\": 30,\n      \"metaTimeConfig\": 60\n    },\n    {\n" +
            "      \"isEnabled\": true,\n      \"groupName\": \"native_banner_1\",\n      \"timeOut\": 30,\n      \"adShowCount\": -1,\n" +
            "      \"defaultTimeConfig\": 45,\n      \"metaTimeConfig\": 45\n    }\n  ]\n}";

        private readonly List<string> events = new List<string>();
        private HDCOnboardingPrefs prefs;

        [SetUp]
        public void CleanPrefs()
        {
            prefs = new HDCOnboardingPrefs();
            events.Clear();
        }

        [TearDown]
        public void RestorePrefs() => prefs.Dispose();

        [Test]
        public void TheOldConfigReadsTheSame()
        {
            HDCNativeBannerConfig config = HDCNativeBannerConfig.Parse(OldConfig);

            Assert.AreEqual("SampleScene", config.activeSceneName);
            Assert.AreEqual(2, config.bannerLoops.Length);
            HDCNativeBannerLoop first = config.bannerLoops[0];
            Assert.IsTrue(first.isEnabled);
            Assert.AreEqual("native_banner", first.groupName);
            Assert.AreEqual(30f, first.timeOut);
            Assert.AreEqual(2, first.adShowCount);
            Assert.AreEqual(30f, first.defaultTimeConfig);
            Assert.AreEqual(60f, first.metaTimeConfig);
            Assert.AreEqual(-1, config.bannerLoops[1].adShowCount);
        }

        [Test]
        public void ABrokenConfigReadsAsNone()
        {
            Assert.IsNull(HDCNativeBannerConfig.Parse(""));
            LogAssert.Expect(LogType.Error, new Regex("first_native_banner_config is not valid JSON"));
            Assert.IsNull(HDCNativeBannerConfig.Parse("{ not json"));
        }

        [Test]
        public void MetaAdsStayForTheMetaTime()
        {
            var loop = new HDCNativeBannerLoop { defaultTimeConfig = 30f, metaTimeConfig = 60f };

            Assert.AreEqual(60f, HDCNativeBannerLoader.HoldSeconds(loop, "Meta Audience Network"));
            Assert.AreEqual(30f, HDCNativeBannerLoader.HoldSeconds(loop, "AdMob Network"));
            Assert.AreEqual(30f, HDCNativeBannerLoader.HoldSeconds(loop, null));
        }

        [UnityTest]
        public IEnumerator TheBannersTakeTurnsUntilNoShowIsLeft()
        {
            yield return new EnterPlayMode();
            Start(Config(Loop("nb_a", 1, 0.3f), Loop("nb_b", 2, 0.3f)));

            yield return WaitFor(() => HDCNativeBannerLoader.IsRunning, 5f);
            yield return WaitFor(() => !HDCNativeBannerLoader.IsRunning, 15f);

            CollectionAssert.AreEqual(new[] { "pu_nb_a", "pu_nb_b", "pu_nb_b" }, Shown(), string.Join(", ", events));
            Assert.Less(events.IndexOf("Closed pu_nb_a"), events.IndexOf("Shown pu_nb_b"), "the banner before is hidden first: " + string.Join(", ", events));
            yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator AnAdThatLoadedAfterItsTimeOutShowsOnItsNextTurnWithoutLoadingAgain()
        {
            yield return new EnterPlayMode();
            Start(Config(Loop("nb_a", 1, 0.3f, 0.1f), Loop("nb_b", 1, 0.8f)));

            yield return WaitFor(() => HDCNativeBannerLoader.IsRunning, 5f);
            yield return WaitFor(() => !HDCNativeBannerLoader.IsRunning, 15f);

            CollectionAssert.AreEqual(new[] { "pu_nb_b", "pu_nb_a" }, Shown(), string.Join(", ", events));
            Assert.AreEqual(1, HDCAdsTracker.Find(HDCAdFormat.Popup, "pu_nb_a")?.Requests, "the ad that came late is shown, not loaded again");
            yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator ABannerThatCannotShowKeepsItsTurns()
        {
            yield return new EnterPlayMode();
            HDCNativeBannerLoader loader = Start(Config(Loop("nb_off", 1, 0.3f), Loop("nb_a", 1, 0.3f)));

            yield return WaitFor(() => Shown().Contains("pu_nb_a"), 10f);
            yield return new WaitForSecondsRealtime(1f);

            Assert.AreEqual(1, loader.Config.bannerLoops[0].adShowCount, "a banner that never showed is not counted");
            Assert.AreEqual(0, loader.Config.bannerLoops[1].adShowCount);
            Assert.IsTrue(HDCNativeBannerLoader.IsRunning, "nb_off still has a turn, so the loader keeps trying");
            CollectionAssert.DoesNotContain(Shown(), "pu_nb_off");
            yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator AFailedLoadWaitsLongerEachTime()
        {
            yield return new EnterPlayMode();
            var failures = new List<float>();
            HDCAdsSdk.AdEvent += adEvent =>
            {
                if (adEvent.format == HDCAdFormat.Popup && adEvent.id == "pu_nb_fail" && adEvent.type == HDCAdEventType.LoadFailed)
                    failures.Add(Time.realtimeSinceStartup);
            };
            LogAssert.Expect(LogType.Warning, new Regex("NativeBanner load failed group=nb_fail retryIn=2s"));
            LogAssert.Expect(LogType.Warning, new Regex("NativeBanner load failed group=nb_fail retryIn=4s"));
            Start(Config(Loop("nb_fail", 1, 0.3f)));

            yield return WaitFor(() => failures.Count >= 2, 10f);
            yield return new WaitForSecondsRealtime(0.2f);

            Assert.GreaterOrEqual(failures[1] - failures[0], 2f, "the second load waits the first retry delay");
            yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator RemovingAdsStopsTheBanners()
        {
            yield return new EnterPlayMode();
            Start(Config(Loop("nb_a", -1, 0.5f)));
            yield return WaitFor(() => Shown().Count > 0, 5f);

            HDCAds.SetAdsRemoved(true);
            yield return WaitFor(() => !HDCNativeBannerLoader.IsRunning, 5f);

            Assert.Contains("Closed pu_nb_a", events);
            HDCAds.SetAdsRemoved(false);
            yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator TheBannersWaitForTheirScene()
        {
            yield return new EnterPlayMode();
            Start("{\"activeSceneName\":\"Somewhere else\",\"bannerLoops\":[" + Loop("nb_a", 1, 0.3f) + "]}");

            yield return new WaitForSecondsRealtime(1.5f);

            Assert.IsFalse(HDCNativeBannerLoader.IsRunning);
            Assert.AreEqual(0, HDCAdsTracker.Find(HDCAdFormat.Popup, "pu_nb_a")?.Requests ?? 0, "nothing loads before the scene opens");
            CollectionAssert.IsEmpty(Shown());
            yield return new ExitPlayMode();
        }

        private HDCNativeBannerLoader Start(string config)
        {
            HDCAdsSdk.AdEvent += adEvent =>
            {
                if (adEvent.format == HDCAdFormat.Popup)
                    events.Add(adEvent.type + " " + adEvent.id);
            };
            HDCCustomConfig.Declare(new Dictionary<string, string> { { HDCOnboardingKeys.FirstNativeBannerConfig, config } });
            HDCAds.Initialize(Ads, Core);

            var canvas = new GameObject("Canvas", typeof(Canvas)).GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var area = new GameObject("Banner Area", typeof(RectTransform)).GetComponent<RectTransform>();
            area.SetParent(canvas.transform, false);
            area.anchorMin = Vector2.zero;
            area.anchorMax = new Vector2(1f, 0.15f);
            area.offsetMin = area.offsetMax = Vector2.zero;

            var root = new GameObject("HDCNativeBanner");
            root.SetActive(false);
            var loader = root.AddComponent<HDCNativeBannerLoader>();
            var serialized = new SerializedObject(loader);
            serialized.FindProperty("bannerArea").objectReferenceValue = area;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            root.SetActive(true);
            return loader;
        }

        private List<string> Shown() =>
            events.Where(value => value.StartsWith("Shown ")).Select(value => value.Substring("Shown ".Length)).ToList();

        private static string Config(params string[] loops) =>
            "{\"activeSceneName\":\"" + SceneManager.GetActiveScene().name + "\",\"bannerLoops\":[" + string.Join(",", loops) + "]}";

        private static string Loop(string group, int showCount, float seconds, float timeOut = 5f) =>
            "{\"isEnabled\":true,\"groupName\":\"" + group + "\",\"timeOut\":" + Number(timeOut) + ",\"adShowCount\":" + showCount +
            ",\"defaultTimeConfig\":" + Number(seconds) + ",\"metaTimeConfig\":" + Number(seconds) + "}";

        private static string Number(float value) => value.ToString(CultureInfo.InvariantCulture);
    }
}
