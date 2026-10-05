using HDC.Ads.Logic;
using HDC.Ads.Ports;
using NUnit.Framework;

namespace HDC.Ads.Tests
{
    public class HDCBannerRefreshTests
    {
        private const string Ads = @"{ ""bannerChannel"": { ""isEnabled"": true, ""fullBottom"": { ""isEnabled"": true } } }";

        [Test]
        public void AFailedRefreshHandsTheSlotToTheBackupUntilThePrimaryLoadsAgain()
        {
            HDCFakeAds ads = Start(0, 30, true);
            HDCFakeAd admob = ads.AdMob.Ad(HDCAdUse.Banner);
            HDCFakeAd native = ads.Native.Ad(HDCAdUse.Banner);
            admob.Loaded();
            Assert.IsTrue(ads.Channels.Banner.Show());

            admob.FailedToLoad();
            Assert.AreEqual(1, native.Loads, "the backup starts when the banner on screen fails to refresh");
            Assert.AreEqual(0, admob.Hides, "nothing to swap to yet, so the failing banner keeps the slot");

            native.Loaded();
            Assert.AreEqual(1, native.Shows);
            Assert.AreEqual(1, admob.Hides);

            ads.Wait(HDCRectGroup.RetryBaseSeconds);
            Assert.AreEqual(2, admob.Loads, "the demoted primary tries again");
            admob.Loaded();
            Assert.AreEqual(2, admob.Shows, "the primary takes the slot back once it loads");
            Assert.AreEqual(1, native.Hides);
        }

        [Test]
        public void ABackupShownLongAgoLoadsAFreshAdBeforeItTakesTheSlot()
        {
            HDCFakeAds ads = Start(0, 30, true);
            HDCFakeAd admob = ads.AdMob.Ad(HDCAdUse.Banner);
            HDCFakeAd native = ads.Native.Ad(HDCAdUse.Banner);
            admob.Loaded();
            ads.Channels.Banner.Show();
            admob.FailedToLoad();
            native.Loaded();
            ads.Wait(HDCRectGroup.RetryBaseSeconds);
            admob.Loaded();
            Assert.AreEqual(2, admob.Shows);

            admob.FailedToLoad();
            Assert.AreEqual(1, native.Shows, "the backup's ad was on screen too long to show again");
            Assert.AreEqual(2, native.Loads, "so it loads a fresh one");
            native.Loaded();
            Assert.AreEqual(2, native.Shows);
        }

        [Test]
        public void AShortRefreshNativeBannerGetsASecondChance()
        {
            HDCFakeAds ads = Start(1, 15, true);
            HDCFakeAd admob = ads.AdMob.Ad(HDCAdUse.Banner);
            HDCFakeAd native = ads.Native.Ad(HDCAdUse.Banner);
            native.Loaded();
            ads.Channels.Banner.Show();

            native.FailedToLoad();
            admob.Loaded();
            Assert.AreEqual(0, admob.Shows, "one failed refresh of a 15 s native banner is not enough");

            native.FailedToLoad();
            Assert.AreEqual(1, admob.Shows);
            Assert.AreEqual(1, native.Hides);
        }

        [Test]
        public void ASilentNativeBannerCountsAsAFailedRefresh()
        {
            HDCFakeAds ads = Start(1, 30, true);
            HDCFakeAd admob = ads.AdMob.Ad(HDCAdUse.Banner);
            HDCFakeAd native = ads.Native.Ad(HDCAdUse.Banner);
            native.Loaded();
            ads.Channels.Banner.Show();

            ads.Wait(34f);
            Assert.AreEqual(0, admob.Loads, "a 30 s native banner may stay silent up to 35 s");
            ads.Wait(2f);
            Assert.AreEqual(1, admob.Loads);
        }

        [Test]
        public void AnAdMobBannerIsNeverTimedOut()
        {
            HDCFakeAds ads = Start(0, 30, true);
            HDCFakeAd admob = ads.AdMob.Ad(HDCAdUse.Banner);
            HDCFakeAd native = ads.Native.Ad(HDCAdUse.Banner);
            admob.Loaded();
            ads.Channels.Banner.Show();
            ads.Wait(120f);
            Assert.AreEqual(0, native.Loads, "AdMob refreshes on the period set in the AdMob console");
        }

        [Test]
        public void WithoutABackupAFailedRefreshKeepsTheBannerOnScreen()
        {
            HDCFakeAds ads = Start(0, 30, false);
            HDCFakeAd admob = ads.AdMob.Ad(HDCAdUse.Banner);
            admob.Loaded();
            ads.Channels.Banner.Show();
            admob.FailedToLoad();
            ads.Wait(60f);
            Assert.IsEmpty(ads.Native.Ads);
            Assert.AreEqual(0, admob.Hides);
            Assert.AreEqual(1, admob.Loads, "the AdMob banner retries by itself");
        }

        private static HDCFakeAds Start(int priority, int reloadTime, bool useBackup)
        {
            string core = @"{ ""bannerUnit"": { ""fullBottom"": { ""mediationPriority"": " + priority + @", ""useBackup"": " + (useBackup ? "true" : "false") + @",
  ""admobUnit"": { ""id"": ""banner-unit"" }, ""androidUnit"": { ""id"": ""native-banner"", ""ids"": [ ""native-banner"" ], ""reloadTime"": " + reloadTime + @" } } } }";
            var ads = new HDCFakeAds();
            ads.Start(Ads, core);
            return ads;
        }
    }
}
