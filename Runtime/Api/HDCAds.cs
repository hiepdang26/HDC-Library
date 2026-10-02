using System;
using HDC.Ads.Composition;
using HDC.Ads.Domain;
using HDC.Ads.Logic;
using HDC.Ads.Ports;

namespace HDC.Ads
{
    /// <summary>
    /// The ads API for game code: seven channels driven by the ads config and the ad core config. Force ads at
    /// game positions, rewarded, app launch and resume, banners, MREC and popups. Call <see cref="Initialize"/>
    /// once with both configs, usually fetched from Remote Config, then use the channels. Call everything from
    /// the main thread.
    /// </summary>
    public static class HDCAds
    {
        public static IForceAds ForceAd => Runtime.Channels.ForceAd;
        public static IRewardedAds Rewarded => Runtime.Channels.Rewarded;
        public static IAppLaunchAds AppLaunch => Runtime.Channels.AppLaunch;
        public static IAppResumeAds AppResume => Runtime.Channels.AppResume;
        public static IBannerAds Banner => Runtime.Channels.Banner;
        public static IMrecAds Mrec => Runtime.Channels.Mrec;
        public static IPopupAds Popup => Runtime.Channels.Popup;

        /// <summary>True once the SDK is ready and the channels have started.</summary>
        public static bool IsInitialized => Context.IsInitialized;

        /// <summary>Raised once the SDK is ready, after channels with autoInit have started loading.</summary>
        public static event Action Initialized
        {
            add => Context.Initialized += value;
            remove => Context.Initialized -= value;
        }

        /// <summary>
        /// Raised for every paid event: what an ad impression earned, with the channel and position that showed
        /// the ad. Forward it to analytics, such as Firebase's ad_impression or Adjust's ad revenue.
        /// </summary>
        public static event Action<HDCAdRevenue> Revenue
        {
            add => Context.Revenue += value;
            remove => Context.Revenue -= value;
        }

        /// <summary>True after the player bought ad removal. Rewarded ads still show.</summary>
        public static bool IsAdsRemoved => Context.IsAdsRemoved;

        /// <summary>
        /// Applies the ads config and the ad core config, both JSON in the Remote Config schema (see the README),
        /// and starts the SDK. Only the first call counts.
        /// </summary>
        public static void Initialize(string adsConfigJson, string coreConfigJson, Action onInitialized = null) =>
            Context.Initialize(adsConfigJson, coreConfigJson, onInitialized);

        /// <summary>Records the ad removal purchase, or undoes it, and hides the ads it covers.</summary>
        public static void SetAdsRemoved(bool removed) => Context.SetAdsRemoved(removed);

        /// <summary>Switches for testing ads. Leave them off in release builds.</summary>
        public static class Testing
        {
            /// <summary>Logs every ad command and event, in Unity and in the native log.</summary>
            public static bool DebugLog
            {
                get => Switches.DebugLog;
                set => Switches.DebugLog = value;
            }

            /// <summary>
            /// Serves Google test ads to this device, in every format including the native ones: requests keep
            /// the configured ad units and Google answers them with test ads. Call it before ads load: ads
            /// loaded earlier are live ads.
            /// </summary>
            public static void EnableTestDevice() => Switches.EnableTestDevice();

            /// <summary>True once <see cref="EnableTestDevice"/> made this device a Google test device.</summary>
            public static bool IsTestDevice => Switches.IsTestDevice;

            /// <summary>
            /// Loads every format from Google's sample ad units in place of the configured ones, to check the ad
            /// flows without them. Set it before ads load: ads already loaded keep their ad units.
            /// </summary>
            public static bool UseTestAdUnits
            {
                get => Switches.UseTestAdUnits;
                set => Switches.UseTestAdUnits = value;
            }

            /// <summary>
            /// Registers this device, plus <paramref name="extraDeviceHashes"/>, as Meta test devices. Call it
            /// before ads load: ads loaded earlier are not test ads. <paramref name="testAdType"/> picks Meta's
            /// test creative; 0 is the default one. Returns whether test mode is on, false without Meta.
            /// </summary>
            public static bool EnableMetaTestMode(string[] extraDeviceHashes = null, int testAdType = 0) =>
                Switches.EnableMetaTestMode(extraDeviceHashes, testAdType);

            /// <summary>Removes every Meta test device, so Meta serves live ads again.</summary>
            public static void DisableMetaTestMode() => Switches.DisableMetaTestMode();

            /// <summary>True while Meta test mode is on.</summary>
            public static bool IsMetaTestMode => Switches.IsMetaTestMode;

            /// <summary>This device's Meta test device hash, to register it from another one.</summary>
            public static string MetaTestDeviceHash => Switches.MetaTestDeviceHash;

            private static IAdsTesting Switches => Runtime.Testing;
        }

        // For the library's own tools: the debug panel, the setup prefab and the tests.

        internal static HDCChannels Channels => Runtime.Channels;
        internal static HDCAdsConfig Config => Context.Config;
        internal static HDCAdCoreConfig CoreConfig => Context.CoreConfig;

        private static HDCAdsRuntime Runtime => HDCAdsRuntime.Current;
        private static HDCAdsContext Context => Runtime.Context;
    }
}
