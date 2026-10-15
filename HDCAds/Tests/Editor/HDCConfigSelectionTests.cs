using System.Collections.Generic;
using System.Linq;
using HDC.Ads.Diagnostics;
using HDC.Ads.Domain;
using HDC.Ads.Logic;
using HDC.Ads.Ports;
using NUnit.Framework;

namespace HDC.Ads.Tests
{
    public class HDCConfigSelectionTests
    {
        private const string AdsKey = HDCConfigSelection.AdsConfigKey;
        private const string RemoteAds = "{\"selectedAdCoreName\":\"core_a\"}";
        private const string DefaultAds = "{\"selectedAdCoreName\":\"core_a\",\"default\":true}";
        private const string CountryAds = "{\"selectedAdCoreName\":\"core_vn\"}";
        private const string VietnamRules = "{\"enabled\":true,\"targetCountry\":\"vn\",\"matchRegion\":true}";

        private FakeRegion region;
        private HDCFakeLog log;

        [SetUp]
        public void Clean()
        {
            HDCConfigReport.Reset();
            region = new FakeRegion();
            log = new HDCFakeLog();
        }

        [TearDown]
        public void CleanUp() => HDCConfigReport.Reset();

        [Test]
        public void OnADeviceAValueComesFromThisLaunchsFetchThenTheDefault()
        {
            var remote = new FakeRemote { { AdsKey, RemoteAds }, { "k1", "r1" } };

            HDCConfigChoice choice = Select(Inputs(HDCConfigMode.RemoteConfig, remote));

            Assert.AreEqual(RemoteAds, choice.Ads);
            Assert.AreEqual("core_a", choice.CoreKey);
            Assert.AreEqual("default core", choice.Core, "core_a has no Remote Config value");
            Assert.AreEqual(HDCConfigSource.Remote, HDCConfigReport.Find(AdsKey).Source);
            Assert.AreEqual(FakeRemote.Origin, HDCConfigReport.Find(AdsKey).RemoteOrigin);
            Assert.AreEqual(HDCConfigSource.Default, HDCConfigReport.Find("core_a").Source);
            Assert.AreEqual("Static", HDCConfigReport.Find("core_a").RemoteOrigin);
            Assert.AreEqual("default core", HDCConfigReport.Find("core_a").Default);

            Assert.AreEqual(("r1", HDCConfigSource.Remote), choice.Custom["k1"]);
            Assert.AreEqual(("d2", HDCConfigSource.Default), choice.Custom["k2"]);
            Assert.AreEqual(("d3", HDCConfigSource.Default), choice.Custom["k3"]);
            Assert.IsFalse(choice.Country.IsOn);
            Assert.AreSame(choice.Country, HDCConfigReport.Country);
        }

        [Test]
        public void WithoutAFetchInThisLaunchEveryKeyUsesItsDefault()
        {
            HDCConfigInputs inputs = Inputs(HDCConfigMode.RemoteConfig, null);
            inputs.RemoteProblem = "timeout";

            HDCConfigChoice choice = Select(inputs);

            Assert.AreEqual(DefaultAds, choice.Ads);
            Assert.AreEqual("default core", choice.Core);
            Assert.AreEqual("Not used: timeout", HDCConfigReport.Find(AdsKey).RemoteOrigin);
            Assert.AreEqual(HDCConfigSource.Default, HDCConfigReport.Find(AdsKey).Source);
            Assert.AreEqual(HDCConfigSource.Default, HDCConfigReport.Find("core_a").Source);
            Assert.IsTrue(choice.Custom.Values.All(custom => custom.Source == HDCConfigSource.Default));
        }

        [Test]
        public void TheDefaultSourceNeverReadsRemoteConfig()
        {
            var remote = new FakeRemote { { AdsKey, RemoteAds }, { "k1", "r1" }, { HDCCountryMode.DevicesKey, "{\"debugDevices\":[\"device-1\"]}" } };

            HDCConfigChoice choice = Select(Inputs(HDCConfigMode.Defaults, remote));

            Assert.AreEqual(DefaultAds, choice.Ads);
            Assert.AreEqual("default core", choice.Core);
            Assert.AreEqual("Not fetched: HDC Ads Settings uses Default", HDCConfigReport.Find(AdsKey).RemoteOrigin);
            Assert.AreEqual(0, remote.Reads);
            Assert.IsTrue(choice.Custom.Values.All(custom => custom.Source == HDCConfigSource.Default));
        }

