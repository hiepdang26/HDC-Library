using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HDC.Ads.Diagnostics;
using HDC.Ads.Domain;
using HDC.Ads.Infrastructure;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace HDC.Ads.Tests
{
    public class HDCCountryTests : HDCPlayModeTest
    {
        private const string CustomKey = "hdc_test_country";

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
        public void ADeviceInTheCountryGetsCountryModeUnlessItIsADebugDevice()
        {
            HDCCountryRules rules = Rules();
            var region = new HDCDeviceRegion { SimCountry = "vn", TimezoneName = "Asia/Bangkok", UtcOffsetHours = 7 };

            HDCCountryResult on = HDCCountry.Decide(rules, "device-1", null, region, false);
            Assert.IsTrue(on.IsOn);
            Assert.AreEqual("SIM country VN", on.Reason);

            Assert.IsFalse(HDCCountry.Decide(rules, "device-1", new[] { " DEVICE-1 " }, region, false).IsOn, "a Remote Config debug device");
            rules.debugDevices = new[] { "device-1" };
            Assert.AreEqual("Debug device", HDCCountry.Decide(rules, "device-1", null, region, false).Reason);
        }

        [Test]
        public void EachSignalCountsAndUtcOffsetOnlyWhenListed()
        {
            HDCCountryRules rules = Rules();
            Assert.IsTrue(HDCCountry.Decide(rules, "d", null, new HDCDeviceRegion { SystemLanguage = "Vietnamese" }, false).IsOn);
            Assert.IsTrue(HDCCountry.Decide(rules, "d", null, new HDCDeviceRegion { LanguageCodes = new[] { "en", "vi" } }, false).IsOn);
            Assert.IsTrue(HDCCountry.Decide(rules, "d", null, new HDCDeviceRegion { Regions = new[] { "VN" } }, false).IsOn);
            Assert.IsTrue(HDCCountry.Decide(rules, "d", null, new HDCDeviceRegion { TimezoneName = "Asia/Ho_Chi_Minh" }, false).IsOn);
            Assert.IsTrue(HDCCountry.Decide(rules, "d", null, new HDCDeviceRegion { NetworkCountry = "vn" }, false).IsOn);

            var bangkok = new HDCDeviceRegion { TimezoneName = "Asia/Bangkok", UtcOffsetHours = 7, Regions = new[] { "th" }, LanguageCodes = new[] { "th" } };
            HDCCountryResult off = HDCCountry.Decide(rules, "d", null, bangkok, false);
            Assert.IsFalse(off.IsOn, "UTC+7 alone is also Thailand");
            Assert.AreEqual("No signal of VN", off.Reason);
            rules.utcOffsets = new[] { 7f };
            Assert.IsTrue(HDCCountry.Decide(rules, "d", null, bangkok, false).IsOn, "listed on purpose");
        }

        [Test]
        public void TheEditorAndAnOffCheckNeverGetCountryMode()
        {
            HDCCountryRules rules = Rules();
            var region = new HDCDeviceRegion { SimCountry = "vn" };
            Assert.IsFalse(HDCCountry.Decide(rules, "d", null, region, true).IsOn);
            rules.simulateInEditor = true;
            Assert.IsTrue(HDCCountry.Decide(rules, "d", null, new HDCDeviceRegion(), true).IsOn, "simulated in the Editor");
            rules.enabled = false;
            Assert.IsFalse(HDCCountry.Decide(rules, "d", null, region, false).IsOn);
        }

        [Test]
        public void RemoteDebugDevicesComeFromTheDevicesKey()
        {
            CollectionAssert.AreEqual(new[] { "a", "b" }, HDCCountry.RemoteDebugDevices("{\"debugDevices\":[\"a\",\" b \",\"\"]}"));
            Assert.IsEmpty(HDCCountry.RemoteDebugDevices("not json"));
            Assert.IsEmpty(HDCCountry.RemoteDebugDevices(""));
        }

        [Test]
        public void TheSettingsGiveCountryValuesWithPlatformFallbacks()
        {
            settings = Settings(enabled: true, simulate: false);

            Assert.AreEqual("{\"selectedAdCoreName\":\"core_vn\"}", settings.CountryAdsConfig);
            Dictionary<string, string> country = settings.CustomCountryValues();
            Assert.AreEqual("country value", country[CustomKey]);
            Assert.AreEqual("plain", country["hdc_test_plain"], "a key without a country value keeps its own value");
            Assert.IsTrue(HDCCountryRules.Parse(settings.CountryRulesJson()).enabled);
        }

#if HDC_FIREBASE
        [UnityTest]
        public IEnumerator CountryModeAppliesTheCountryConfigsInsteadOfRemoteConfig()
        {
            yield return new EnterPlayMode();
            settings = Settings(enabled: true, simulate: true);
            HDCAdsSettings.Override = settings;
            string ads = null, core = null;
            HDCRemoteConfig.Fetch("{\"selectedAdCoreName\":\"core\"}", new Dictionary<string, string> { { "core", "{}" } }, (a, c) =>
            {
                ads = a;
                core = c;
            });
            yield return WaitFor(() => ads != null, 5f);

            Assert.AreEqual("{\"selectedAdCoreName\":\"core_vn\"}", ads);
            Assert.AreEqual("{\"forceAdGroups\":[]}", core);
            Assert.IsTrue(HDCConfigReport.Country.IsOn);
            Assert.AreEqual(HDCConfigSource.Country, HDCConfigReport.Find("ads_config").Source);
            Assert.AreEqual(HDCConfigSource.Country, HDCConfigReport.Find("core_vn").Source);
            Assert.AreEqual("country value", HDCCustomConfig.Get(CustomKey));
            Assert.AreEqual(HDCCustomConfig.CountrySource, HDCCustomConfig.SourceOf(CustomKey));
            yield return new ExitPlayMode();
        }
#endif

        private static HDCCountryRules Rules() =>
            new HDCCountryRules
            {
                enabled = true,
                targetCountry = "vn",
                systemLanguages = new[] { "Vietnamese" },
                languageCodes = new[] { "vi" },
                timezoneNames = new[] { "Ho_Chi_Minh", "Saigon" },
            };

        private static HDCAdsSettings Settings(bool enabled, bool simulate)
        {
            var created = ScriptableObject.CreateInstance<HDCAdsSettings>();
            var serialized = new SerializedObject(created);
            foreach (string name in new[] { "countryAdsConfigAndroid", "countryAdsConfigIos" })
                serialized.FindProperty(name).stringValue = "{\"selectedAdCoreName\":\"core_vn\"}";
            foreach (string name in new[] { "countryCoreConfigAndroid", "countryCoreConfigIos" })
                serialized.FindProperty(name).stringValue = "{\"forceAdGroups\":[]}";
            serialized.FindProperty("countryCheck.enabled").boolValue = enabled;
            serialized.FindProperty("countryCheck.simulateInEditor").boolValue = simulate;
            SerializedProperty keys = serialized.FindProperty("customKeys");
            keys.arraySize = 2;
            keys.GetArrayElementAtIndex(0).FindPropertyRelative("key").stringValue = CustomKey;
            keys.GetArrayElementAtIndex(0).FindPropertyRelative("android").stringValue = "normal value";
            keys.GetArrayElementAtIndex(0).FindPropertyRelative("country").stringValue = "country value";
            keys.GetArrayElementAtIndex(1).FindPropertyRelative("key").stringValue = "hdc_test_plain";
            keys.GetArrayElementAtIndex(1).FindPropertyRelative("android").stringValue = "plain";
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return created;
        }
    }
}
