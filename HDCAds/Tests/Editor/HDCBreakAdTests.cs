using System.Collections.Generic;
using System.Linq;
using HDC.Ads.Diagnostics;
using HDC.Ads.Domain;
using HDC.Ads.Editor;
using HDC.Ads.Infrastructure;
using HDC.Ads.Logic;
using HDC.Ads.Ports;
using NUnit.Framework;
using UnityEngine;

namespace HDC.Ads.Tests
{
    public class HDCBreakAdTests
    {
        private const string Ads = @"{ ""selectedAdCoreName"": ""core"",
  ""forceAdChannel"": { ""isEnabled"": true, ""positionConfigs"": [ { ""positionName"": ""pos"", ""cappingTime"": 0 } ] } }";

        private const string EveryTenSeconds = @"{ ""adUnitId"": ""break-unit"", ""layoutGroup"": ""cls"", ""showBreakAdTime"": 10, ""notiBreakAdTime"": 3 }";

        private static string Core(string breakAd) => @"{
  ""forceAdLayoutConfig"": { ""layoutGroups"": [ { ""groupName"": ""cls"", ""layouts"": [ { ""layout"": ""fs_single_cls_01"" } ] } ] },
  ""forceAdGroups"": [ { ""groupName"": ""g"", ""positionNames"": [ ""pos"" ], ""mediationPriority"": 1, ""nativeAdUnit"": { ""id"": ""fa-unit"" } } ],
  ""break_ads_config"": " + breakAd + " }";

        [Test]
        public void TheNoticeCountsDownThenTheAdShows()
        {
            HDCFakeAds ads = Started();
            HDCBreakAds breakAd = ads.Channels.BreakAd;
            var states = new List<HDCBreakAdState>();
            breakAd.StateChanged += states.Add;
            Assert.AreEqual(HDCBreakAdState.Waiting, breakAd.State);

            Run(ads, 6.75f);
            Assert.AreEqual(HDCBreakAdState.Waiting, breakAd.State, "the notice starts notiBreakAdTime before the break");
            Run(ads, 0.25f);
            Assert.AreEqual(HDCBreakAdState.Countdown, breakAd.State);
            Assert.AreEqual(3f, breakAd.CountdownSeconds);
            HDCFakeAd ad = BreakAd(ads);
            Assert.AreEqual(1, ad.Loads, "the ad loads when the notice starts");
            Assert.AreEqual(HDCAdFormat.Fullscreen, ad.Format);
            ad.Loaded();

            Run(ads, 2.75f);
            Assert.AreEqual(HDCBreakAdState.Countdown, breakAd.State);
            Assert.AreEqual(0.25f, breakAd.CountdownLeft, 0.001f);
            Run(ads, 0.25f);
            Assert.AreEqual(HDCBreakAdState.Showing, breakAd.State);
            Assert.AreEqual(1, ad.Shows);
            ad.Displayed();
            Assert.IsTrue(ads.Context.HasOverlayAd, "app resume waits while the break ad is on screen");
            Run(ads, 4f);
            Assert.AreEqual(HDCBreakAdState.Showing, breakAd.State);
            ad.Closed();
            Assert.IsFalse(ads.Context.HasOverlayAd);
            Assert.AreEqual(ads.Clock.RealTime, ads.Context.LastFullscreenAdTime, "force ad capping counts from the break ad");
            Assert.AreEqual(HDCBreakAdState.Waiting, breakAd.State);
            CollectionAssert.AreEqual(new[] { HDCBreakAdState.Countdown, HDCBreakAdState.Showing, HDCBreakAdState.Waiting }, states);

            Run(ads, 6.75f);
            Assert.AreEqual(HDCBreakAdState.Waiting, breakAd.State, "the next break counts from the close");
            Run(ads, 0.25f);
            Assert.AreEqual(HDCBreakAdState.Countdown, breakAd.State);
            Assert.AreEqual(2, ad.Loads, "a new ad loads for the next break");
        }

        [Test]
        public void ItsRevenueIsAForceAdAtTheBreakAdPosition()
        {
            HDCFakeAds ads = Started();
            HDCAdRevenue paid = null;
            ads.Context.Revenue += revenue => paid = revenue;
            HDCFakeAd ad = ShowBreak(ads);
            ad.Paid(1500);

            Assert.IsNotNull(paid);
            Assert.AreEqual(HDCAdChannel.ForceAd, paid.Channel);
            Assert.AreEqual(HDCBreakAds.Position, paid.Position);
            Assert.AreEqual(HDCAdFormat.Fullscreen, paid.Format);
        }

        [Test]
        public void ALateAdShowsWithinTheLoadingTimeoutAndTheBreakIsSkippedAfterIt()
        {
            HDCFakeAds ads = Started();
            HDCBreakAds breakAd = ads.Channels.BreakAd;
            breakAd.LoadingTimeout = 2f;
            Run(ads, 10f);
            Assert.AreEqual(HDCBreakAdState.Loading, breakAd.State, "the countdown ended before the ad loaded");
            Run(ads, 1.75f);
            Assert.AreEqual(HDCBreakAdState.Loading, breakAd.State);
            Run(ads, 0.25f);
            Assert.AreEqual(HDCBreakAdState.Waiting, breakAd.State, "skipped after LoadingTimeout");
            HDCFakeAd ad = BreakAd(ads);
            Assert.AreEqual(0, ad.Shows);

            ad.Loaded();
            Run(ads, 7f);
            Assert.AreEqual(HDCBreakAdState.Countdown, breakAd.State);
            Assert.AreEqual(1, ad.Loads, "the ad that loaded late waits for the next break");
            Run(ads, 3f);
            Assert.AreEqual(HDCBreakAdState.Showing, breakAd.State);
            Assert.AreEqual(1, ad.Shows);
        }

        [Test]
        public void AnAdThatLoadsWhileLoadingShowsAtOnce()
        {
            HDCFakeAds ads = Started();
            Run(ads, 10.5f);
            Assert.AreEqual(HDCBreakAdState.Loading, ads.Channels.BreakAd.State);
            BreakAd(ads).Loaded();
            Run(ads, 0.25f);
            Assert.AreEqual(HDCBreakAdState.Showing, ads.Channels.BreakAd.State);
            Assert.AreEqual(1, BreakAd(ads).Shows);
        }

        [Test]
        public void ALoadFailureSkipsTheBreak()
        {
            HDCFakeAds ads = Started();
            Run(ads, 7f);
            HDCFakeAd ad = BreakAd(ads);
            ad.FailedToLoad();
            Run(ads, 3f);
            Assert.AreEqual(HDCBreakAdState.Waiting, ads.Channels.BreakAd.State);
            Assert.AreEqual(0, ad.Shows);
        }

        [Test]
        public void AFailedShowStartsTheNextCycle()
        {
            HDCFakeAds ads = Started();
            HDCFakeAd ad = ShowBreak(ads, false);
            ad.FailedToShow();
            Assert.AreEqual(HDCBreakAdState.Waiting, ads.Channels.BreakAd.State);
            Assert.IsFalse(ads.Context.HasOverlayAd);
        }

        [Test]
        public void AnotherFullScreenAdHidesTheNoticeAndRestartsTheCount()
        {
            HDCFakeAds ads = Started();
            ads.Channels.ForceAd.Initialize("g");
            HDCFakeAd forceAd = ads.Native.Ad(HDCAdUse.ForceAd);
            forceAd.Loaded();
            Run(ads, 8f);
            Assert.AreEqual(HDCBreakAdState.Countdown, ads.Channels.BreakAd.State);

            Assert.IsTrue(ads.Channels.ForceAd.Show("pos"));
            forceAd.Displayed();
            Assert.AreEqual(HDCBreakAdState.Waiting, ads.Channels.BreakAd.State, "the notice goes away");
            Run(ads, 20f);
            Assert.AreEqual(HDCBreakAdState.Waiting, ads.Channels.BreakAd.State, "no count while the other ad is on screen");
            forceAd.Closed();
            Run(ads, 6.75f);
            Assert.AreEqual(HDCBreakAdState.Waiting, ads.Channels.BreakAd.State);
            Run(ads, 0.25f);
            Assert.AreEqual(HDCBreakAdState.Countdown, ads.Channels.BreakAd.State);
        }

        [Test]
        public void TimeInTheBackgroundDoesNotCount()
        {
            HDCFakeAds ads = Started();
            Run(ads, 5f);
            ads.MainThread.Pause(true);
            ads.Clock.Advance(300f);
            ads.MainThread.Pause(false);
            Run(ads, 1.75f);
            Assert.AreEqual(HDCBreakAdState.Waiting, ads.Channels.BreakAd.State);
            Run(ads, 0.25f);
            Assert.AreEqual(HDCBreakAdState.Countdown, ads.Channels.BreakAd.State);
        }

        [Test]
        public void ALongFrameCountsAtMostOneSecond()
        {
            HDCFakeAds ads = Started();
            ads.Wait(30f);
            Assert.AreEqual(HDCBreakAdState.Waiting, ads.Channels.BreakAd.State);
        }

        [Test]
        public void RemovingTheAdsTurnsItOff()
        {
            HDCFakeAds ads = Started();
            Run(ads, 8f);
            Assert.AreEqual(HDCBreakAdState.Countdown, ads.Channels.BreakAd.State);
            ads.Context.SetAdsRemoved(true);
            Assert.AreEqual(HDCBreakAdState.Off, ads.Channels.BreakAd.State);
            Run(ads, 30f);
            Assert.AreEqual(HDCBreakAdState.Off, ads.Channels.BreakAd.State);
            Assert.AreEqual(0, BreakAd(ads).Shows);
        }

        [Test]
        public void WithoutAnAdUnitItStaysOff()
        {
            foreach (string core in new[] { "{}", Core(@"{ ""layoutGroup"": ""cls"" }"), Core(@"{ ""adUnitId"": ""u"", ""showBreakAdTime"": 0 }") })
            {
                var ads = new HDCFakeAds();
                ads.Start(Ads, core);
                ads.Channels.BreakAd.Start();
                Run(ads, 120f);
                Assert.AreEqual(HDCBreakAdState.Off, ads.Channels.BreakAd.State, core);
                Assert.IsFalse(ads.Native.Ads.Any(ad => ad.Use == HDCAdUse.BreakAd), core);
            }
        }

        [Test]
        public void StopEndsTheNoticeAndStartCountsFromZero()
        {
            HDCFakeAds ads = Started();
            HDCBreakAds breakAd = ads.Channels.BreakAd;
            Run(ads, 8f);
            breakAd.Stop();
            Assert.IsFalse(breakAd.IsRunning);
            Assert.AreEqual(HDCBreakAdState.Off, breakAd.State);
            Run(ads, 30f);
            Assert.AreEqual(HDCBreakAdState.Off, breakAd.State);

            breakAd.Start();
            Assert.AreEqual(HDCBreakAdState.Waiting, breakAd.State);
            Run(ads, 6.75f);
            Assert.AreEqual(HDCBreakAdState.Waiting, breakAd.State);
            Run(ads, 0.25f);
            Assert.AreEqual(HDCBreakAdState.Countdown, breakAd.State);
        }

        [Test]
        public void ItWaitsForTheSdkWhenStartedEarly()
        {
            var ads = new HDCFakeAds();
            ads.Channels.BreakAd.Start();
            Run(ads, 1f);
            Assert.AreEqual(HDCBreakAdState.Off, ads.Channels.BreakAd.State);
            ads.Start(Ads, Core(EveryTenSeconds));
            Run(ads, 0.25f);
            Assert.AreEqual(HDCBreakAdState.Waiting, ads.Channels.BreakAd.State);
        }

        [Test]
        public void TheNativeNetworkPlansItAsTheOldFullScreenInstance()
        {
            HDCAdCoreConfig core = HDCAdCoreConfig.Parse(Core(EveryTenSeconds));
            HDCAdPlan plan = new HDCNativeNetwork().Plan(HDCAdUse.BreakAd,
                new HDCAdUnitSpec("", core.break_ads_config.UnitFor(HDCAdUnitKeys.Native), false, core));
            Assert.AreEqual(HDCAdNames.NativeBreakAd, plan.InstanceId);
            Assert.AreEqual(HDCAdFormat.Fullscreen, plan.Format);
            Assert.AreEqual("break-unit", plan.AdUnitId);
            Assert.IsNull(core.break_ads_config.UnitFor(HDCAdUnitKeys.AdMob), "native only");

            HDCFakeAds ads = Started();
            Assert.IsFalse(ads.Context.Groups.BreakAdPlan().Spec.ReloadAfterShow, "it loads only when the notice starts");
        }

        [Test]
        public void ItsEventsGoToTheBreakAdTabNotForceAd()
        {
            var ads = new HDCFakeAds();
            Assert.IsTrue(((IAdChannel)ads.Channels.BreakAd).Diagnostics.Owns(HDCAdNames.NativeBreakAd));
            Assert.IsFalse(((IAdChannel)ads.Channels.ForceAd).Diagnostics.Owns(HDCAdNames.NativeBreakAd));
            Assert.IsTrue(((IAdChannel)ads.Channels.ForceAd).Diagnostics.Owns(HDCAdNames.NativeForceAd("gameplay")));
            Assert.AreEqual("BRK", ads.Channels.All.Last().Key);
        }

        [Test]
        public void TheConfigCheckFlagsAMissingLayoutGroupAndALongNotice()
        {
            IConfigRule rule = ((IAdChannel)new HDCFakeAds().Channels.BreakAd).ConfigRule;
            HDCAdCoreConfig odd = HDCAdCoreConfig.Parse(Core(@"{ ""adUnitId"": ""u"", ""layoutGroup"": ""nope"", ""showBreakAdTime"": 5, ""notiBreakAdTime"": 5 }"));
            List<string> findings = rule.Check(new HDCAdsConfig(), odd).Select(finding => finding.Text).ToList();
            Assert.AreEqual(2, findings.Count, string.Join("\n", findings));
            StringAssert.Contains("notiBreakAdTime", findings[0]);
            StringAssert.Contains("'nope'", findings[1]);
            Assert.IsEmpty(rule.Check(new HDCAdsConfig(), HDCAdCoreConfig.Parse(Core(EveryTenSeconds))));
            Assert.IsEmpty(rule.Check(new HDCAdsConfig(), HDCAdCoreConfig.Parse("{}")), "no break ad, nothing to check");
        }

        [Test]
        public void TheOldBreakAdConfigOfTheAdsConfigIsIgnored()
        {
            const string oldAds = @"{ ""forceAdChannel"": { ""isEnabled"": true,
  ""positionConfigs"": [ { ""positionName"": ""pos"", ""cappingTime"": 0 } ],
  ""breakAdConfig"": { ""isEnabled"": true, ""positionName"": ""pos"", ""notificationLeadTimeSeconds"": 3 } } }";
            var ads = new HDCFakeAds();
            ads.Start(oldAds, "{}");
            ads.Channels.BreakAd.Start();
            Run(ads, 60f);
            Assert.AreEqual(HDCBreakAdState.Off, ads.Channels.BreakAd.State);
        }

        [Test]
        public void TheConfigMenuAddsTheBlockOnce()
        {
            string added = HDCBreakAdConfigTemplate.WithBreakAd("{}", "unit");
            HDCAdCoreConfig parsed = HDCAdCoreConfig.Parse(added);
            Assert.AreEqual("unit", parsed.break_ads_config.adUnitId);
            Assert.AreEqual(60f, parsed.break_ads_config.showBreakAdTime);
            Assert.AreEqual(5f, parsed.break_ads_config.notiBreakAdTime);
            Assert.IsNull(HDCBreakAdConfigTemplate.WithBreakAd(added, "unit"), "already there");

            const string withGroups = @"{ ""forceAdLayoutConfig"": { ""layoutGroups"": [ { ""groupName"": ""cls"" } ] } }";
            HDCAdCoreConfig grouped = HDCAdCoreConfig.Parse(HDCBreakAdConfigTemplate.WithBreakAd(withGroups, "unit"));
            Assert.AreEqual("cls", grouped.break_ads_config.layoutGroup, "the first layout group of the core");
            Assert.AreEqual(1, grouped.forceAdLayoutConfig.layoutGroups.Length);

            Assert.AreEqual("unit", HDCAdCoreConfig.Parse(HDCBreakAdConfigTemplate.WithBreakAd("", "unit")).break_ads_config.adUnitId);
            Assert.IsNull(HDCBreakAdConfigTemplate.WithBreakAd("not json", "unit"));
        }

        private static HDCFakeAds Started(string breakAd = EveryTenSeconds)
        {
            var ads = new HDCFakeAds();
            ads.Start(Ads, Core(breakAd));
            ads.Channels.BreakAd.Start();
            return ads;
        }

        private static HDCFakeAd ShowBreak(HDCFakeAds ads, bool displayed = true)
        {
            Run(ads, 7f);
            HDCFakeAd ad = BreakAd(ads);
            ad.Loaded();
            Run(ads, 3f);
            Assert.AreEqual(HDCBreakAdState.Showing, ads.Channels.BreakAd.State);
            if (displayed)
                ad.Displayed();
            return ad;
        }

        private static HDCFakeAd BreakAd(HDCFakeAds ads) => ads.Native.Ad(HDCAdUse.BreakAd);

        private static void Run(HDCFakeAds ads, float seconds)
        {
            for (int step = 0; step < Mathf.RoundToInt(seconds * 4f); step++)
                ads.Wait(0.25f);
        }
    }
}