        [Test]
        public void TheEditorUsesOnlyTheDefaults()
        {
            HDCConfigInputs inputs = Inputs(HDCConfigMode.EditorDefaults, new FakeRemote { { AdsKey, RemoteAds }, { "k1", "r1" } });

            HDCConfigChoice choice = Select(inputs);

            Assert.AreEqual(DefaultAds, choice.Ads);
            Assert.AreEqual("default core", choice.Core);
            Assert.AreEqual("Not fetched in the Editor", HDCConfigReport.Find(AdsKey).RemoteOrigin);
            Assert.IsTrue(choice.Custom.Values.All(custom => custom.Source == HDCConfigSource.Default));
        }

        [Test]
        public void ABuildWithoutRemoteConfigUsesTheProjectConfigs()
        {
            HDCConfigInputs inputs = Inputs(HDCConfigMode.NoRemoteConfig, null);
            inputs.DefaultAds = "{}";

            HDCConfigChoice choice = Select(inputs);

            Assert.AreEqual("{}", choice.Ads);
            Assert.AreEqual("project core", choice.Core, "the project core config, whatever ads_config names");
            Assert.AreEqual(HDCConfigSource.Default, HDCConfigReport.Find(AdsKey).Source, "the report says the project config runs, so the debug panel does not call it Direct");
            Assert.AreEqual("Not in this build: no Firebase", HDCConfigReport.Find(AdsKey).RemoteOrigin);
            Assert.AreEqual("", choice.CoreKey, "an ads_config without selectedAdCoreName selects no ad core");
            Assert.AreEqual(("d1", HDCConfigSource.Default), choice.Custom["k1"]);
            Assert.AreEqual(("d2", HDCConfigSource.Default), choice.Custom["k2"]);
        }

        [Test]
        public void CountryModeReplacesTheConfigsAndCustomKeys()
        {
            region.Region = new HDCDeviceRegion { Regions = new[] { "vn" } };
            HDCConfigInputs inputs = Inputs(HDCConfigMode.RemoteConfig, new FakeRemote { { AdsKey, RemoteAds }, { "core_a", "remote core" }, { "k1", "r1" } });
            inputs.CountryRules = VietnamRules;
            inputs.CountryAds = CountryAds;
            inputs.CountryCore = "country core";
            inputs.CustomCountry = new Dictionary<string, string> { { "k1", "c1" }, { "k2", "d2" }, { "undeclared", "x" } };

            HDCConfigChoice choice = Select(inputs);

            Assert.IsTrue(choice.Country.IsOn);
            Assert.AreEqual("Locale region VN", choice.Country.Reason);
            Assert.AreEqual(CountryAds, choice.Ads);
            Assert.AreEqual("core_vn", choice.CoreKey);
            Assert.AreEqual("country core", choice.Core);
            Assert.AreEqual(HDCConfigSource.Country, HDCConfigReport.Find(AdsKey).Source);
            Assert.AreEqual(HDCConfigSource.Country, HDCConfigReport.Find("core_vn").Source);
            Assert.AreEqual(HDCConfigSource.Remote, HDCConfigReport.Find("core_a").Source);
            CollectionAssert.AreEquivalent(new[] { "k1", "k2" }, choice.Custom.Keys);
            Assert.AreEqual(("c1", HDCConfigSource.Country), choice.Custom["k1"]);
            StringAssert.Contains("country mode on: Locale region VN", log.Lines.Single());
        }

        [Test]
        public void CountryModeWithoutCountryConfigsKeepsTheChosenConfigs()
        {
            region.Region = new HDCDeviceRegion { Regions = new[] { "vn" } };
            HDCConfigInputs inputs = Inputs(HDCConfigMode.RemoteConfig, new FakeRemote { { AdsKey, RemoteAds } });
            inputs.CountryRules = VietnamRules;

            HDCConfigChoice choice = Select(inputs);

            Assert.IsTrue(choice.Country.IsOn);
            Assert.AreEqual(RemoteAds, choice.Ads);
            Assert.AreEqual("default core", choice.Core);
            Assert.AreEqual(HDCConfigSource.Remote, HDCConfigReport.Find(AdsKey).Source);
        }

