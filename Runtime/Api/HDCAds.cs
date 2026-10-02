using System;
using HDC.Ads.Composition;
using HDC.Ads.Domain;
using HDC.Ads.Logic;
using HDC.Ads.Ports;

namespace HDC.Ads
{
    public static class HDCAds
    {
        public static IForceAds ForceAd => Runtime.Channels.ForceAd;
        public static IRewardedAds Rewarded => Runtime.Channels.Rewarded;
        public static IAppLaunchAds AppLaunch => Runtime.Channels.AppLaunch;
        public static IAppResumeAds AppResume => Runtime.Channels.AppResume;
        public static IBannerAds Banner => Runtime.Channels.Banner;
        public static IMrecAds Mrec => Runtime.Channels.Mrec;
        public static IPopupAds Popup => Runtime.Channels.Popup;

        public static bool IsInitialized => Context.IsInitialized;

        public static event Action Initialized
        {
            add => Context.Initialized += value;
            remove => Context.Initialized -= value;
        }

        public static event Action<HDCAdRevenue> Revenue
        {
            add => Context.Revenue += value;
            remove => Context.Revenue -= value;
        }

        public static bool IsAdsRemoved => Context.IsAdsRemoved;

        public static void Initialize(string adsConfigJson, string coreConfigJson, Action onInitialized = null) =>
            Context.Initialize(adsConfigJson, coreConfigJson, onInitialized);

        public static void SetAdsRemoved(bool removed) => Context.SetAdsRemoved(removed);

        public static class Testing
        {
            public static bool DebugLog
            {
                get => Switches.DebugLog;
                set => Switches.DebugLog = value;
            }

            public static void EnableTestDevice() => Switches.EnableTestDevice();

            public static bool IsTestDevice => Switches.IsTestDevice;

            public static bool UseTestAdUnits
            {
                get => Switches.UseTestAdUnits;
                set => Switches.UseTestAdUnits = value;
            }

            public static bool EnableMetaTestMode(string[] extraDeviceHashes = null, int testAdType = 0) =>
                Switches.EnableMetaTestMode(extraDeviceHashes, testAdType);

            public static void DisableMetaTestMode() => Switches.DisableMetaTestMode();

            public static bool IsMetaTestMode => Switches.IsMetaTestMode;

            public static string MetaTestDeviceHash => Switches.MetaTestDeviceHash;

            private static IAdsTesting Switches => Runtime.Testing;
        }

        internal static HDCChannels Channels => Runtime.Channels;
        internal static HDCAdsConfig Config => Context.Config;
        internal static HDCAdCoreConfig CoreConfig => Context.CoreConfig;

        private static HDCAdsRuntime Runtime => HDCAdsRuntime.Current;
        private static HDCAdsContext Context => Runtime.Context;
    }
}
