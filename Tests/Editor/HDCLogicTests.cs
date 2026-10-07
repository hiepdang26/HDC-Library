using System;
using System.Collections.Generic;
using System.Linq;
using HDC.Ads.Domain;
using HDC.Ads.Logic;
using HDC.Ads.Ports;
using NUnit.Framework;

namespace HDC.Ads.Tests
{
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

            ads.Clock.Advance(59f);
            Assert.IsFalse(ads.Channels.ForceAd.CanShow("pos"));
            ads.Clock.Advance(1f);
            Assert.IsTrue(ads.Channels.ForceAd.CanShow("pos"));
            ShowAndClose(ads, ad);

            ads.Clock.Advance(19f);
            Assert.IsFalse(ads.Channels.ForceAd.CanShow("pos"));
            ads.Clock.Advance(1f);
            Assert.IsTrue(ads.Channels.ForceAd.CanShow("pos"));
            ShowAndClose(ads, ad);

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

        [Test]
        public void ACompanionEarnsForItsPositionAndKeepsAppResumeAway()
        {
            const string adsConfig = @"{ ""selectedAdCoreName"": ""core"", ""appResumeChannel"": { ""isEnabled"": true, ""adUnitId"": ""resume-unit"" },
  ""forceAdChannel"": { ""isEnabled"": true, ""positionConfigs"": [ { ""positionName"": ""pos"", ""cappingTime"": 0 } ] } }";
            var ads = new HDCFakeAds();
            ads.Native.ForceAdsHaveCompanions = true;
            var revenues = new List<HDCAdRevenue>();
            ads.Context.Revenue += revenues.Add;
            ads.Start(adsConfig, NativeGroupCore);
            ads.Channels.ForceAd.Initialize("g");
            HDCFakeAd ad = ads.Native.Ad(HDCAdUse.ForceAd);
            HDCFakeAd companion = ad.CompanionAd;
            HDCFakeAd resume = ads.Native.Ad(HDCAdUse.AppResume);
            ad.Loaded();

            Assert.IsTrue(ads.Channels.ForceAd.Show("pos"));
            Assert.IsEmpty(companion.ArmedLeaders, "a companion that is not loaded cannot follow its leader");
            ad.Displayed();
            Assert.AreEqual(1, companion.Loads, "the first companion loads once its leader has shown");
            ads.MainThread.Pause(true);
            ad.Closed();
            ads.MainThread.Pause(false);
            companion.Loaded();
            Assert.AreEqual(1, companion.Shows, "and shows as soon as it has loaded");
            Assert.IsTrue(ads.Context.HasOverlayAd, "the companion is on screen before its own Shown event");
            companion.Displayed();
            companion.Paid(1200);
            ads.MainThread.Pause(true);
            Assert.AreEqual(0, resume.Loads, "no app resume ad over the companion");
            ads.MainThread.Pause(false);
            companion.Closed();
            Assert.IsFalse(ads.Context.HasOverlayAd);
            ads.MainThread.Pause(true);