        [Test]
        public void ARemoteConfigDebugDeviceNeverGetsCountryMode()
        {
            region.Region = new HDCDeviceRegion { Regions = new[] { "vn" } };
            HDCConfigInputs inputs = Inputs(HDCConfigMode.RemoteConfig,
                new FakeRemote { { AdsKey, RemoteAds }, { HDCCountryMode.DevicesKey, "{\"debugDevices\":[\" DEVICE-1 \"]}" } });
            inputs.CountryRules = VietnamRules;
            inputs.CountryAds = CountryAds;

            HDCConfigChoice choice = Select(inputs);

            Assert.IsFalse(choice.Country.IsOn);
            Assert.AreEqual("Debug device", choice.Country.Reason);
            Assert.AreEqual(RemoteAds, choice.Ads);
        }

        [Test]
        public void ABuildWithoutRemoteConfigTakesTheCountryConfigsToo()
        {
            region.Region = new HDCDeviceRegion { Regions = new[] { "vn" } };
            HDCConfigInputs inputs = Inputs(HDCConfigMode.NoRemoteConfig, null);
            inputs.CountryRules = VietnamRules;
            inputs.CountryAds = CountryAds;
            inputs.CountryCore = "country core";

            HDCConfigChoice choice = Select(inputs);

            Assert.AreEqual(CountryAds, choice.Ads);
            Assert.AreEqual("country core", choice.Core);
            Assert.AreEqual("core_vn", choice.CoreKey);
            Assert.AreEqual(HDCConfigSource.Country, HDCConfigReport.Find(AdsKey).Source);
            Assert.AreEqual(HDCConfigSource.Country, HDCConfigReport.Find("core_vn").Source);
        }

        [Test]
        public void TheEditorGetsCountryModeOnlyWhenSimulated()
        {
            region.IsEditor = true;
            region.Region = new HDCDeviceRegion { Regions = new[] { "vn" } };
            HDCConfigInputs inputs = Inputs(HDCConfigMode.EditorDefaults, null);
            inputs.CountryRules = VietnamRules;
            Assert.IsFalse(Select(inputs).Country.IsOn);
            Assert.AreEqual(0, region.Reads, "the Editor does not read the device unless it simulates the country");

            inputs.CountryRules = "{\"enabled\":true,\"targetCountry\":\"vn\",\"simulateInEditor\":true}";
            HDCConfigChoice simulated = Select(inputs);
            Assert.IsTrue(simulated.Country.IsOn);
            Assert.AreEqual("Simulated in the Editor", simulated.Country.Reason);
        }

        [Test]
        public void CustomSourcesKeepTheNamesOfTheCustomConfig()
        {
            Assert.AreEqual(HDCCustomConfig.RemoteSource, HDCConfigSource.Remote.ToString());
            Assert.AreEqual(HDCCustomConfig.DefaultSource, HDCConfigSource.Default.ToString());
            Assert.AreEqual(HDCCustomConfig.CountrySource, HDCConfigSource.Country.ToString());

            var choice = new HDCConfigChoice();
            choice.Custom["k"] = ("v", HDCConfigSource.Country);
            Assert.AreEqual(("v", HDCCustomConfig.CountrySource), choice.CustomValues()["k"]);
        }

        private HDCConfigChoice Select(HDCConfigInputs inputs) => new HDCConfigSelection(new HDCCountryMode(region, log), log).Select(inputs);

        private static HDCConfigInputs Inputs(HDCConfigMode mode, IRemoteConfigValues remote) =>
            new HDCConfigInputs
            {
                Mode = mode,
                Remote = remote,
                DefaultAds = DefaultAds,
                DefaultCores = new Dictionary<string, string> { { "core_a", "default core" }, { "core_vn", "default vn core" } },
                DefaultCore = "project core",
                CustomDefaults = new Dictionary<string, string> { { "k1", "d1" }, { "k2", "d2" }, { "k3", "d3" } },
            };

        private sealed class FakeRegion : IDeviceRegionSource
        {
            internal HDCDeviceRegion Region { get; set; } = new HDCDeviceRegion();

            internal int Reads { get; private set; }

            public bool IsEditor { get; set; }

            public string DeviceId => "device-1";

            public HDCDeviceRegion Read()
            {
                Reads++;
                return Region;
            }
        }

        private sealed class FakeRemote : Dictionary<string, string>, IRemoteConfigValues
        {
            internal const string Origin = "Remote";

            internal int Reads { get; private set; }

            public string Read(string key, out string origin)
            {
                Reads++;
                origin = ContainsKey(key) ? Origin : "Static";
                return TryGetValue(key, out string value) ? value : "";
            }
        }
    }
}
