using System;
using System.Collections.Generic;
using System.Reflection;
using HDC.Ads.DebugUI;
using HDC.Ads.Diagnostics;
using HDC.Ads.Domain;
using HDC.Ads.Logic;
using HDC.Ads.Ports;
using NUnit.Framework;

namespace HDC.Ads.Tests
{
    public class HDCAppliedConfigTests
    {
        private const string Core = "adcore_test";
        private const string Ads = "{\"selectedAdCoreName\":\"adcore_test\"}";
        private const string CoreJson = "{\"forceAdGroups\":[]}";

        [SetUp]
        public void Clean() => HDCConfigReport.Reset();

        [TearDown]
        public void CleanUp() => HDCConfigReport.Reset();

        [Test]
        public void TheAppliedSourceIsWhatTheLoadChose()
        {
            Loaded(HDCConfigSource.Remote, Ads);

            HDCConfigReport.Applied(Ads, CoreJson);

            Assert.AreEqual(HDCConfigSource.Remote, HDCConfigReport.AppliedAdsSource);
            Assert.AreEqual(Core, HDCConfigReport.AppliedCoreKey);
            Assert.AreEqual(HDCConfigSource.Remote, HDCConfigReport.AppliedCoreSource);
        }

        [Test]
        public void ALaterLoadDoesNotChangeTheAppliedSource()
        {
            Loaded(HDCConfigSource.Default, Ads);
            HDCConfigReport.Applied(Ads, CoreJson);

            Loaded(HDCConfigSource.Remote, "{\"selectedAdCoreName\":\"adcore_test\",\"newer\":1}");

            Assert.AreEqual(HDCConfigSource.Default, HDCConfigReport.AppliedAdsSource, "the label follows the config HDCAds runs on, not the newest load");
            Assert.AreEqual(HDCConfigSource.Default, HDCConfigReport.AppliedCoreSource);
        }

        [Test]
        public void AConfigGivenStraightToInitializeIsDirect()
        {
            HDCConfigReport.Applied(Ads, CoreJson);

            Assert.IsNull(HDCConfigReport.AppliedAdsSource);
            Assert.IsNull(HDCConfigReport.AppliedCoreSource);
            Assert.AreEqual(Core, HDCConfigReport.AppliedCoreKey);
        }

        [Test]
        public void AnotherConfigThanTheLoadChoseIsDirect()
        {
            Loaded(HDCConfigSource.Remote, Ads);

            HDCConfigReport.Applied("{\"selectedAdCoreName\":\"adcore_test\",\"other\":2}", CoreJson);

            Assert.IsNull(HDCConfigReport.AppliedAdsSource);
            Assert.AreEqual(HDCConfigSource.Remote, HDCConfigReport.AppliedCoreSource, "each part is matched on its own");
        }

        [Test]
        public void SpacesInTheJsonDoNotMakeItDirect()
        {
            Loaded(HDCConfigSource.Remote, Ads);

            HDCConfigReport.Applied("{ \"selectedAdCoreName\" : \"adcore_test\" }", CoreJson);

            Assert.AreEqual(HDCConfigSource.Remote, HDCConfigReport.AppliedAdsSource);
        }

        [Test]
        public void AnAdsConfigWithoutACoreNameSelectsNoAdCore()
        {
            Loaded(HDCConfigSource.Default, "{}");

            HDCConfigReport.Applied("{}", "");

            Assert.AreEqual(HDCConfigSource.Default, HDCConfigReport.AppliedAdsSource);
            Assert.AreEqual("", HDCConfigReport.AppliedCoreKey);
            Assert.IsNull(HDCConfigReport.AppliedCoreSource);
        }

