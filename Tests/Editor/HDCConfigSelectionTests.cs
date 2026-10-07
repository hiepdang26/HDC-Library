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
        public void OnADeviceAValueComesFromRemoteThenSavedThenDefaultAndIsSaved()
        {
            var remote = new FakeRemote { { AdsKey, RemoteAds }, { "k1", "r1" } };
            HDCConfigInputs inputs = Inputs(HDCConfigMode.RemoteConfig, remote);
            inputs.Saved = key => key == "core_a" ? "saved core" : "";
            inputs.SavedCustom = key => key == "k2" ? "s2" : "";

            HDCConfigChoice choice = Select(inputs);

            Assert.AreEqual(RemoteAds, choice.Ads);
            Assert.AreEqual("core_a", choice.CoreKey);
            Assert.AreEqual("saved core", choice.Core);
            CollectionAssert.AreEqual(new[] { Pair(AdsKey, RemoteAds), Pair("core_a", "saved core") }, choice.Saves);
            Assert.AreEqual(HDCConfigSource.Remote, HDCConfigReport.Find(AdsKey).Source);
            Assert.AreEqual(FakeRemote.Origin, HDCConfigReport.Find(AdsKey).RemoteOrigin);
            Assert.AreEqual(HDCConfigSource.Saved, HDCConfigReport.Find("core_a").Source);
            Assert.AreEqual("default core", HDCConfigReport.Find("core_a").Default);

            Assert.AreEqual(("r1", HDCConfigSource.Remote), choice.Custom["k1"]);
            Assert.AreEqual(("s2", HDCConfigSource.Saved), choice.Custom["k2"]);
            Assert.AreEqual(("d3", HDCConfigSource.Default), choice.Custom["k3"]);
            Assert.IsTrue(choice.SaveCustom);
            Assert.IsFalse(choice.Country.IsOn);
            Assert.AreSame(choice.Country, HDCConfigReport.Country);
        }

        [Test]
        public void OnADeviceWithoutFirebaseTheSavedValuesWin()
        {
            HDCConfigInputs inputs = Inputs(HDCConfigMode.RemoteConfig, null);
            inputs.Saved = key => key == AdsKey ? RemoteAds : "";

            HDCConfigChoice choice = Select(inputs);

            Assert.AreEqual(RemoteAds, choice.Ads);
            Assert.AreEqual("default core", choice.Core);
            Assert.AreEqual("Firebase not ready", HDCConfigReport.Find(AdsKey).RemoteOrigin);
            Assert.AreEqual(HDCConfigSource.Saved, HDCConfigReport.Find(AdsKey).Source);
            Assert.AreEqual(HDCConfigSource.Default, HDCConfigReport.Find("core_a").Source);
            Assert.AreEqual(2, choice.Saves.Count, "a device keeps what it used for the next launch");
        }

        [Test]
        public void TheEditorUsesOnlyTheDefaultsAndSavesNothing()
        {
            HDCConfigInputs inputs = Inputs(HDCConfigMode.EditorDefaults, new FakeRemote { { AdsKey, RemoteAds }, { "k1", "r1" } });
            inputs.Saved = key => "saved";
            inputs.SavedCustom = key => "saved";

            HDCConfigChoice choice = Select(inputs);

            Assert.AreEqual(DefaultAds, choice.Ads);
            Assert.AreEqual("default core", choice.Core);
            Assert.AreEqual("Not fetched in the Editor", HDCConfigReport.Find(AdsKey).RemoteOrigin);
            Assert.AreEqual("saved", HDCConfigReport.Find(AdsKey).Saved, "the report still shows the saved value");
            Assert.IsEmpty(choice.Saves);
            Assert.IsTrue(choice.Custom.Values.All(custom => custom.Source == HDCConfigSource.Default));
            Assert.IsFalse(choice.SaveCustom);
        }

        [Test]
        public void ABuildWithoutRemoteConfigUsesTheProjectConfigsAndSavedCustomKeys()
        {
            HDCConfigInputs inputs = Inputs(HDCConfigMode.NoRemoteConfig, null);
            inputs.DefaultAds = "{}";
            inputs.Saved = key => "saved";
            inputs.SavedCustom = key => key == "k1" ? "s1" : "";

            HDCConfigChoice choice = Select(inputs);

            Assert.AreEqual("{}", choice.Ads);
            Assert.AreEqual("project core", choice.Core, "the project core config, whatever ads_config names");
            Assert.IsNull(HDCConfigReport.Find(AdsKey), "nothing was fetched, so nothing goes to the report");
            Assert.IsEmpty(choice.Saves);
            Assert.AreEqual(("s1", HDCConfigSource.Saved), choice.Custom["k1"]);
            Assert.AreEqual(("d2", HDCConfigSource.Default), choice.Custom["k2"]);
            Assert.IsFalse(choice.SaveCustom);
        }

        [Test]
        public void CountryModeReplacesTheConfigsAndCustomKeysButSavesWhatRemoteConfigGave()
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
            CollectionAssert.AreEqual(new[] { Pair(AdsKey, RemoteAds), Pair("core_a", "remote core") }, choice.Saves);
            CollectionAssert.AreEquivalent(new[] { "k1", "k2" }, choice.Custom.Keys);
            Assert.AreEqual(("c1", HDCConfigSource.Country), choice.Custom["k1"]);
            Assert.IsFalse(choice.SaveCustom);
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
            Assert.IsNull(HDCConfigReport.Find(AdsKey));
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
            Assert.AreEqual(HDCCustomConfig.SavedSource, HDCConfigSource.Saved.ToString());
            Assert.AreEqual(HDCCustomConfig.DefaultSource, HDCConfigSource.Default.ToString());
            Assert.AreEqual(HDCCustomConfig.CountrySource, HDCConfigSource.Country.ToString());

            var choice = new HDCConfigChoice();
            choice.Custom["k"] = ("v", HDCConfigSource.Saved);
            Assert.AreEqual(("v", HDCCustomConfig.SavedSource), choice.CustomValues()["k"]);
        }

        private HDCConfigChoice Select(HDCConfigInputs inputs) => new HDCConfigSelection(new HDCCountryMode(region, log)).Select(inputs);

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

        private static KeyValuePair<string, string> Pair(string key, string value) => new KeyValuePair<string, string>(key, value);

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

            public string Read(string key, out string origin)
            {
                origin = ContainsKey(key) ? Origin : "Static";
                return TryGetValue(key, out string value) ? value : "";
            }
        }
    }
}
