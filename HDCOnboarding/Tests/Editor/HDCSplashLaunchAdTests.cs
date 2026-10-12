using System.Text.RegularExpressions;
using HDC.Ads;
using HDC.Ads.Domain;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace HDC.Onboarding.Tests
{
    public class HDCSplashLaunchAdTests
    {
        [TestCase(0, HDCAdFormat.Interstitial, HDCOnboardingAds.SplashInterstitialId, "inter-unit")]
        [TestCase(1, HDCAdFormat.Fullscreen, HDCOnboardingAds.SplashLaunchNativeId, "native-unit")]
        [TestCase(2, HDCAdFormat.AppOpen, HDCOnboardingAds.SplashLaunchAppOpenId, "open-unit")]
        public void EachLaunchTypeMakesItsOwnAd(int type, string format, string id, string adUnitId)
        {
            HDCSplashLaunchAd ad = new HDCSplashLaunchAd("it", Launch(type), 5f);
            try
            {
                Assert.AreEqual(format, ad.Ad.Format);
                Assert.AreEqual(id, ad.Ad.Id);
                Assert.AreEqual(adUnitId, ad.AdUnitId);
                Assert.AreEqual(type == 0, ad.IsInterstitial, "only the interstitial lets the native after show under it");
            }
            finally
            {
                ad.Destroy();
            }
        }

        [Test]
        public void AnUnknownTypeHasNoLaunchAd()
        {
            LogAssert.Expect(LogType.Error, new Regex("launchAdType 7 is not 0"));
            HDCSplashLaunchAd ad = new HDCSplashLaunchAd("it", Launch(7), 5f);
            try
            {
                Assert.AreEqual("", ad.AdUnitId, "the splash skips a launch ad without a unit");
            }
            finally
            {
                ad.Destroy();
            }
        }

        private static HDCSplashLaunchChannel Launch(int type)
        {
            var launch = new HDCSplashLaunchChannel { launchAdType = type };
            launch.forceAdUnit.admobUnit.id = "inter-unit";
            launch.forceAdUnit.nativeAdUnit.id = "native-unit";
            launch.forceAdUnit.nativeAdUnit.layoutGroupName = "cls";
            launch.appOpenUnit.admobUnit.id = "open-unit";
            return launch;
        }
    }
}
