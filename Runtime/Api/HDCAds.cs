using System;
using System.Collections.Generic;
using HDC.Ads.Diagnostics;
using HDC.Ads.Domain;
using HDC.Ads.Infrastructure;
using HDC.Ads.Logic;
using UnityEngine;

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
        private const string RemovedAdsKey = "REMOVEADS";

        private static readonly Dictionary<string, HDCFullscreenGroup> forceAdGroups = new Dictionary<string, HDCFullscreenGroup>();
        private static HDCFullscreenGroup rewardedGroup;
        private static HDCFullscreenGroup appOpenGroup;
        private static bool initializeCalled;
        private static bool listening;
        private static bool? adsRemoved;

        public static IForceAds ForceAd => Channels.ForceAd;
        public static IRewardedAds Rewarded => Channels.Rewarded;
        public static IAppLaunchAds AppLaunch => Channels.AppLaunch;
        public static IAppResumeAds AppResume => Channels.AppResume;
        public static IBannerAds Banner => Channels.Banner;
        public static IMrecAds Mrec => Channels.Mrec;
        public static IPopupAds Popup => Channels.Popup;

        /// <summary>True once the SDK is ready and the channels have started.</summary>
        public static bool IsInitialized { get; private set; }

        /// <summary>Raised once the SDK is ready, after channels with autoInit have started loading.</summary>
        public static event Action Initialized;

        /// <summary>
        /// Raised for every paid event: what an ad impression earned, with the channel and position that showed
        /// the ad. Forward it to analytics, such as Firebase's ad_impression or Adjust's ad revenue.
        /// </summary>
        public static event Action<HDCAdRevenue> Revenue;

        /// <summary>True after the player bought ad removal. Rewarded ads still show.</summary>
        public static bool IsAdsRemoved
        {
            get
            {
                if (adsRemoved == null)
                {
                    try
                    {
                        adsRemoved = PlayerPrefs.GetInt(RemovedAdsKey, 0) == 1;
                    }
                    catch (Exception)
                    {
                        // PlayerPrefs refuses calls from constructors and field initializers; read it next time.
                        return false;
                    }
                }

                return adsRemoved.Value;
            }
        }

        /// <summary>
        /// Applies the ads config and the ad core config, both JSON in the Remote Config schema (see the README),
        /// and starts the SDK. Only the first call counts.
        /// </summary>
        public static void Initialize(string adsConfigJson, string coreConfigJson, Action onInitialized = null)
        {
            if (initializeCalled)
            {
                Debug.LogWarning("[HDCAds] HDCAds.Initialize was already called");
                if (onInitialized != null)
                {
                    if (IsInitialized)
                        onInitialized();
                    else
                        Initialized += onInitialized;
                }

                return;
            }

            initializeCalled = true;
            HDCConfigReport.Applied(adsConfigJson, coreConfigJson);
            Config = HDCAdsConfig.Parse(adsConfigJson);
            CoreConfig = HDCAdCoreConfig.Parse(coreConfigJson);
            if (onInitialized != null)
                Initialized += onInitialized;
            Listen();
            HDCAdsSdk.Initialize(OnSdkInitialized);
        }

        /// <summary>Records the ad removal purchase, or undoes it, and hides the ads it covers.</summary>
        public static void SetAdsRemoved(bool removed)
        {
            adsRemoved = removed;
            try
            {
                PlayerPrefs.SetInt(RemovedAdsKey, removed ? 1 : 0);
                PlayerPrefs.Save();
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[HDCAds] cannot save ad removal: " + exception.Message);
            }

            if (!removed)
                return;
            Channels.Banner.HideAll();
            Channels.Mrec.Hide();
            Channels.Popup.HideAll();
        }

        /// <summary>Switches for testing ads. Leave them off in release builds.</summary>
        public static class Testing
        {
            /// <summary>Logs every ad command and event, in Unity and in the native log.</summary>
            public static bool DebugLog
            {
                get => HDCAdsSdk.DebugLog;
                set => HDCAdsSdk.DebugLog = value;
            }

            /// <summary>
            /// Serves Google test ads to this device, in every format including the native ones: requests keep
            /// the configured ad units and Google answers them with test ads. Call it before ads load: ads
            /// loaded earlier are live ads.
            /// </summary>
            public static void EnableTestDevice() => HDCAdsSdk.EnableTestDevice();

            /// <summary>True once <see cref="EnableTestDevice"/> made this device a Google test device.</summary>
            public static bool IsTestDevice => HDCAdsSdk.IsTestDevice;

            /// <summary>
            /// Loads every format from Google's sample ad units in place of the configured ones, to check the ad
            /// flows without them. Set it before ads load: ads already loaded keep their ad units.
            /// </summary>
            public static bool UseTestAdUnits
            {
                get => HDCAdsSdk.UseTestAdUnits;
                set => HDCAdsSdk.UseTestAdUnits = value;
            }

            /// <summary>
            /// Registers this device, plus <paramref name="extraDeviceHashes"/>, as Meta test devices. Call it
            /// before ads load: ads loaded earlier are not test ads. <paramref name="testAdType"/> picks Meta's
            /// test creative; 0 is the default one. Returns whether test mode is on, false without Meta.
            /// </summary>
            public static bool EnableMetaTestMode(string[] extraDeviceHashes = null, int testAdType = 0) =>
                HDCAdsSdk.EnableMetaTestMode(extraDeviceHashes, testAdType);

            /// <summary>Removes every Meta test device, so Meta serves live ads again.</summary>
            public static void DisableMetaTestMode() => HDCAdsSdk.DisableMetaTestMode();

            /// <summary>True while Meta test mode is on.</summary>
            public static bool IsMetaTestMode => HDCAdsSdk.IsMetaTestMode();

            /// <summary>This device's Meta test device hash, to register it from another one.</summary>
            public static string MetaTestDeviceHash => HDCAdsSdk.GetMetaTestDeviceHash();
        }

        // The rest is for the library's own code.

        /// <summary>The channels behind the interfaces above.</summary>
        internal static HDCChannels Channels { get; private set; } = new HDCChannels();

        internal static HDCAdsConfig Config { get; private set; } = new HDCAdsConfig();
        internal static HDCAdCoreConfig CoreConfig { get; private set; } = new HDCAdCoreConfig();

        /// <summary>Real time, in seconds since startup, when the last full-screen ad closed.</summary>
        internal static float LastFullscreenAdTime { get; private set; }

        /// <summary>Called right before any channel shows a full-screen ad.</summary>
        internal static event Action FullscreenOpening;

        /// <summary>Called when the player taps a banner, which may take them out of the app.</summary>
        internal static event Action BannerClicked;

        internal static void NotifyFullscreenOpening() => FullscreenOpening?.Invoke();

        // The groups made so far, for the debug panel, which must not make any.
        internal static HDCFullscreenGroup ExistingForceAdGroup(string groupName) =>
            !string.IsNullOrEmpty(groupName) && forceAdGroups.TryGetValue(groupName, out HDCFullscreenGroup group) ? group : null;

        internal static HDCFullscreenGroup ExistingRewardedGroup => rewardedGroup;

        internal static HDCFullscreenGroup ForceAdGroup(string groupName)
        {
            if (string.IsNullOrEmpty(groupName))
                return null;
            if (forceAdGroups.TryGetValue(groupName, out HDCFullscreenGroup group))
                return group;

            HDCAdCoreConfig.ForceAdGroup config = CoreConfig.ForceAdGroupNamed(groupName);
            if (config == null)
                return null;

            var sources = new List<HDCFullscreenSource>();
            foreach (int priority in Order(config.mediationPriority, config.useBackup))
            {
                if (priority == PluginUnit && !string.IsNullOrEmpty(config.admobUnit?.id))
                {
                    var ad = new HDCGmaInterstitialAd("fa_plugin_" + groupName, config.admobUnit.id, config.admobUnit.preloadAd, config.admobUnit.adBufferSize)
                    {
                        LoadOnce = config.disablePostInitReload && !config.admobUnit.preloadAd,
                    };
                    sources.Add(new HDCPluginFullscreenSource(HDCAdFormat.Interstitial, ad));
                }
                else if (priority == NativeUnit && !string.IsNullOrEmpty(config.androidUnit?.id))
                {
                    sources.Add(NativeForceAdSource(groupName, config));
                }
            }

            group = new HDCFullscreenGroup(groupName, sources, config.useBackup, config.maxShowCount);
            forceAdGroups[groupName] = group;
            return group;
        }

        /// <summary>Drops a force ad group, with its ads and show count, and loads it again. False while it shows.</summary>
        internal static bool ReinitializeForceAdGroup(string groupName)
        {
            if (string.IsNullOrEmpty(groupName))
                return false;
            if (forceAdGroups.TryGetValue(groupName, out HDCFullscreenGroup old))
            {
                if (old.IsShowing)
                    return false;
                old.Destroy();
                forceAdGroups.Remove(groupName);
            }

            HDCFullscreenGroup group = ForceAdGroup(groupName);
            group?.Initialize();
            return group != null;
        }

        internal static HDCFullscreenGroup RewardedGroup()
        {
            if (rewardedGroup != null)
                return rewardedGroup;

            HDCAdCoreConfig.FullscreenUnit config = CoreConfig.rewardedUnit ?? new HDCAdCoreConfig.FullscreenUnit();
            var sources = new List<HDCFullscreenSource>();
            foreach (int priority in Order(config.mediationPriority, config.useBackup))
            {
                if (priority == PluginUnit && !string.IsNullOrEmpty(config.admobUnit?.id))
                {
                    var ad = new HDCGmaRewardedAd("rw_plugin", config.admobUnit.id, config.admobUnit.preloadAd, config.admobUnit.adBufferSize);
                    sources.Add(new HDCPluginFullscreenSource(HDCAdFormat.Rewarded, ad));
                }
                else if (priority == NativeUnit && !string.IsNullOrEmpty(config.androidUnit?.id))
                {
                    // A native full-screen ad served as rewarded: the reward comes when it closes.
                    var picker = new HDCLayoutPicker(CoreConfig, config.androidUnit.layoutGroupName);
                    sources.Add(new HDCNativeFullscreenSource("rw_native", config.androidUnit.id, true, picker) { RewardsOnClose = true });
                }
            }

            rewardedGroup = new HDCFullscreenGroup("rewarded", sources, config.useBackup, 0);
            return rewardedGroup;
        }

        internal static HDCFullscreenGroup AppOpenGroup()
        {
            if (appOpenGroup != null)
                return appOpenGroup;

            // App open units only come from the plugin: priority 0 picks it, anything else needs backups on.
            HDCAdCoreConfig.FullscreenUnit config = CoreConfig.appOpenUnit ?? new HDCAdCoreConfig.FullscreenUnit();
            var sources = new List<HDCFullscreenSource>();
            if ((config.mediationPriority == PluginUnit || config.useBackup) && !string.IsNullOrEmpty(config.admobUnit?.id))
            {
                var ad = new HDCGmaAppOpenAd("ao_plugin", config.admobUnit.id, config.admobUnit.preloadAd, config.admobUnit.adBufferSize);
                sources.Add(new HDCPluginFullscreenSource(HDCAdFormat.AppOpen, ad));
            }

            appOpenGroup = new HDCFullscreenGroup("app_open", sources, config.useBackup, 0);
            return appOpenGroup;
        }

        // Priority values of force ads, rewarded and the bottom banner.
        internal const int PluginUnit = 0;
        internal const int NativeUnit = 1;
        private const int RemovedNetwork = 2;

        /// <summary>
        /// Units in show order: the chosen one, then with backups the rest in the order plugin, native. Units of
        /// networks no longer served are left out.
        /// </summary>
        internal static IEnumerable<int> Order(int priority, bool useBackup)
        {
            var order = new List<int> { priority };
            if (useBackup)
            {
                foreach (int unit in new[] { PluginUnit, RemovedNetwork, NativeUnit })
                {
                    if (!order.Contains(unit))
                        order.Add(unit);
                }
            }

            order.Remove(RemovedNetwork);
            return order;
        }

        private static HDCFullscreenSource NativeForceAdSource(string groupName, HDCAdCoreConfig.ForceAdGroup config)
        {
            HDCAdCoreConfig.NativeUnit unit = config.androidUnit;
            HDCAdCoreConfig.Interstitials interstitials = unit.androidInterstitials;
            if (interstitials != null && interstitials.switchToInterstitialAndroid)
            {
                int bufferSize = interstitials.isPreloadAd && interstitials.bufferSize > 0 ? interstitials.bufferSize : 1;
                return new HDCNativeInterstitialSource("fa_interstitial_" + groupName, unit.id, bufferSize, !config.disablePostInitReload);
            }

            var picker = new HDCLayoutPicker(CoreConfig, unit.layoutGroupName);
            return new HDCNativeFullscreenSource("fa_" + groupName, unit.id, !config.disablePostInitReload, picker);
        }

        private static void OnSdkInitialized()
        {
            IsInitialized = true;
            Channels.AppLaunch.OnSdkInitialized();
            Channels.AppResume.OnSdkInitialized();
            Channels.ForceAd.OnSdkInitialized();
            Channels.Rewarded.OnSdkInitialized();
            Channels.Banner.OnSdkInitialized();
            Channels.Mrec.OnSdkInitialized();
            Channels.Popup.OnSdkInitialized();
            Action callbacks = Initialized;
            Initialized = null;
            if (callbacks == null)
                return;
            foreach (Action callback in callbacks.GetInvocationList())
            {
                try
                {
                    callback();
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// Puts every static of the library back to its start state when Play Mode starts. Without a domain
        /// reload (Enter Play Mode Options, the default of new Unity 6.6 projects), statics keep the last
        /// session's ads, callbacks and subscribers. Players always start fresh, so this is Editor only.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForPlayMode()
        {
            // By now this class has made its channels, which subscribe to events: clear the events, then make
            // the channels again.
            HDCMainThread.ResetStatics();
            HDCGma.ResetStatics();
            HDCAdsSdk.ResetStatics();
            HDCConfigReport.Reset();
            HDCAdPlacements.Reset();

            forceAdGroups.Clear();
            rewardedGroup = null;
            appOpenGroup = null;
            initializeCalled = false;
            listening = false;
            adsRemoved = null;
            Initialized = null;
            Revenue = null;
            FullscreenOpening = null;
            BannerClicked = null;
            Config = new HDCAdsConfig();
            CoreConfig = new HDCAdCoreConfig();
            IsInitialized = false;
            LastFullscreenAdTime = 0f;

            Channels = new HDCChannels();
        }
#endif

        private static void Listen()
        {
            if (listening)
                return;
            listening = true;
            HDCAdsSdk.AdEvent += adEvent =>
            {
                if (adEvent.type == HDCAdEventType.Paid)
                {
                    RaiseRevenue(adEvent);
                    return;
                }

                bool fullscreen = adEvent.format == HDCAdFormat.Interstitial || adEvent.format == HDCAdFormat.Fullscreen
                    || adEvent.format == HDCAdFormat.Rewarded || adEvent.format == HDCAdFormat.AppOpen;
                if (fullscreen && adEvent.type == HDCAdEventType.Closed)
                    LastFullscreenAdTime = Time.realtimeSinceStartup;
                else if (adEvent.type == HDCAdEventType.Clicked
                    && (adEvent.format == HDCAdFormat.Banner || adEvent.format == HDCAdFormat.BannerView))
                    BannerClicked?.Invoke();
            };
        }

        private static void RaiseRevenue(HDCAdEvent adEvent)
        {
            Action<HDCAdRevenue> handlers = Revenue;
            if (handlers == null)
                return;

            (HDCAdChannel channel, string position) = HDCAdPlacements.Find(adEvent.id);
            var revenue = new HDCAdRevenue(channel, position, adEvent.format, HDCAdRevenue.AdMob, adEvent.adSource,
                adEvent.adUnitId, adEvent.Revenue, adEvent.currency, adEvent.precision);
            // One handler's exception must not keep the revenue from the others.
            foreach (Action<HDCAdRevenue> handler in handlers.GetInvocationList())
                HDCAdsLog.Run(() => handler(revenue));
        }
    }
}
