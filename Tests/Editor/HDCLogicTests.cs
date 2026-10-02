using System.Collections.Generic;
using System.Linq;
using HDC.Ads.Domain;
using HDC.Ads.Logic;
using HDC.Ads.Ports;
using NUnit.Framework;

namespace HDC.Ads.Tests
{
    /// <summary>
    /// The channels' rules on fake ports: a clock, a store and an SDK the test drives, and fake networks in place
    /// of the plugin and the native library. No Play Mode, no Unity time, no native code.
    /// </summary>
    public class HDCLogicTests
    {
        private const string ForceAdAds = @"{ ""selectedAdCoreName"": ""core"",
  ""forceAdChannel"": { ""isEnabled"": true, ""positionConfigs"": [ { ""positionName"": ""pos"", ""cappingTime"": 0 } ] } }";

        private const string NativeGroupCore = @"{
  ""forceAdGroups"": [ { ""groupName"": ""g"", ""positionNames"": [ ""pos"" ], ""mediationPriority"": 1,
      ""androidUnit"": { ""id"": ""native-unit"" } } ] }";

        [Test]
        public void AFakeNetworkLoadsAndShowsAForceAd()
        {
            var ads = new HDCFakeAds();
            ads.Start(ForceAdAds, NativeGroupCore);
            ads.Channels.ForceAd.Initialize("g");
            HDCFakeAd ad = ads.Native.Ad(HDCAdUse.ForceAd);
            Assert.AreEqual(1, ad.Loads);
            Assert.IsFalse(ads.Channels.ForceAd.CanShow("pos"), "not loaded yet");

            ad.Loaded();
            Assert.IsTrue(ads.Channels.ForceAd.CanShow("pos"));
            bool done = false;
            Assert.IsTrue(ads.Channels.ForceAd.Show("pos", () => done = true));
            Assert.AreEqual(1, ad.Shows);
            ad.Displayed();
            ads.Clock.Advance(3f);
            ad.Closed();

            Assert.IsTrue(done);
            Assert.AreEqual(1, ads.Channels.ForceAd.ImpressionCount("pos"));
            Assert.AreEqual(1, ads.Store.Values[HDCAdNames.ForceAdTotalKey]);
            Assert.AreEqual(3f, ads.Context.LastFullscreenAdTime);
        }

        [Test]
        public void CappingWaitsForTheLaunchThenShrinksWithImpressions()
        {
            const string adsConfig = @"{ ""forceAdChannel"": { ""isEnabled"": true, ""launchCappingTime"": 60,
  ""positionConfigs"": [ { ""positionName"": ""pos"", ""cappingTime"": 30, ""cappingDecreasePerImpression"": 10, ""minimumCappingTime"": 15 } ] } }";
            var ads = new HDCFakeAds();
            ads.Start(adsConfig, NativeGroupCore);
            ads.Channels.ForceAd.Initialize("g");
            HDCFakeAd ad = ads.Native.Ad(HDCAdUse.ForceAd);
            ad.Loaded();

            // The first ad of a session waits for the launch capping, which is longer than the position's.
            ads.Clock.Advance(59f);
            Assert.IsFalse(ads.Channels.ForceAd.CanShow("pos"));
            ads.Clock.Advance(1f);
            Assert.IsTrue(ads.Channels.ForceAd.CanShow("pos"));
            ShowAndClose(ads, ad);

            // One impression takes 10 s off the 30 s capping.
            ads.Clock.Advance(19f);
            Assert.IsFalse(ads.Channels.ForceAd.CanShow("pos"));
            ads.Clock.Advance(1f);
            Assert.IsTrue(ads.Channels.ForceAd.CanShow("pos"));
            ShowAndClose(ads, ad);

            // Two would take 20 s off, but the capping stops at the minimum of 15 s.
            ads.Clock.Advance(14f);
            Assert.IsFalse(ads.Channels.ForceAd.CanShow("pos"));
            ads.Clock.Advance(1f);
            Assert.IsTrue(ads.Channels.ForceAd.CanShow("pos"));
        }

        [Test]
        public void ABackupLoadsOnceTheFirstNetworkFails()
        {
            const string core = @"{ ""forceAdGroups"": [ { ""groupName"": ""g"", ""positionNames"": [ ""pos"" ],
  ""mediationPriority"": 1, ""useBackup"": true, ""admobUnit"": { ""id"": ""admob-unit"" }, ""androidUnit"": { ""id"": ""native-unit"" } } ] }";
            var ads = new HDCFakeAds();
            ads.Start(ForceAdAds, core);
            ads.Channels.ForceAd.Initialize("g");
            HDCFakeAd native = ads.Native.Ad(HDCAdUse.ForceAd);
            HDCFakeAd admob = ads.AdMob.Ad(HDCAdUse.ForceAd);

            // Priority 1 puts the native unit first; the AdMob backup waits.
            Assert.AreEqual(1, native.Loads);
            Assert.AreEqual(0, admob.Loads);
            native.FailedToLoad();
            Assert.AreEqual(1, admob.Loads, "the backup starts once the first unit failed");

            admob.Loaded();
            Assert.IsTrue(ads.Channels.ForceAd.Show("pos"));
            Assert.AreEqual(1, admob.Shows);
            Assert.AreEqual(0, native.Shows);
        }

        [Test]
        public void TheLaunchGoesOnWithoutAnAdAfterTheTimeout()
        {
            HDCFakeAds ads = StartLaunch();
            HDCFakeAd ad = ads.AdMob.Ad(HDCAdUse.AppOpen);
            int completed = 0;
            ads.Channels.AppLaunch.Completed += () => completed++;
            Assert.AreEqual(1, ad.Loads);

            ads.Wait(3f);
            Assert.IsFalse(ads.Channels.AppLaunch.IsCompleted, "past the 2 s minimum, before the 5 s timeout");
            ads.Wait(2f);
            Assert.IsTrue(ads.Channels.AppLaunch.IsCompleted);
            Assert.IsTrue(ads.Channels.AppLaunch.IsBeforeShowRaised);
            ads.Wait(1f);
            Assert.AreEqual(1, completed, "once only");
            Assert.AreEqual(0, ad.Shows);
        }

        [Test]
        public void TheLaunchShowsItsAdOnceTheMinimumWaitPassed()
        {
            HDCFakeAds ads = StartLaunch();
            HDCFakeAd ad = ads.AdMob.Ad(HDCAdUse.AppOpen);
            bool completed = false;
            ads.Channels.AppLaunch.Completed += () => completed = true;

            ad.Loaded();
            ads.Wait(1f);
            Assert.AreEqual(0, ad.Shows, "the launch waits 2 s at least");
            ads.Wait(1f);
            Assert.AreEqual(1, ad.Shows);
            Assert.IsFalse(completed);
            ad.Displayed();
            ad.Closed();
            Assert.IsTrue(completed);
        }

        [Test]
        public void AdRemovalHidesBannersAndOutlivesTheSession()
        {
            const string adsConfig = @"{ ""bannerChannel"": { ""isEnabled"": true, ""fullBottom"": { ""isEnabled"": true } } }";
            const string core = @"{ ""bannerUnit"": { ""fullBottom"": { ""mediationPriority"": 0, ""admobUnit"": { ""id"": ""banner-unit"" } } } }";
            var ads = new HDCFakeAds();
            ads.Start(adsConfig, core);
            HDCFakeAd banner = ads.AdMob.Ad(HDCAdUse.Banner);
            banner.Loaded();
            Assert.IsTrue(ads.Channels.Banner.Show());
            Assert.AreEqual(1, banner.Shows);

            ads.Context.SetAdsRemoved(true);
            Assert.AreEqual(1, banner.Hides);
            Assert.IsFalse(ads.Channels.Banner.Show(), "removed ads stay off");
            Assert.AreEqual(1, ads.Store.Saves, "the purchase is saved at once");

            var nextSession = new HDCFakeAds(ads.Store);
            Assert.IsTrue(nextSession.Context.IsAdsRemoved);
        }

        [Test]
        public void PriorityOrderFollowsTheConfig()
        {
            var order = new HDCPriorityOrder();
            CollectionAssert.AreEqual(new[] { HDCAdUnitKeys.AdMob }, order.Order(0, false));
            CollectionAssert.AreEqual(new[] { HDCAdUnitKeys.Native, HDCAdUnitKeys.AdMob }, order.Order(1, true));
            CollectionAssert.AreEqual(new[] { HDCAdUnitKeys.AdMob, HDCAdUnitKeys.Native }, order.Order(0, true));
            CollectionAssert.IsEmpty(order.Order(2, false), "2 is a network no longer served");
            CollectionAssert.AreEqual(new[] { HDCAdUnitKeys.AdMob, HDCAdUnitKeys.Native }, order.Order(2, true));
            CollectionAssert.AreEqual(new[] { HDCAdUnitKeys.AdMob, HDCAdUnitKeys.Native }, order.Order(7, true));
        }

        [Test]
        public void RevenueNamesTheChannelPositionAndNetwork()
        {
            var ads = new HDCFakeAds();
            var revenues = new List<HDCAdRevenue>();
            ads.Context.Revenue += revenues.Add;
            ads.Start(ForceAdAds, NativeGroupCore);
            ads.Channels.ForceAd.Initialize("g");
            HDCFakeAd ad = ads.Native.Ad(HDCAdUse.ForceAd);
            ad.Loaded();
            ads.Channels.ForceAd.Show("pos");
            ad.Displayed();
            ad.Paid(2500);

            HDCAdRevenue revenue = revenues.Single();
            Assert.AreEqual(HDCAdChannel.ForceAd, revenue.Channel);
            Assert.AreEqual("pos", revenue.Position);
            Assert.AreEqual("Fake", revenue.Network);
            Assert.AreEqual(0.0025, revenue.Value, 1e-9);
        }

        private static HDCFakeAds StartLaunch()
        {
            const string adsConfig = @"{ ""appLaunchChannel"": { ""isEnabled"": true, ""minWaitSeconds"": 2, ""timeoutSeconds"": 5 } }";
            const string core = @"{ ""comebackChannel"": { ""launchAdType"": 1 },
  ""appOpenUnit"": { ""mediationPriority"": 0, ""admobUnit"": { ""id"": ""app-open-unit"" } } }";
            var ads = new HDCFakeAds();
            ads.Start(adsConfig, core);
            return ads;
        }

        private static void ShowAndClose(HDCFakeAds ads, HDCFakeAd ad)
        {
            Assert.IsTrue(ads.Channels.ForceAd.Show("pos"));
            ad.Displayed();
            ad.Closed();
            ad.Loaded();
        }
    }
}