            Assert.AreEqual(1, resume.Loads, "app resume works again once the companion closed");
            HDCAdRevenue revenue = revenues.Single();
            Assert.AreEqual(HDCAdChannel.ForceAd, revenue.Channel);
            Assert.AreEqual("pos", revenue.Position);
            Assert.AreEqual(HDCAdFormat.Fullscreen, revenue.Format);
            Assert.AreEqual(0.0012, revenue.Value, 1e-9);
        }

        [Test]
        public void ACompanionOnScreenOutlivesItsGroup()
        {
            const string core = @"{ ""forceAdGroups"": [ { ""groupName"": ""g"", ""positionNames"": [ ""pos"" ], ""mediationPriority"": 1,
      ""maxShowCount"": 1, ""androidUnit"": { ""id"": ""native-unit"" } } ] }";
            var ads = new HDCFakeAds();
            ads.Native.ForceAdsHaveCompanions = true;
            ads.Start(ForceAdAds, core);
            ads.Channels.ForceAd.Initialize("g");
            HDCFakeAd ad = ads.Native.Ad(HDCAdUse.ForceAd);
            ad.Loaded();

            Assert.IsTrue(ads.Channels.ForceAd.Show("pos"));
            ad.Displayed();
            ad.CompanionAd.Displayed();
            ad.Closed();

            Assert.IsTrue(ad.Destroyed, "the group ran out of shows");
            Assert.IsTrue(ads.Context.HasOverlayAd, "the companion is still on screen");
            Assert.IsFalse(ad.CompanionAd.Destroyed, "it is destroyed once it closes");
            ad.CompanionAd.Closed();
            Assert.IsFalse(ads.Context.HasOverlayAd);
            Assert.IsTrue(ad.CompanionAd.Destroyed);
        }

        [Test]
        public void ALoadedCompanionFollowsItsLeaderOnTheNativeSide()
        {
            HDCFakeAd ad = StartWithCompanion(out HDCFakeAds ads);
            ad.CompanionAd.Loaded();

            Assert.IsTrue(ads.Channels.ForceAd.Show("pos"));
            CollectionAssert.AreEqual(new[] { ad.Id }, ad.CompanionAd.ArmedLeaders);
            ad.CompanionAd.ShownByLeader = true;
            ad.Displayed();

            Assert.AreEqual(0, ad.CompanionAd.Shows, "the native side already showed it with its leader");
        }

        [Test]
        public void ACompanionTheLeaderDidNotShowIsShownFromHere()
        {
            HDCFakeAd ad = StartWithCompanion(out HDCFakeAds ads);
            ad.CompanionAd.Loaded();

            Assert.IsTrue(ads.Channels.ForceAd.Show("pos"));
            ad.Displayed();

            Assert.AreEqual(1, ad.CompanionAd.Shows);
            Assert.IsTrue(ads.Context.HasOverlayAd);
        }

        [Test]
        public void ALeaderThatFailsToShowCancelsItsCompanion()
        {
            HDCFakeAd ad = StartWithCompanion(out HDCFakeAds ads);
            ad.CompanionAd.Loaded();

            Assert.IsTrue(ads.Channels.ForceAd.Show("pos"));
            ad.FailedToShow();

            Assert.AreEqual(1, ad.CompanionAd.LeaderCancels);
            Assert.AreEqual(0, ad.CompanionAd.Shows);
        }

        [Test]
        public void ACompanionThatFailedToLoadWaitsForTheNextLeader()
        {
            HDCFakeAd ad = StartWithCompanion(out HDCFakeAds ads);

            Assert.IsTrue(ads.Channels.ForceAd.Show("pos"));
            ad.Displayed();
            ad.CompanionAd.FailedToLoad();
            ad.CompanionAd.Loaded();

            Assert.AreEqual(1, ad.CompanionAd.Loads);
            Assert.AreEqual(0, ad.CompanionAd.Shows, "a later load does not show it on its own");
        }

        [Test]
        public void InvalidConfigsAndFailingGameHandlersGoToTheLog()
        {
            var ads = new HDCFakeAds();
            ads.Start("{not json", "{not json either");
            Assert.AreEqual(2, ads.Log.Warnings.Count);
            StringAssert.StartsWith("invalid ads config: ", ads.Log.Warnings[0]);
            StringAssert.StartsWith("invalid ad core config: ", ads.Log.Warnings[1]);

            ads = new HDCFakeAds();
            ads.Start(ForceAdAds, NativeGroupCore);
            ads.Channels.ForceAd.Initialize("g");
            int calls = 0;
            ads.Context.Revenue += revenue => throw new InvalidOperationException("a game handler failed");
            ads.Context.Revenue += revenue => calls++;
            ads.Native.Ad(HDCAdUse.ForceAd).Paid(1000);

            Assert.AreEqual("a game handler failed", ads.Log.Exceptions.Single().Message);
            Assert.AreEqual(1, calls, "the other handlers still run");
        }

        private static HDCFakeAd StartWithCompanion(out HDCFakeAds ads)
        {
            ads = new HDCFakeAds();
            ads.Native.ForceAdsHaveCompanions = true;
            ads.Start(ForceAdAds, NativeGroupCore);
            ads.Channels.ForceAd.Initialize("g");
            HDCFakeAd ad = ads.Native.Ad(HDCAdUse.ForceAd);
            ad.Loaded();
            return ad;
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
