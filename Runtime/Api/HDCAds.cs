using System;
using System.Collections.Generic;
using HDC.Ads.Composition;
using HDC.Ads.Diagnostics;
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

            public static IReadOnlyList<string> Groups(HDCAdChannel channel) => ConfigNames(channel)?.Groups() ?? NoNames;

            public static IReadOnlyList<string> Positions(HDCAdChannel channel, string group = null) =>
                ConfigNames(channel)?.Positions(group ?? string.Empty) ?? NoNames;

            private static readonly string[] NoNames = new string[0];

            private static IAdsTesting Switches => Runtime.Testing;

            private static IChannelDiagnostics ConfigNames(HDCAdChannel channel)
            {
                switch (channel)
                {
                    case HDCAdChannel.ForceAd:
                        return ((IAdChannel)Channels.ForceAd).Diagnostics;
                    case HDCAdChannel.Popup:
                        return ((IAdChannel)Channels.Popup).Diagnostics;
                    default:
                        return null;
                }
            }
        }

        internal static HDCChannels Channels => Runtime.Channels;
        internal static HDCAdsConfig Config => Context.Config;
        internal static HDCAdCoreConfig CoreConfig => Context.CoreConfig;

        private static HDCAdsRuntime Runtime => HDCAdsRuntime.Current;
        private static HDCAdsContext Context => Runtime.Context;
    }
}
