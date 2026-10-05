using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HDC.Ads.Diagnostics;
using HDC.Ads.Domain;
using HDC.Ads.Infrastructure;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace HDC.Ads.Tests
{
    public class HDCNativeAfterInterstitialTests : HDCPlayModeTest
    {
        private const string Ads = @"{ ""selectedAdCoreName"": ""core"",
  ""forceAdChannel"": { ""isEnabled"": true, ""positionConfigs"": [ { ""positionName"": ""ui"", ""canShow"": true, ""cappingTime"": 0 } ] } }";

        private const string Core = @"{
  ""forceAdLayoutConfig"": { ""layoutGroups"": [ { ""groupName"": ""naf"", ""layouts"": [ { ""layout"": ""fs_single_cls_01"", ""layoutTime"": 5 } ] } ] },
  ""forceAdGroups"": [ { ""mediationPriority"": 1, ""groupName"": ""inter"", ""positionNames"": [ ""ui"" ],
      ""androidUnit"": { ""id"": ""inter-ok"", ""androidInterstitials"": { ""switchToInterstitialAndroid"": true,
        ""useNativeAfterInterstitial"": true, ""nativeAfterInterstitialId"": ""naf-ok"", ""nativeAfterInterstitialLayout"": ""naf"" } } } ] }";

        private const string OneShowCore = @"{
  ""forceAdLayoutConfig"": { ""layoutGroups"": [ { ""groupName"": ""naf"", ""layouts"": [ { ""layout"": ""fs_single_cls_01"", ""layoutTime"": 5 } ] } ] },
  ""forceAdGroups"": [ { ""mediationPriority"": 1, ""groupName"": ""inter"", ""positionNames"": [ ""ui"" ], ""maxShowCount"": 1,
      ""androidUnit"": { ""id"": ""inter-ok"", ""androidInterstitials"": { ""switchToInterstitialAndroid"": true,
        ""useNativeAfterInterstitial"": true, ""nativeAfterInterstitialId"": ""naf-ok"", ""nativeAfterInterstitialLayout"": ""naf"" } } } ] }";

        private const string InterstitialId = "fa_interstitial_inter";
        private const string NativeId = "fa_naf_inter";

        [TearDown]
        public void ForgetFollowers() => HDCShowFollowers.Reset();

        [Test]
        public void AnArmedNativeShowsWhenItsInterstitialShows()
        {
            var calls = new List<string>();
            HDCShowFollowers.Arm(InterstitialId, "{\"id\":\"" + NativeId + "\"}");

            HDCShowFollowers.OnNativeEvent(Event(HDCAdFormat.Interstitial, "Loaded", InterstitialId), Recorder(calls, true));
            HDCShowFollowers.OnNativeEvent(Event(HDCAdFormat.Fullscreen, "Shown", InterstitialId), Recorder(calls, true));
            Assert.IsEmpty(calls, "only the interstitial's Shown starts the native");

            HDCShowFollowers.OnNativeEvent(Event(HDCAdFormat.Interstitial, "Shown", InterstitialId), Recorder(calls, true));
            HDCShowFollowers.OnNativeEvent(Event(HDCAdFormat.Interstitial, "Shown", InterstitialId), Recorder(calls, true));

            CollectionAssert.AreEqual(new[] { "fullscreen.show {\"id\":\"" + NativeId + "\"}" }, calls, "once per arm");
            Assert.IsTrue(HDCShowFollowers.Take(InterstitialId));
            Assert.IsFalse(HDCShowFollowers.Take(InterstitialId));
        }

        [Test]
        public void ANativeThatDidNotStartIsLeftToTheMainThread()
        {
            var calls = new List<string>();
            HDCShowFollowers.Arm(InterstitialId, "{\"id\":\"" + NativeId + "\"}");

            HDCShowFollowers.OnNativeEvent(Event(HDCAdFormat.Interstitial, "Shown", InterstitialId), Recorder(calls, false));

            Assert.AreEqual(1, calls.Count);
            Assert.IsFalse(HDCShowFollowers.Take(InterstitialId));
        }

        [Test]
        public void ConfigCheckExplainsANativeAfterInterstitialThatCannotRun()
        {
            const string core = @"{ ""forceAdLayoutConfig"": { ""layoutGroups"": [] }, ""forceAdGroups"": [
    { ""groupName"": ""off"", ""positionNames"": [ ""ui"" ], ""androidUnit"": { ""id"": ""a"", ""layoutGroupName"": ""x"",
        ""androidInterstitials"": { ""useNativeAfterInterstitial"": true, ""nativeAfterInterstitialId"": ""n"" } } },
    { ""groupName"": ""noid"", ""positionNames"": [ ""ui"" ], ""androidUnit"": { ""id"": ""b"",
        ""androidInterstitials"": { ""switchToInterstitialAndroid"": true, ""useNativeAfterInterstitial"": true } } },
    { ""groupName"": ""nolayout"", ""positionNames"": [ ""ui"" ], ""androidUnit"": { ""id"": ""c"",
        ""androidInterstitials"": { ""switchToInterstitialAndroid"": true, ""useNativeAfterInterstitial"": true,
          ""nativeAfterInterstitialId"": ""n"", ""nativeAfterInterstitialLayout"": ""missing"" } } } ] }";
            var ads = new HDCFakeAds();
            List<string> findings = ads.Channels.All
                .SelectMany(channel => channel.ConfigRule.Check(HDCAdsConfig.Parse(Ads), HDCAdCoreConfig.Parse(core)))
                .Select(finding => finding.Text)
                .ToList();
            string all = string.Join("\n", findings);

            Assert.IsTrue(findings.Any(text => text.Contains("'off'") && text.Contains("switchToInterstitialAndroid")), all);
            Assert.IsTrue(findings.Any(text => text.Contains("'noid'") && text.Contains("nativeAfterInterstitialId")), all);
            Assert.IsTrue(findings.Any(text => text.Contains("'nolayout'") && text.Contains("'missing'")), all);
        }

        [UnityTest]
        public IEnumerator TheNativeShowsUnderTheInterstitialOnceItHasLoaded()
        {
#if !UNITY_ANDROID
            Assert.Ignore("Native after interstitial runs on Android only, as in the old system: switch the build target to Android.");
#endif
            yield return new EnterPlayMode();
            var events = new List<string>();
            var revenues = new List<HDCAdRevenue>();
            HDCAdsSdk.AdEvent += adEvent => events.Add(adEvent.format + " " + adEvent.type + " " + adEvent.id);
            bool ready = false;
            HDCAds.Initialize(Ads, Core, () => ready = true);
            HDCAds.Revenue += revenues.Add;
            yield return WaitFor(() => ready, 10f);
            HDCAds.ForceAd.Initialize("inter");
            yield return WaitFor(() => HDCAds.ForceAd.CanShow("ui"), 5f);

            Assert.IsTrue(HDCAds.ForceAd.Show("ui"));
            yield return WaitFor(() => events.Contains("fullscreen Shown " + NativeId), 5f);
            Assert.Less(events.IndexOf("interstitial Shown " + InterstitialId), events.IndexOf("fullscreen Loaded " + NativeId),
                "the first native loads only once its interstitial has shown");
            Assert.IsTrue(revenues.Any(revenue => revenue.Format == HDCAdFormat.Fullscreen && revenue.Channel == HDCAdChannel.ForceAd && revenue.Position == "ui"),
                "the native's revenue counts for the force ad position");

            yield return WaitFor(() => events.Count(e => e == "fullscreen Loaded " + NativeId) == 2 && HDCAds.ForceAd.CanShow("ui"), 5f);
            int second = events.Count;
            Assert.IsTrue(HDCAds.ForceAd.Show("ui"));
            yield return WaitFor(() => events.Skip(second).Contains("interstitial Closed " + InterstitialId), 5f);

            List<string> cycle = events.Skip(second).ToList();
            Assert.AreEqual(cycle.IndexOf("interstitial Shown " + InterstitialId) + 1, cycle.IndexOf("fullscreen Shown " + NativeId),
                "a loaded native shows the moment its interstitial does, under it: " + string.Join(", ", cycle));
            yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator AGroupOutOfShowsLetsItsNativeFinish()
        {
#if !UNITY_ANDROID
            Assert.Ignore("Native after interstitial runs on Android only, as in the old system: switch the build target to Android.");
#endif
            yield return new EnterPlayMode();
            var times = new Dictionary<string, float>();
            HDCAdsSdk.AdEvent += adEvent => times[adEvent.format + " " + adEvent.type + " " + adEvent.id] = Time.realtimeSinceStartup;
            bool ready = false;
            HDCAds.Initialize(Ads, OneShowCore, () => ready = true);
            yield return WaitFor(() => ready, 10f);
            HDCAds.ForceAd.Initialize("inter");
            yield return WaitFor(() => HDCAds.ForceAd.CanShow("ui"), 5f);

            Assert.IsTrue(HDCAds.ForceAd.Show("ui"));
            yield return WaitFor(() => times.ContainsKey("fullscreen Closed " + NativeId) && times.ContainsKey("interstitial Closed " + InterstitialId), 5f);

            Assert.Greater(times["fullscreen Closed " + NativeId] - times["interstitial Closed " + InterstitialId], 0.3f,
                "the native closes on its own time, not with the group");
            yield return WaitFor(() => HDCAdsTracker.Find(HDCAdFormat.Fullscreen, NativeId)?.State == HDCAdState.Destroyed, 3f);
            yield return new ExitPlayMode();
        }

        private static string Event(string format, string type, string id) =>
            JsonUtility.ToJson(new HDCAdEvent { id = id, format = format, type = type });

        private static System.Func<string, string, string> Recorder(List<string> calls, bool started) =>
            (method, argsJson) =>
            {
                calls.Add(method + " " + argsJson);
                return started ? "{\"ok\":true,\"value\":true}" : "{\"ok\":true,\"value\":false}";
            };
    }
}
