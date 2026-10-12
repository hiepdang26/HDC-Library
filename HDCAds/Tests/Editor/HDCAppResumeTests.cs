using System.Collections.Generic;
using System.Linq;
using HDC.Ads.Diagnostics;
using HDC.Ads.Domain;
using HDC.Ads.Logic;
using HDC.Ads.Ports;
using NUnit.Framework;

namespace HDC.Ads.Tests
{
    public class HDCAppResumeTests
    {
        private const string Units = @"""forceAdUnit"": { ""admobUnit"": { ""id"": ""resume-inter"" }, ""nativeAdUnit"": { ""id"": ""resume-native"" } },
    ""appOpenUnit"": { ""admobUnit"": { ""id"": ""resume-open"" } }";

        [Test]
        public void TypeZeroShowsAnAdMobInterstitialAfterTheAppComesBack()
        {
            HDCFakeAds ads = Start(0);
            HDCFakeAd ad = ads.AdMob.Ad(HDCAdUse.AppResume);
            Assert.AreEqual(HDCAdFormat.Interstitial, ad.Format);
            Assert.AreEqual("resume-inter", ad.AdUnitId);
            Assert.IsEmpty(ads.Native.Ads);

            ads.MainThread.Pause(true);
            Assert.AreEqual(1, ad.Loads, "the resume ad loads when the app goes to the background");
            ads.MainThread.Pause(false);
            ad.Loaded();
            Assert.AreEqual(1, ad.Shows, "and shows once it has loaded");
        }

        [Test]
        public void TypeOneShowsTheNativeFullScreenAd()
        {
            HDCFakeAds ads = Start(1);
            HDCFakeAd ad = ads.Native.Ad(HDCAdUse.AppResume);
            Assert.AreEqual(HDCAdFormat.Fullscreen, ad.Format);
            Assert.AreEqual("resume-native", ad.AdUnitId);
            Assert.IsEmpty(ads.AdMob.Ads);

            ads.MainThread.Pause(true);
            ads.MainThread.Pause(false);
            ad.Loaded();
            Assert.AreEqual(1, ad.Shows);
        }

        [Test]
        public void TypeTwoShowsAnAppOpenAd()
        {
            var revenues = new List<HDCAdRevenue>();
            HDCFakeAds ads = Start(2, revenues);
            HDCFakeAd ad = ads.AdMob.Ad(HDCAdUse.AppOpen);
            Assert.AreEqual(HDCAdFormat.AppOpen, ad.Format);
            Assert.AreEqual("resume-open", ad.AdUnitId);
            Assert.IsEmpty(ads.Native.Ads);

            ads.MainThread.Pause(true);
            ads.MainThread.Pause(false);
            ad.Loaded();
            ad.Displayed();
            ad.Paid(1000);
            Assert.AreEqual(1, ad.Shows);
            Assert.AreEqual(HDCAdChannel.AppResume, revenues.Single().Channel);
        }

        [Test]
        public void AnAdThatIsReadyWhenTheAppComesBackShowsAtOnce()
        {
            HDCFakeAds ads = Start(2);
            HDCFakeAd ad = ads.AdMob.Ad(HDCAdUse.AppOpen);
            ad.Loaded();
            Assert.AreEqual(0, ad.Shows, "nothing shows before the app went to the background");

            ads.MainThread.Pause(true);
            Assert.AreEqual(0, ad.Shows);
            ads.MainThread.Pause(false);
            Assert.AreEqual(1, ad.Shows, "a preloaded ad has no new Loaded event, so it shows on return");
            ad.Loaded();
            Assert.AreEqual(1, ad.Shows, "and only once");
        }

        [Test]
        public void TheResumeChannelIsReadFromTheAdCoreOnly()
        {
            var ads = new HDCFakeAds();
            ads.Start(@"{ ""appResumeChannel"": { ""isEnabled"": true, ""adUnitId"": ""old-unit"" } }",
                @"{ ""comebackChannel"": { ""resumeAdType"": 0 } }");
            ads.MainThread.Pause(true);

            Assert.IsFalse(ads.Channels.AppResume.IsInitialized);
            Assert.IsEmpty(ads.AdMob.Ads);
            Assert.IsEmpty(ads.Native.Ads);
        }

        [Test]
        public void AnUnknownTypeMakesNoAd()
        {
            HDCFakeAds ads = Start(3);
            Assert.IsFalse(ads.Channels.AppResume.IsInitialized);
            Assert.IsEmpty(ads.AdMob.Ads);
            Assert.IsEmpty(ads.Native.Ads);
        }

        [Test]
        public void TheConfigCheckNamesTheMissingUnitOfTheChosenType()
        {
            IConfigRule rule = ((IAdChannel)new HDCFakeAds().Channels.AppResume).ConfigRule;
            HDCAdsConfig ads = HDCAdsConfig.Parse("{}");

            Assert.IsEmpty(rule.Check(ads, Core(2, Units)));
            StringAssert.Contains("appResumeChannel.appOpenUnit.admobUnit.id", rule.Check(ads, Core(2, "")).Single().Text);
            StringAssert.Contains("appResumeChannel.forceAdUnit.admobUnit.id", rule.Check(ads, Core(0, "")).Single().Text);
            StringAssert.Contains("appResumeChannel.forceAdUnit.nativeAdUnit.id", rule.Check(ads, Core(1, "")).Single().Text);
            StringAssert.Contains("resumeAdType 4", rule.Check(ads, Core(4, Units)).Single().Text);
            Assert.IsEmpty(rule.Check(ads, HDCAdCoreConfig.Parse(@"{ ""appResumeChannel"": { ""isEnabled"": false, ""resumeAdType"": 2 } }")));
        }

        private static HDCFakeAds Start(int type, List<HDCAdRevenue> revenues = null)
        {
            var ads = new HDCFakeAds();
            if (revenues != null)
                ads.Context.Revenue += revenues.Add;
            ads.Start("{}", Json(type, Units));
            return ads;
        }

        private static HDCAdCoreConfig Core(int type, string units) => HDCAdCoreConfig.Parse(Json(type, units));

        private static string Json(int type, string units) =>
            @"{ ""appResumeChannel"": { ""isEnabled"": true, ""resumeAdType"": " + type + (units.Length > 0 ? ",\n    " + units : "") + " } }";
    }
}
