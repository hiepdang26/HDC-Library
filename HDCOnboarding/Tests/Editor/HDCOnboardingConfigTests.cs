using System.Text.RegularExpressions;
using HDC.Onboarding.Editor;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace HDC.Onboarding.Tests
{
    public class HDCOnboardingConfigTests
    {
        private const string OldIntroConfig =
            "{\"nextSceneName\":\"SampleScene\",\"Configs\":[{\"IntroType\":1,\"AdConfig\":{\"PreloadType\":0,\"GroupName\":\"native_intro1\"}," +
            "\"TimeDelayPreload\":0,\"IntroInterval\":5,\"IntroDescription\":\"Welcome\",\"IntroCoverImage\":\"cover_1\",\"IntroBackgroundImage\":\"back_1\"}," +
            "{\"IntroType\":0,\"AdConfig\":{\"PreloadType\":0,\"GroupName\":\"native_intro2\"},\"TimeDelayPreload\":0,\"IntroInterval\":5}," +
            "{\"IntroType\":5,\"AdConfig\":{\"PreloadType\":1,\"GroupName\":\"native_intro_full\"},\"TimeDelayPreload\":0,\"IntroBackgroundImage\":\"back_4\"}]," +
            "\"Profile\":{\"isEnable\":false},\"Avatar\":{\"isEnable\":false}}";

        private const string OldSplashConfig =
            "{\n  \"loadingTime\": 25,\n  \"nativeSplashGroup\": \"native_splash\",\n  \"nativeSplashShowTime\": 5,\n  \"nativeSplashLoadTimeOut\": 20,\n" +
            "  \"enableAdmobLayout\": false,\n  \"interSplashAdUnitId\": \"ca-app-pub-3940256099942544/4411468910\",\n  \"waitNativeSplashLoad\": false,\n" +
            "  \"interLoadAdTimeout\": 25,\n  \"delayShowInterstitials\": 0.1,\n  \"nativeAfterInterId\": \"ca-app-pub-3940256099942544/3986624511\",\n" +
            "  \"nativeAfterLayoutGroup\": \"cls\",\n  \"firstOpenNextScene\": \"Intro\",\n  \"nextScene\": \"SampleScene\"\n}";

        private static string Onboarding(string splash, string intro) =>
            "{\"splash_config\":" + (splash ?? "{}") + ",\"intro_config\":" + (intro ?? "{}") + "}";

        [Test]
        public void TheOldIntroConfigReadsTheSameInsideOnboardingConfig()
        {
            HDCIntroConfig config = HDCOnboardingConfig.Parse(Onboarding(null, OldIntroConfig)).intro_config;

            Assert.AreEqual("SampleScene", config.nextSceneName);
            Assert.AreEqual(3, config.Configs.Length);
            Assert.AreEqual(HDCIntroType.AL, config.Configs[0].IntroType);
            Assert.AreEqual(HDCIntroPreload.Popup, config.Configs[0].AdConfig.PreloadType);
            Assert.AreEqual("native_intro1", config.Configs[0].AdConfig.GroupName);
            Assert.AreEqual(5f, config.Configs[0].IntroInterval);
            Assert.AreEqual("cover_1", config.Configs[0].IntroCoverImage);
            Assert.AreEqual(HDCIntroType.AR, config.Configs[1].IntroType);
            Assert.AreEqual(HDCIntroType.FORCE_AD, config.Configs[2].IntroType);
            Assert.AreEqual(HDCIntroPreload.ForceAd, config.Configs[2].AdConfig.PreloadType);
            Assert.AreEqual("native_intro_full", config.Configs[2].AdConfig.GroupName);
        }

        [Test]
        public void TheOldSplashConfigReadsTheSameInsideOnboardingConfig()
        {
            HDCSplashConfig config = HDCOnboardingConfig.Parse(Onboarding(OldSplashConfig, OldIntroConfig)).splash_config;

            Assert.AreEqual(25f, config.loadingTime);
            Assert.AreEqual("native_splash", config.nativeSplashGroup);
            Assert.AreEqual(5f, config.nativeSplashShowTime);
            Assert.AreEqual(20f, config.nativeSplashLoadTimeOut);
            Assert.AreEqual("", config.launchChannel.AdUnitId, "interSplashAdUnitId is not read any more: the launch ad lives in launchChannel");
            Assert.AreEqual(25f, config.interLoadAdTimeout);
            Assert.AreEqual(0.1f, config.delayShowInterstitials, 1e-6f);
            Assert.AreEqual("cls", config.nativeAfterLayoutGroup);
            Assert.AreEqual("Intro", config.firstOpenNextScene);
            Assert.AreEqual("SampleScene", config.nextScene);
        }

        [Test]
        public void TheLaunchChannelPicksTheUnitOfItsType()
        {
            const string units = "\"forceAdUnit\":{\"admobUnit\":{\"id\":\"inter-unit\"},\"nativeAdUnit\":{\"id\":\"native-unit\",\"layoutGroupName\":\"cls\"}}," +
                                 "\"appOpenUnit\":{\"admobUnit\":{\"id\":\"open-unit\"}}";

            HDCSplashLaunchChannel Launch(int type) =>
                HDCOnboardingConfig.Parse(Onboarding("{\"launchChannel\":{\"launchAdType\":" + type + "," + units + "}}", null)).splash_config.launchChannel;

            Assert.AreEqual("inter-unit", Launch(0).AdUnitId);
            Assert.AreEqual("native-unit", Launch(1).AdUnitId);
            Assert.AreEqual("cls", Launch(1).LayoutGroup);
            Assert.AreEqual("open-unit", Launch(2).AdUnitId);
            Assert.IsFalse(Launch(3).IsKnownType);
            Assert.AreEqual("", Launch(3).AdUnitId, "an unknown type has no launch ad");
        }

        [Test]
        public void ProfileAndAvatarOfAnOldConfigAreIgnored()
        {
            HDCIntroConfig config = HDCOnboardingConfig.Parse(Onboarding(null,
                "{\"nextSceneName\":\"Game\",\"Configs\":[{\"IntroType\":3},{\"IntroType\":\"AVATAR\"},{\"IntroType\":5}]," +
                "\"Profile\":{\"isEnable\":true,\"PopupConfig\":\"p\"},\"Avatar\":{\"isEnable\":true}}")).intro_config;

            Assert.AreEqual("Game", config.nextSceneName);
            Assert.AreEqual(3, config.Configs.Length);
            Assert.AreEqual(3, (int)config.Configs[0].IntroType, "an old PROFILE page keeps its number and has no view");
            Assert.AreEqual(-1, (int)config.Configs[1].IntroType, "an unknown page name has no view either");
            Assert.AreEqual(HDCIntroType.FORCE_AD, config.Configs[2].IntroType);
        }

        [Test]
        public void EnumNamesReadLikeNumbers()
        {
            HDCIntroConfig config = HDCOnboardingConfig.Parse(Onboarding(null,
                "{\"Configs\":[{\"IntroType\":\"PT\",\"AdConfig\":{\"PreloadType\":\"ForceAd\",\"GroupName\":\"g\"}}," +
                "{\"IntroType\" : \"force_ad\",\"AdConfig\":{\"PreloadType\":\"Popup\"}}]}")).intro_config;

            Assert.AreEqual(HDCIntroType.PT, config.Configs[0].IntroType);
            Assert.AreEqual(HDCIntroPreload.ForceAd, config.Configs[0].AdConfig.PreloadType);
            Assert.AreEqual(HDCIntroType.FORCE_AD, config.Configs[1].IntroType);
            Assert.AreEqual(HDCIntroPreload.Popup, config.Configs[1].AdConfig.PreloadType);
        }

        [Test]
        public void AMissingPartOrBrokenConfigReadsEmpty()
        {
            HDCOnboardingConfig empty = HDCOnboardingConfig.Parse("");
            Assert.AreEqual(0, empty.intro_config.Configs.Length);
            Assert.IsNull(empty.splash_config.nativeSplashGroup);

            HDCOnboardingConfig introOnly = HDCOnboardingConfig.Parse("{\"intro_config\":{\"nextSceneName\":\"Game\"}}");
            Assert.AreEqual("Game", introOnly.intro_config.nextSceneName);
            Assert.AreEqual(0, introOnly.intro_config.Configs.Length);
            Assert.IsNotNull(introOnly.splash_config);
            Assert.AreEqual("", introOnly.splash_config.launchChannel.AdUnitId);

            LogAssert.Expect(LogType.Error, new Regex("onboarding_config is not valid JSON"));
            Assert.AreEqual(0, HDCOnboardingConfig.Parse("{not json").intro_config.Configs.Length);
        }

        [Test]
        public void TheDefaultConfigKeyIsValid()
        {
            HDCOnboardingConfig config = HDCOnboardingConfig.Parse(HDCOnboardingConfigKeys.OnboardingConfig(
                HDCOnboardingConfigKeys.SplashConfig("inter", "native", "Game"), HDCOnboardingConfigKeys.IntroConfig("Game")));

            Assert.AreEqual(0, config.splash_config.launchChannel.launchAdType);
            Assert.AreEqual("inter", config.splash_config.launchChannel.AdUnitId);
            Assert.AreEqual("native", config.splash_config.nativeAfterInterId);
            Assert.AreEqual("Intro", config.splash_config.firstOpenNextScene);
            Assert.AreEqual("Game", config.splash_config.nextScene);
            Assert.AreEqual("Game", config.intro_config.nextSceneName);
            Assert.AreEqual(3, config.intro_config.Configs.Length);
            Assert.AreEqual(HDCIntroType.FORCE_AD, config.intro_config.Configs[2].IntroType);
        }
    }
}
