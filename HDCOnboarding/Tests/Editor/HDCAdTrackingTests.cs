using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HDC.Ads;
using HDC.Ads.Domain;
using HDC.Ads.Infrastructure;
using HDC.Ads.Tests;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace HDC.Onboarding.Tests
{
    public class HDCAdTrackingTests : HDCPlayModeTest
    {
        private const string Ads = @"{ ""selectedAdCoreName"": ""core"",
  ""popupChannel"": { ""isEnabled"": true, ""positionConfigs"": [ { ""isEnabled"": true, ""positionName"": ""native_splash"" } ] } }";

        private const string Core = @"{ ""popupGroups"": [ { ""groupName"": ""native_splash"", ""positionNames"": [ ""native_splash"" ],
      ""nativeAdUnit"": { ""id"": ""popup-ok"", ""layout"": ""popup_single_manual_01"", ""timeShow"": 3 } } ] }";

        private readonly List<HDCAdPaid> tracked = new List<HDCAdPaid>();
        private HDCOnboardingPrefs prefs;

        [SetUp]
        public void CleanPrefs()
        {
            prefs = new HDCOnboardingPrefs();
            tracked.Clear();
        }

        [TearDown]
        public void RestorePrefs() => prefs.Dispose();

        [UnityTest]
        public IEnumerator APaidPopupOfATrackedGroupIsCountedAndSent()
        {
            yield return new EnterPlayMode();
            yield return InitializeAds();
            StartTracking("native_splash");
            HDCAds.Popup.Initialize("native_splash");
            yield return WaitFor(() => HDCAds.Popup.IsGroupReady("native_splash"), 5f);

            HDCAds.Popup.Move("native_splash", new Rect(0f, 0f, 320f, 100f));
            Assert.IsTrue(HDCAds.Popup.Show("native_splash"));
            yield return WaitFor(() => tracked.Count > 0, 5f);

            HDCAdPaid paid = tracked.Single();
            Assert.AreEqual("native_splash", paid.Name);
            Assert.AreEqual(HDCAdChannel.Popup, paid.Revenue.Channel);
            Assert.AreEqual("native_splash", paid.Revenue.Position);
            Assert.AreEqual("native_splash", paid.Revenue.Group);
            Assert.AreEqual(1, paid.SessionCount);
            Assert.AreEqual(1, paid.NamePaidCount);
            Assert.AreEqual(1, paid.TotalPaidCount);

            Dictionary<string, object> parameters = paid.Parameters().ToDictionary(pair => pair.Key, pair => pair.Value);
            Assert.AreEqual("native_splash", parameters["ad_name"]);
            Assert.AreEqual("popup", parameters["ad_channel"]);
            Assert.AreEqual("native_splash", parameters["ad_position"]);
            Assert.AreEqual("native_splash", parameters["ad_group"]);
            Assert.AreEqual(HDCAdFormat.Popup, parameters["ad_format"]);
            Assert.AreEqual("popup-ok", parameters["ad_unit"]);
            Assert.AreEqual("USD", parameters["currency"]);
            Assert.IsInstanceOf<double>(parameters["value"]);
            Assert.AreEqual(1L, parameters["session_count"]);
            Assert.AreEqual(1L, parameters["name_paid_count"]);
            Assert.AreEqual(1L, parameters["total_paid_count"]);
            Assert.IsInstanceOf<double>(parameters["total_value"]);
            Assert.AreEqual(1, PlayerPrefs.GetInt(HDCAdTrackingCounts.CountPrefix + "native_splash"));
            Assert.AreEqual(1, PlayerPrefs.GetInt(HDCAdTrackingCounts.TotalKey));
            yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator OnlyListedAdsCountAndTheGroupNameWins()
        {
            yield return new EnterPlayMode();
            yield return InitializeAds();
            StartTracking("splash", "grp");

            Paid("splash_it", HDCAdChannel.AppLaunch, "splash", "", HDCAdFormat.Interstitial, 1000);
            Paid("fa_grp", HDCAdChannel.ForceAd, "pos", "grp", HDCAdFormat.Fullscreen, 2000);
            Paid("fa_other", HDCAdChannel.ForceAd, "other", "other_group", HDCAdFormat.Fullscreen, 4000);

            CollectionAssert.AreEqual(new[] { "splash", "grp" }, tracked.Select(paid => paid.Name).ToArray());
            Assert.AreEqual("app_launch", tracked[0].Parameters().First(pair => pair.Key == "ad_channel").Value);
            Assert.IsFalse(tracked[0].Parameters().Any(pair => pair.Key == "ad_group"), "an ad without a group sends no ad_group");
            Assert.AreEqual("pos", tracked[1].Revenue.Position);
            Assert.AreEqual(new[] { 1, 1 }, tracked.Select(paid => paid.NamePaidCount).ToArray());
            Assert.AreEqual(new[] { 1, 2 }, tracked.Select(paid => paid.TotalPaidCount).ToArray());
            Assert.AreEqual(0.003, tracked[1].TotalValue, 1e-9);
            yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator CountsContinueFromTheOldSystem()
        {
            PlayerPrefs.SetInt(HDCAdTrackingCounts.LegacySessionKey, 5);
            PlayerPrefs.SetInt(HDCAdTrackingCounts.LegacyTotalKey, 10);
            PlayerPrefs.SetInt("native_splash", 3);
            yield return new EnterPlayMode();
            yield return InitializeAds();
            StartTracking("native_splash");

            Paid("pu_native_splash", HDCAdChannel.Popup, "native_splash", "native_splash", HDCAdFormat.Popup, 1500);

            HDCAdPaid paid = tracked.Single();
            Assert.AreEqual(6, paid.SessionCount);
            Assert.AreEqual(4, paid.NamePaidCount);
            Assert.AreEqual(11, paid.TotalPaidCount);
            Assert.AreEqual(6, HDCAdTracking.SessionCount);
            yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator ASecondTrackerNeitherCountsTheSessionNorTheAdAgain()
        {
            yield return new EnterPlayMode();
            yield return InitializeAds();
            StartTracking("splash");
            GameObject second = StartTracking("splash");
            yield return null;

            Assert.IsNull(second.GetComponent<HDCAdTracking>());
            Assert.AreEqual(1, HDCAdTracking.SessionCount);
            Paid("splash_naf", HDCAdChannel.AppLaunch, "splash", "", HDCAdFormat.Fullscreen, 1000);
            Assert.AreEqual(1, tracked.Count);
            yield return new ExitPlayMode();
        }

        [Test]
        public void TheParametersBecomeFirebaseParameters()
        {
            Type parameterType = Type.GetType(HDCFirebaseAnalytics.ParameterType);
            if (parameterType == null)
                Assert.Ignore("Firebase Analytics is not in this project");
            var revenue = new HDCAdRevenue(HDCAdChannel.Popup, "native_splash", HDCAdFormat.Popup, HDCAdRevenue.AdMob, "AdMob Network", "unit",
                0.0012, "USD", 3, "native_splash");
            IReadOnlyList<KeyValuePair<string, object>> parameters = new HDCAdPaid(revenue, "native_splash", 2, 3, 4, 0.5).Parameters();

            Array values = HDCFirebaseAnalytics.Parameters(parameterType, parameters);

            Assert.AreEqual(parameters.Count, values.Length);
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            PropertyInfo name = parameterType.GetProperty("Name", flags);
            PropertyInfo value = parameterType.GetProperty("Value", flags);
            Dictionary<string, object> built = values.Cast<object>().ToDictionary(item => (string)name.GetValue(item), item => value.GetValue(item));
            Assert.AreEqual("native_splash", built["ad_name"]);
            Assert.AreEqual(0.0012, (double)built["value"], 1e-12);
            Assert.AreEqual(2L, built["session_count"]);
            Assert.AreEqual(3L, built["precision"]);
            Assert.AreEqual(0.5, (double)built["total_value"], 1e-12);
        }

        private IEnumerator InitializeAds()
        {
            bool ready = false;
            HDCAds.Initialize(Ads, Core, () => ready = true);
            HDCAdTracking.Tracked += tracked.Add;
            yield return WaitFor(() => ready, 10f);
        }

        private static GameObject StartTracking(params string[] names)
        {
            var root = new GameObject("HDCAdTracking");
            root.SetActive(false);
            var serialized = new SerializedObject(root.AddComponent<HDCAdTracking>());
            SerializedProperty list = serialized.FindProperty("trackedAds");
            list.arraySize = names.Length;
            for (int i = 0; i < names.Length; i++)
                list.GetArrayElementAtIndex(i).stringValue = names[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
            root.SetActive(true);
            return root;
        }

        private static void Paid(string id, HDCAdChannel channel, string position, string group, string format, long valueMicros)
        {
            HDCOnboardingAds.Context.Placements.Record(id, channel, position, HDCAdRevenue.AdMob, group);
            HDCAdsSdk.Emit(new HDCAdEvent
            {
                id = id, format = format, type = HDCAdEventType.Paid, adUnitId = "unit", valueMicros = valueMicros, currency = "USD", precision = 1,
            });
        }
    }
}