        [Test]
        public void ABuildWithoutFirebaseShowsItsDefaults()
        {
            HDCConfigReport.LoadStarted(HDCConfigPlan.NoFirebase, 0f);
            HDCConfigChoice choice = new HDCConfigSelection(new HDCCountryMode(new NoRegion(), new HDCFakeLog()), new HDCFakeLog()).Select(new HDCConfigInputs
            {
                Mode = HDCConfigMode.NoRemoteConfig,
                DefaultAds = Ads,
                DefaultCore = CoreJson,
                CustomDefaults = new Dictionary<string, string>(),
            });
            HDCConfigReport.LoadFinished("no Firebase in this build", choice.CoreKey, 0f);

            HDCConfigReport.Applied(choice.Ads, choice.Core);

            Assert.AreEqual("Not in this build", HDCConfigReport.Firebase);
            Assert.IsTrue(HDCConfigReport.DefaultsOnly);
            Assert.AreEqual(HDCConfigSource.Default, HDCConfigReport.AppliedAdsSource);
            Assert.AreEqual(HDCConfigSource.Default, HDCConfigReport.AppliedCoreSource);
        }

        [Test]
        public void ThePanelSaysNoAdCoreInsteadOfDirect()
        {
            Loaded(HDCConfigSource.Default, "{}");
            HDCConfigReport.Applied("{}", "");

            Assert.AreEqual("No ad core", Label("Core", out string reason));
            StringAssert.Contains("selectedAdCoreName", reason);
            Assert.AreEqual("Default", Label("Ads", out _));
        }

        [Test]
        public void ThePanelSaysDefaultForABuildWithoutFirebase()
        {
            ABuildWithoutFirebaseShowsItsDefaults();

            Assert.AreEqual("Default", Label("Ads", out string reason));
            StringAssert.Contains("không có Firebase", reason);
            Assert.AreEqual("Default", Label("Core", out _));
        }

        [Test]
        public void ThePanelKeepsTheAppliedLabelAfterALaterLoad()
        {
            Loaded(HDCConfigSource.Default, Ads);
            HDCConfigReport.Applied(Ads, CoreJson);
            Loaded(HDCConfigSource.Remote, "{\"selectedAdCoreName\":\"adcore_test\",\"newer\":1}");

            Assert.AreEqual("Default", Label("Ads", out _));
        }

        [Test]
        public void ThePanelNamesCountryConfigs()
        {
            Loaded(HDCConfigSource.Country, Ads);
            HDCConfigReport.Applied(Ads, CoreJson);

            Assert.AreEqual("Country", Label("Ads", out _));
            Assert.AreEqual("Country", Label("Core", out _));
        }

        [Test]
        public void ThePanelSaysDirectOnlyForAConfigGivenStraightToInitialize()
        {
            HDCConfigReport.Applied(Ads, CoreJson);

            Assert.AreEqual("Direct", Label("Ads", out string reason));
            StringAssert.Contains("truyền thẳng vào HDCAds.Initialize", reason);
        }

        private static string Label(string part, out string reason)
        {
            const BindingFlags statics = BindingFlags.NonPublic | BindingFlags.Static;
            Type partType = typeof(HDCRemoteConfigPage).GetNestedType("Part", BindingFlags.NonPublic);
            object[] arguments = { Enum.Parse(partType, part), null, null };
            var label = (string)typeof(HDCRemoteConfigPage).GetMethod("AppliedFrom", statics).Invoke(null, arguments);
            reason = (string)arguments[1];
            return label;
        }

        private static void Loaded(HDCConfigSource source, string ads)
        {
            HDCConfigReport.LoadStarted(HDCConfigPlan.RemoteConfig, 30f);
            Use(HDCConfigReport.AdsKey, ads, source);
            Use(Core, CoreJson, source);
            HDCConfigReport.LoadFinished(source == HDCConfigSource.Remote ? "fetched" : "timeout", Core, 1f);
        }

        private static void Use(string key, string value, HDCConfigSource source)
        {
            HDCConfigEntry entry = HDCConfigReport.Entry(key);
            entry.Used = value;
            entry.Source = source;
        }

        private sealed class NoRegion : IDeviceRegionSource
        {
            public bool IsEditor => false;

            public string DeviceId => "device-1";

            public HDCDeviceRegion Read() => new HDCDeviceRegion();
        }
    }
}
