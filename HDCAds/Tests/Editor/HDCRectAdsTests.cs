using System.Linq;
using HDC.Ads.Domain;
using HDC.Ads.Infrastructure;
using HDC.Ads.Logic;
using HDC.Ads.Ports;
using NUnit.Framework;
using UnityEngine;

namespace HDC.Ads.Tests
{
    public class HDCRectAdsTests
    {
        private const string Core = @"{
  ""bannerUnit"": { ""fullBottom"": { ""mediationPriority"": 0, ""admobUnit"": { ""id"": ""banner-unit"" } } },
  ""mrecUnit"": { ""mediationPriority"": 0, ""admobUnit"": { ""id"": ""mrec-unit"" } },
  ""rewardedUnit"": { ""mediationPriority"": 0, ""admobUnit"": { ""id"": ""rewarded-unit"" } } }";

        private static string Ads(bool bannerOn = true, bool slotOn = true, bool slotAutoInit = false, bool mrecOn = true, bool rewardedOn = true,
            bool rewardedAutoInit = true) =>
            "{ \"bannerChannel\": { \"isEnabled\": " + Json(bannerOn) + ", \"fullBottom\": { \"isEnabled\": " + Json(slotOn) +
            ", \"autoInit\": " + Json(slotAutoInit) + " } }, \"mrecChannel\": { \"isEnabled\": " + Json(mrecOn) + ", \"autoInit\": false }," +
            " \"rewardedChannel\": { \"isEnabled\": " + Json(rewardedOn) + ", \"autoInit\": " + Json(rewardedAutoInit) + " } }";

        [Test]
        public void AFullBottomThatIsOffStillShowsWhenTheGameAsks()
        {
            HDCFakeAds ads = Started(Ads(slotOn: false, slotAutoInit: true));
            Assert.IsFalse(ads.AdMob.Ads.Any(ad => ad.Use == HDCAdUse.Banner), "a fullBottom that is off does not start by itself");
            ads.Channels.Banner.Initialize();
            Assert.IsFalse(ads.AdMob.Ads.Any(ad => ad.Use == HDCAdUse.Banner), "nor through Initialize");

            Assert.IsFalse(ads.Channels.Banner.Show(), "Show loads the banner, which shows once it has an ad");
            HDCFakeAd banner = ads.AdMob.Ad(HDCAdUse.Banner);
            Assert.AreEqual(1, banner.Loads);
            Assert.IsFalse(ads.Channels.Banner.CanShow());
            banner.Loaded();
            Assert.IsTrue(ads.Channels.Banner.CanShow());
            Assert.IsTrue(ads.Channels.Banner.Show());
        }

        [Test]
        public void TheBannerChannelOrRemovedAdsBlockEveryShow()
        {
            HDCFakeAds off = Started(Ads(bannerOn: false));
            Assert.IsFalse(off.Channels.Banner.Show());
            Assert.IsFalse(off.AdMob.Ads.Any(ad => ad.Use == HDCAdUse.Banner));

            HDCFakeAds removed = Started(Ads());
            removed.Context.SetAdsRemoved(true);
            Assert.IsFalse(removed.Channels.Banner.Show());
            Assert.IsFalse(removed.AdMob.Ads.Any(ad => ad.Use == HDCAdUse.Banner));
        }

        [Test]
        public void AutoInitStartsTheBannerWhenFullBottomIsOn()
        {
            HDCFakeAds ads = Started(Ads(slotOn: true, slotAutoInit: true));
            Assert.AreEqual(1, ads.AdMob.Ad(HDCAdUse.Banner).Loads);
        }

        [Test]
        public void OnlyFullBottomIsReadFromTheConfigs()
        {
            HDCFakeAds ads = Started("{ \"bannerChannel\": { \"isEnabled\": true, \"fullTop\": { \"isEnabled\": true, \"autoInit\": true }," +
                " \"fullBottom\": { \"isEnabled\": true, \"autoInit\": true } } }",
                @"{ ""bannerUnit"": { ""fullTop"": { ""admobUnit"": { ""id"": ""top-unit"" } }, ""fullBottom"": { ""admobUnit"": { ""id"": ""bottom-unit"" } } } }");

            HDCFakeAd banner = ads.AdMob.Ads.Single(ad => ad.Use == HDCAdUse.Banner);
            Assert.AreEqual("bottom-unit", banner.AdUnitId, "fullTop and the corners are not read any more");

            HDCAdPlan plan = new HDCAdMobNetwork().Plan(HDCAdUse.Banner,
                new HDCAdUnitSpec("", new HDCAdCoreConfig.AdmobUnit { id = "bottom-unit" }, true, new HDCAdCoreConfig()));
            Assert.AreEqual("bn_plugin_FullBottom", plan.InstanceId, "the AdMob banner keeps its old instance name");
            Assert.AreEqual(HDCAdFormat.BannerView, plan.Format);
        }

