using System.Collections;
using System.Collections.Generic;
using HDC.Ads.Diagnostics;
using HDC.Ads.Logic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace HDC.Ads.Tests
{
    public class HDCConfigLoadTests : HDCPlayModeTest
    {
        private const string Ads = "{\"selectedAdCoreName\":\"core\"}";

        private HDCAdsSettings settings;

        [TearDown]
        public void DropSettings()
        {
            HDCAdsSettings.Override = null;
            HDCCustomConfig.Reset();
            if (settings != null)
                Object.DestroyImmediate(settings);
            settings = null;
        }

        [Test]
        public void AFrameLongerThanOneSecondCountsAsOneSecond()
        {
            var clock = new HDCLoadClock(30f);

            Assert.IsFalse(clock.Tick(89f, false), "a system prompt or the background stops the game, so it does not use up the timeout");
            Assert.AreEqual(HDCLoadClock.LongestCountedFrame, clock.Running);
            Assert.IsFalse(clock.Tick(-1f, false));
            Assert.AreEqual(HDCLoadClock.LongestCountedFrame, clock.Running);
        }

        [Test]
        public void TheLoadTimesOutAfterItsTimeoutOfPlayTime()
        {
            var clock = new HDCLoadClock(HDCAdsSettings.RemoteConfigTimeoutSeconds);
            for (int frame = 0; frame < 29; frame++)
                Assert.IsFalse(clock.Tick(1f, false));

            Assert.IsTrue(clock.Tick(1f, false));
            Assert.AreEqual(30f, clock.Running, 0.001f);
        }

        [Test]
        public void ActivationGetsAShortGraceAfterTheTimeout()
        {
            var clock = new HDCLoadClock(2f);
            Assert.IsFalse(clock.Tick(1f, true));
            Assert.IsFalse(clock.Tick(1f, true), "the values already came, so they are not dropped at the timeout");
            for (int frame = 0; frame < 4; frame++)
                Assert.IsFalse(clock.Tick(1f, true));

            Assert.IsTrue(clock.Tick(1f, true));
        }

#if HDC_FIREBASE
        [UnityTest]
        public IEnumerator ASecondLoadWaitsForTheRunningLoadInsteadOfFetchingAgain()
        {
            yield return new EnterPlayMode();
            var loads = new List<(string Ads, string Core)>();
            var cores = new Dictionary<string, string> { { "core", "{}" } };
            HDCRemoteConfig.Fetch(Ads, cores, (ads, core) => loads.Add((ads, core)));
            System.DateTime started = HDCConfigReport.StartClock;
            HDCRemoteConfig.Fetch("{}", new Dictionary<string, string>(), (ads, core) => loads.Add((ads, core)));
            yield return WaitFor(() => loads.Count == 2, 5f);

            Assert.AreEqual((Ads, "{}"), loads[0]);
            Assert.AreEqual(loads[0], loads[1], "the second call gets what the running load chose");
            Assert.AreEqual(1, HDCConfigReport.JoinedLoads);
            Assert.AreEqual(started, HDCConfigReport.StartClock, "the report of the running load is kept");

            HDCRemoteConfig.Fetch("{}", new Dictionary<string, string>(), (ads, core) => loads.Add((ads, core)));
            yield return WaitFor(() => loads.Count == 3, 5f);
            Assert.AreEqual(loads[0], loads[2], "a call after the load gets the same configs");
            Assert.AreEqual(2, HDCConfigReport.JoinedLoads);
            yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator TheDefaultSourceNeverTouchesFirebase()
        {
            yield return new EnterPlayMode();
            settings = ScriptableObject.CreateInstance<HDCAdsSettings>();
            var serialized = new SerializedObject(settings);
            foreach (string name in new[] { "configSourceAndroid", "configSourceIos" })
                serialized.FindProperty(name).intValue = (int)HDCAdsConfigSource.Default;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            HDCAdsSettings.Override = settings;
            Assert.AreEqual(HDCAdsConfigSource.Default, settings.ConfigSource);

            HDCRemoteConfig.FetchInEditor = true;
            try
            {
                string loaded = null;
                HDCRemoteConfig.Fetch(Ads, new Dictionary<string, string> { { "core", "{\"forceAdGroups\":[]}" } }, (ads, core) => loaded = core);
                yield return WaitFor(() => loaded != null, 5f);

                Assert.AreEqual("{\"forceAdGroups\":[]}", loaded);
                Assert.AreEqual(HDCConfigPlan.Default, HDCConfigReport.Plan);
                Assert.IsTrue(HDCConfigReport.DefaultsOnly);
                Assert.AreEqual("chosen in HDC Ads Settings", HDCConfigReport.Outcome);
                Assert.AreEqual("Not used: HDC Ads Settings uses Default", HDCConfigReport.Firebase);
                Assert.AreEqual("", HDCConfigReport.FetchStatus, "no fetch was made");
                Assert.AreEqual(HDCConfigSource.Default, HDCConfigReport.Find(HDCRemoteConfig.AdsConfigKey).Source);
                Assert.AreEqual(HDCConfigSource.Default, HDCConfigReport.Find("core").Source);
            }
            finally
            {
                HDCRemoteConfig.FetchInEditor = false;
            }

            yield return new ExitPlayMode();
        }
#endif
    }
}
