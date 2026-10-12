using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace HDC.Ads.Tests
{
    public class HDCAdjustTests
    {
        private static readonly string[] Keys = { HDCAdjust.NetworkKey, HDCAdjust.CampaignKey, HDCAdjust.CreativeKey, HDCAdjust.CostKey };

        private readonly Dictionary<string, string> savedPrefs = new Dictionary<string, string>();

        internal static GameObject Prefab()
        {
            string path = HDCTestPaths.AssemblyFolder("HDC.Ads.Adjust") + "/HDCAdjust.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.IsNotNull(prefab, "no HDCAdjust prefab at " + path);
            return prefab;
        }

        [SetUp]
        public void SaveState()
        {
            HDCAdjust.ResetState();
            savedPrefs.Clear();
            foreach (string key in Keys)
            {
                if (PlayerPrefs.HasKey(key))
                    savedPrefs[key] = PlayerPrefs.GetString(key);
                PlayerPrefs.DeleteKey(key);
            }
        }

        [TearDown]
        public void RestoreState()
        {
            HDCAdjust.ResetState();
            foreach (string key in Keys)
            {
                if (savedPrefs.TryGetValue(key, out string value))
                    PlayerPrefs.SetString(key, value);
                else
                    PlayerPrefs.DeleteKey(key);
            }
        }

        [TestCase("Facebook Installs", "facebook_installs")]
        [TestCase("Google Ads ACI", "google_ads_aci")]
        [TestCase("Organic", "organic")]
        [TestCase("1Organic", "organic")]
        [TestCase("Apple Search Ads (iAd)", "apple_search_ads_iad")]
        [TestCase("TikTok-Ads", "tiktokads")]
        [TestCase("An Extremely Long Network Name That Keeps Going", "an_extremely_long_network_name")]
        [TestCase("", "")]
        [TestCase(null, "")]
        public void NetworkIsNormalizedLikeTheOldSystem(string network, string expected) =>
            Assert.AreEqual(expected, HDCAdjustAttributions.NormalizeNetwork(network));

        [TestCase(HDCAdjustEnvironment.Auto, true, true)]
        [TestCase(HDCAdjustEnvironment.Auto, false, false)]
        [TestCase(HDCAdjustEnvironment.Sandbox, false, true)]
        [TestCase(HDCAdjustEnvironment.Production, true, false)]
        public void AutoUsesSandboxOnlyInDevelopmentBuilds(HDCAdjustEnvironment environment, bool developmentBuild, bool sandbox) =>
            Assert.AreEqual(sandbox, HDCAdjustSdk.UsesSandbox(environment, developmentBuild));

        [Test]
        public void AdRevenueGoesToAdjustAsAdMobRevenue()
        {
            var revenue = new HDCAdRevenue(HDCAdChannel.ForceAd, "ui", HDCAdFormat.Fullscreen, HDCAdRevenue.AdMob, "Meta Audience Network",
                "ca-app-pub-1/2", 0.0123, "USD", 3);

            HDCAdjustAdRevenue adjust = HDCAdjustAdRevenue.From(revenue);

            Assert.AreEqual("admob_sdk", adjust.Source);
            Assert.AreEqual(0.0123, adjust.Revenue, 1e-12);
            Assert.AreEqual("USD", adjust.Currency);
            Assert.AreEqual("Meta Audience Network", adjust.Network);
            Assert.AreEqual("ca-app-pub-1/2", adjust.Unit);
            Assert.AreEqual("ui", adjust.Placement);
        }

        [Test]
        public void AdRevenueWithoutAnAdSourceNamesTheNetwork()
        {
            var revenue = new HDCAdRevenue(HDCAdChannel.Banner, "FullBottom", HDCAdFormat.Banner, HDCAdRevenue.AdMob, "", "unit", 0.001,
                "EUR", 1);

            Assert.AreEqual(HDCAdRevenue.AdMob, HDCAdjustAdRevenue.From(revenue).Network);
        }

        [Test]
        public void AttributionIsStoredAndReportedOncePerChange()
        {
            var seen = new List<HDCAdjustAttribution>();
            HDCAdjust.AttributionChanged += seen.Add;

            HDCAdjustAttributions.Receive(Attribution("Facebook Installs", "spring", 0.42));
            HDCAdjustAttributions.Receive(Attribution("Facebook Installs", "spring", 0.42));

            Assert.AreEqual(1, seen.Count);
            Assert.IsTrue(HDCAdjust.IsAttributionReady);
            Assert.AreEqual("facebook_installs", HDCAdjust.Network);
            Assert.AreEqual("Facebook Installs", PlayerPrefs.GetString(HDCAdjust.NetworkKey));
            Assert.AreEqual("spring", PlayerPrefs.GetString(HDCAdjust.CampaignKey));
            Assert.AreEqual("banner_a", PlayerPrefs.GetString(HDCAdjust.CreativeKey));
            Assert.AreEqual("0.42", PlayerPrefs.GetString(HDCAdjust.CostKey));

            HDCAdjustAttributions.Receive(Attribution("Organic", "", null));

            Assert.AreEqual(2, seen.Count);
            Assert.AreEqual("organic", HDCAdjust.Network);
            Assert.AreSame(seen[1], HDCAdjust.Attribution);
        }

        [Test]
        public void AnEmptyAttributionIsNoAttribution()
        {
            int calls = 0;
            HDCAdjust.AttributionChanged += _ => calls++;

            HDCAdjustAttributions.Receive(new HDCAdjustAttribution(null, null, null, null, null, null, null, null, null, null));

            Assert.AreEqual(0, calls);
            Assert.IsFalse(HDCAdjust.IsAttributionReady);
            Assert.IsFalse(PlayerPrefs.HasKey(HDCAdjust.NetworkKey));
        }

        [Test]
        public void AFailingHandlerDoesNotStopTheOthers()
        {
            int calls = 0;
            HDCAdjust.AttributionChanged += _ => throw new InvalidOperationException("a game handler failed");
            HDCAdjust.AttributionChanged += _ => calls++;
            LogAssert.Expect(LogType.Exception, new Regex("a game handler failed"));

            HDCAdjustAttributions.Receive(Attribution("Organic", "", null));

            Assert.AreEqual(1, calls);
        }

        [Test]
        public void NetworkReadsTimeOutUntilALateAttribution()
        {
            HDCAdjustAttributions.MarkTimedOut();
            Assert.AreEqual(HDCAdjust.TimedOutNetwork, HDCAdjust.Network);
            Assert.IsFalse(HDCAdjust.IsAttributionReady);

            HDCAdjustAttributions.Receive(Attribution("Unattributed", "", null));
            HDCAdjustAttributions.MarkTimedOut();

            Assert.AreEqual("unattributed", HDCAdjust.Network, "a timeout does not undo an attribution");
        }

        [Test]
        public void ThePrefabStartsAdjustWithTheSdkPrefabDefaults()
        {
            var adjust = Prefab().GetComponent<HDCAdjust>();
            Assert.IsNotNull(adjust, "the prefab has no HDCAdjust");
            var settings = new SerializedObject(adjust);

            Assert.IsFalse(settings.FindProperty("startManually").boolValue, "HDCAdjust starts Adjust itself");
            Assert.AreEqual("", settings.FindProperty("androidAppToken").stringValue, "each game sets its own token");
            Assert.AreEqual("", settings.FindProperty("iosAppToken").stringValue, "each game sets its own token");
            Assert.AreEqual((int)HDCAdjustEnvironment.Auto, settings.FindProperty("environment").intValue);
            Assert.AreEqual((int)HDCAdjustLogLevel.Info, settings.FindProperty("logLevel").intValue);
            Assert.IsFalse(settings.FindProperty("coppaCompliance").boolValue);
            Assert.IsFalse(settings.FindProperty("sendInBackground").boolValue);
            Assert.IsTrue(settings.FindProperty("launchDeferredDeeplink").boolValue);
            Assert.IsFalse(settings.FindProperty("costDataInAttribution").boolValue);
            Assert.IsFalse(settings.FindProperty("linkMe").boolValue);
            Assert.IsFalse(settings.FindProperty("preinstallTracking").boolValue);
            Assert.IsTrue(settings.FindProperty("adServices").boolValue);
            Assert.IsTrue(settings.FindProperty("idfaReading").boolValue);
            Assert.IsTrue(settings.FindProperty("skanAttribution").boolValue);
            Assert.IsTrue(settings.FindProperty("sendAdRevenue").boolValue);
            Assert.AreEqual(7f, settings.FindProperty("attributionTimeoutSeconds").floatValue);
        }

        [Test]
        public void TheAdjustDefineFollowsTheSdk()
        {
            Type activation = Type.GetType("HDC.Ads.Editor.HDCAdsActivation, HDC.Ads.Editor");
            Assert.IsNotNull(activation, "HDCAdsActivation is gone");
            MethodInfo set = activation.GetMethod("SetAdjustDefine", BindingFlags.NonPublic | BindingFlags.Static);
            var defines = new List<string> { "HDC_ADS" };

            Assert.IsTrue((bool)set.Invoke(null, new object[] { defines, true }));
            Assert.IsFalse((bool)set.Invoke(null, new object[] { defines, true }), "already there");
            CollectionAssert.AreEqual(new[] { "HDC_ADS", "HDC_ADJUST" }, defines);
            Assert.IsTrue((bool)set.Invoke(null, new object[] { defines, false }));
            CollectionAssert.AreEqual(new[] { "HDC_ADS" }, defines);
        }

        private static HDCAdjustAttribution Attribution(string network, string campaign, double? cost) =>
            new HDCAdjustAttribution("abc123", network + "::" + campaign, network, campaign, "group", "banner_a", "", cost.HasValue ? "cpi" : "",
                cost, cost.HasValue ? "USD" : "");
    }

    public class HDCAdjustRevenueTests : HDCPlayModeTest
    {
        [UnityTest]
        public IEnumerator HDCAdjustPicksUpTheRevenueOfEveryAd()
        {
#if !HDC_ADJUST
            Assert.Ignore("The project has no Adjust SDK, so HDCAdjust sends nothing.");
#endif
            yield return new EnterPlayMode();
            Object.Instantiate(HDCAdjustTests.Prefab());
            Assert.AreEqual(HDCAdjustStart.Editor, HDCAdjust.StartedBy);
            bool ready = false;
            HDCAds.Initialize(HDCTestConfigs.Ads, HDCTestConfigs.Core, () => ready = true);
            yield return WaitFor(() => ready, 10f);
            HDCAds.ForceAd.Initialize("native_ui");
            yield return WaitFor(() => HDCAds.ForceAd.CanShow("ui"), 5f);

            Assert.IsTrue(HDCAds.ForceAd.Show("ui"));
            yield return WaitFor(() => HDCAdjustRevenue.Count > 0, 3f);

            HDCAdjustAdRevenue revenue = HDCAdjustRevenue.Last.Value;
            Assert.AreEqual(HDCAdjust.AdMobRevenueSource, revenue.Source);
            Assert.AreEqual("native-ok", revenue.Unit);
            Assert.AreEqual("ui", revenue.Placement);
            Assert.AreEqual("Editor simulation", revenue.Network);
            Assert.AreEqual("USD", revenue.Currency);
            yield return new ExitPlayMode();
        }
    }
}