        [Test]
        public void TheMrecPositionSetBeforeItsAdIsAppliedOnEveryLoad()
        {
            HDCFakeAds ads = Started(Ads());
            ads.Channels.Mrec.Move(HDCAdPosition.Top);
            Assert.IsFalse(ads.Channels.Mrec.Show(), "not loaded yet: the MREC shows once it has an ad");
            HDCFakeAd mrec = ads.AdMob.Ad(HDCAdUse.Mrec);
            CollectionAssert.AreEqual(new[] { "Top" }, mrec.Moves, "applied as soon as the view exists");

            mrec.Loaded();
            CollectionAssert.AreEqual(new[] { "Top", "Top" }, mrec.Moves, "applied again on load");
            Assert.IsTrue(ads.Channels.Mrec.Show());
            CollectionAssert.AreEqual(new[] { "Top", "Top", "Top" }, mrec.Moves, "and on show");
        }

        [Test]
        public void TheMrecFollowsItsTargetOnEachLoad()
        {
            var target = new GameObject("MREC target");
            try
            {
                HDCFakeAds ads = Started(Ads());
                ads.Channels.Mrec.Initialize();
                HDCFakeAd mrec = ads.AdMob.Ad(HDCAdUse.Mrec);
                target.transform.position = new Vector3(100f, 200f, 0f);
                ads.Channels.Mrec.Move(target);
                mrec.Loaded();
                target.transform.position = new Vector3(300f, 400f, 0f);
                mrec.Loaded();
                CollectionAssert.AreEqual(new[] { "100,200", "100,200", "300,400" }, mrec.Moves);
            }
            finally
            {
                Object.DestroyImmediate(target);
            }
        }

        [Test]
        public void TheMrecTakesTheCentreOfARectNotItsPivot()
        {
            var target = new GameObject("MrecPos", typeof(RectTransform));
            try
            {
                var rect = (RectTransform)target.transform;
                rect.pivot = Vector2.zero;
                rect.sizeDelta = new Vector2(200f, 100f);
                rect.position = new Vector3(100f, 100f, 0f);
                HDCFakeAds ads = Started(Ads());
                ads.Channels.Mrec.Initialize();
                HDCFakeAd mrec = ads.AdMob.Ad(HDCAdUse.Mrec);
                ads.Channels.Mrec.Move(target);
                CollectionAssert.AreEqual(new[] { "200,150" }, mrec.Moves, "the pivot is the bottom-left corner at (100, 100)");
            }
            finally
            {
                Object.DestroyImmediate(target);
            }
        }

        [Test]
        public void AShownMrecFollowsItsTargetOnlyWhenItMoves()
        {
            var target = new GameObject("MrecPos", typeof(RectTransform));
            try
            {
                var rect = (RectTransform)target.transform;
                rect.sizeDelta = new Vector2(300f, 250f);
                rect.position = new Vector3(540f, 900f, 0f);
                HDCFakeAds ads = Started(Ads());
                ads.Channels.Mrec.Move(target);
                ads.Channels.Mrec.Show();
                HDCFakeAd mrec = ads.AdMob.Ad(HDCAdUse.Mrec);
                mrec.Loaded();
                mrec.Moves.Clear();

                ads.MainThread.LateTick();
                CollectionAssert.IsEmpty(mrec.Moves, "a target that stays put sends nothing");

                rect.position = new Vector3(540f, 960f, 0f);
                ads.MainThread.LateTick();
                ads.MainThread.LateTick();
                CollectionAssert.AreEqual(new[] { "540,960" }, mrec.Moves, "a popup sliding in: the MREC goes with it, once per change");

                rect.position = new Vector3(540.2f, 960f, 0f);
                ads.MainThread.LateTick();
                Assert.AreEqual(1, mrec.Moves.Count, "less than half a pixel is no move");

                ads.Channels.Mrec.Hide();
                rect.position = new Vector3(100f, 100f, 0f);
                ads.MainThread.LateTick();
                Assert.AreEqual(1, mrec.Moves.Count, "a hidden MREC stops following");
            }
            finally
            {
                Object.DestroyImmediate(target);
            }
        }

        [Test]
        public void TheCameraComesFromTheCanvasOfTheTarget()
        {
            var cameraObject = new GameObject("UI Camera", typeof(Camera));
            var canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
            var plain = new GameObject("World object");
            try
            {
                var camera = cameraObject.GetComponent<Camera>();
                var canvas = canvasObject.GetComponent<Canvas>();
                var target = new GameObject("MrecPos", typeof(RectTransform));
                target.transform.SetParent(canvasObject.transform, false);

                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                Assert.IsNull(HDCMrecAds.CameraFor(target.transform, null), "an overlay canvas is in screen pixels");
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                Assert.AreSame(camera, HDCMrecAds.CameraFor(target.transform, null), "a camera canvas is seen through its camera");
                canvas.worldCamera = null;
                Assert.IsNull(HDCMrecAds.CameraFor(target.transform, null), "a camera canvas without a camera draws like an overlay");
                Assert.AreSame(camera, HDCMrecAds.CameraFor(target.transform, camera), "a camera passed to Move wins");
                Assert.IsNull(HDCMrecAds.CameraFor(plain.transform, null), "a plain object keeps its position as a screen point");
            }
            finally
            {
                Object.DestroyImmediate(canvasObject);
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(plain);
            }
        }

        [Test]
        public void AnMrecThatIsOffIgnoresMovesAndShows()
        {
            HDCFakeAds ads = Started(Ads(mrecOn: false));
            ads.Channels.Mrec.Move(HDCAdPosition.Top);
            Assert.IsFalse(ads.Channels.Mrec.Show());
            Assert.IsFalse(ads.AdMob.Ads.Any(ad => ad.Use == HDCAdUse.Mrec));
        }

        [Test]
        public void AFailedBannerLoadIsRebuiltOnTheOldSchedule()
        {
            CollectionAssert.AreEqual(new[] { 20f, 25f, 30f, 35f, 40f, 40f },
                Enumerable.Range(0, 6).Select(HDCGmaBannerView.RecoveryDelay).ToArray());
        }

        [Test]
        public void TheRewardedRewardsOnCloseAndCountsTheImpression()
        {
            HDCFakeAds ads = Started(Ads());
            HDCFakeAd rewarded = ads.AdMob.Ad(HDCAdUse.Rewarded);
            Assert.AreEqual(1, rewarded.Loads, "autoInit loads it once the SDK is ready");
            Assert.IsFalse(ads.Channels.Rewarded.CanShow);
            rewarded.Loaded();
            Assert.IsTrue(ads.Channels.Rewarded.CanShow);

            string calls = "";
            Assert.IsTrue(ads.Channels.Rewarded.Show("shop", () => calls += "rewarded,", () => calls += "closed"));
            rewarded.Displayed();
            ads.Sdk.Emit(new HDCAdEvent { id = rewarded.Id, format = rewarded.Format, type = HDCAdEventType.Rewarded });
            ads.Clock.Advance(5f);
            rewarded.Closed();
            Assert.AreEqual("rewarded,closed", calls);
            Assert.AreEqual(1, ads.Channels.Rewarded.ImpressionCount);
            Assert.AreEqual(5f, ads.Context.LastFullscreenAdTime, "a rewarded close restarts the force ad capping");
        }

        [Test]
        public void ARewardedClosedEarlyGivesNoReward()
        {
            HDCFakeAds ads = Started(Ads());
            HDCFakeAd rewarded = ads.AdMob.Ad(HDCAdUse.Rewarded);
            rewarded.Loaded();
            string calls = "";
            ads.Channels.Rewarded.Show("shop", () => calls += "rewarded,", () => calls += "closed");
            rewarded.Displayed();
            rewarded.Closed();
            Assert.AreEqual("closed", calls);
        }

        [Test]
        public void TheRewardedGatesMatchTheOldSystem()
        {
            HDCFakeAds notReady = Started(Ads());
            Assert.IsFalse(notReady.Channels.Rewarded.Show("shop", () => Assert.Fail("no reward")), "not loaded: false, no callback");

            HDCFakeAds off = Started(Ads(rewardedOn: false));
            Assert.IsFalse(off.Channels.Rewarded.Show("shop", () => Assert.Fail("no reward")));

            HDCFakeAds ignored = Started(Ads());
            ignored.Channels.Rewarded.IgnoreAds = true;
            string calls = "";
            Assert.IsTrue(ignored.Channels.Rewarded.Show("shop", () => calls += "rewarded,", () => calls += "closed"));
            Assert.AreEqual("rewarded,closed", calls, "IgnoreAds gives the reward at once");

            HDCFakeAds removed = Started(Ads());
            removed.Context.SetAdsRemoved(true);
            HDCFakeAd rewarded = removed.AdMob.Ad(HDCAdUse.Rewarded);
            rewarded.Loaded();
            Assert.IsTrue(removed.Channels.Rewarded.Show("shop", null), "removing ads keeps rewarded");
        }

        [Test]
        public void AManualRewardedLoadsOnlyWhenTheGameInitializesIt()
        {
            HDCFakeAds ads = Started(Ads(rewardedAutoInit: false));
            Assert.IsFalse(ads.AdMob.Ads.Any(ad => ad.Use == HDCAdUse.Rewarded && ad.Loads > 0));
            ads.Channels.Rewarded.Initialize();
            Assert.AreEqual(1, ads.AdMob.Ad(HDCAdUse.Rewarded).Loads);
        }

        private static HDCFakeAds Started(string adsConfig, string coreConfig = Core)
        {
            var ads = new HDCFakeAds();
            ads.Start(adsConfig, coreConfig);
            return ads;
        }

        private static string Json(bool value) => value ? "true" : "false";
    }
}
