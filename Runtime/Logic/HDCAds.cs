using System;
using System.Collections.Generic;
using HDC.Ads.Internal;
using UnityEngine;

namespace HDC.Ads
{
    /// <summary>
    /// Ads by channel, driven by the ads config and the ad core config: force ads at game positions, rewarded,
    /// app launch and resume, banners, MREC and popups. Call <see cref="Initialize"/> once with both configs,
    /// usually fetched from Remote Config, then use the channels. Call everything from the main thread.
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

        public static HDCForceAds ForceAd { get; private set; } = new HDCForceAds();
        public static HDCRewardedAds Rewarded { get; private set; } = new HDCRewardedAds();
        public static HDCAppLaunchAds AppLaunch { get; private set; } = new HDCAppLaunchAds();
        public static HDCAppResumeAds AppResume { get; private set; } = new HDCAppResumeAds();
        public static HDCBannerAds Banner { get; private set; } = new HDCBannerAds();
        public static HDCMrecAds Mrec { get; private set; } = new HDCMrecAds();
        public static HDCPopupAds Popup { get; private set; } = new HDCPopupAds();

        public static HDCAdsConfig Config { get; private set; } = new HDCAdsConfig();
        public static HDCAdCoreConfig CoreConfig { get; private set; } = new HDCAdCoreConfig();

        /// <summary>True once the SDK is ready and the channels have started.</summary>
        public static bool IsInitialized { get; private set; }

        /// <summary>Raised once the SDK is ready, after channels with autoInit have started loading.</summary>
        public static event Action Initialized;

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

        /// <summary>Real time, in seconds since startup, when the last full-screen ad closed.</summary>
        public static float LastFullscreenAdTime { get; private set; }

        /// <summary>
        /// Applies the ads config and the ad core config (JSON, see <see cref="HDCAdsConfig"/> and
        /// <see cref="HDCAdCoreConfig"/>) and starts the SDK. Only the first call counts.
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
            Banner.HideAll();
            Mrec.Hide();
            Popup.HideAll();
        }

        /// <summary>Called right before any channel shows a full-screen ad.</summary>
        internal static event Action FullscreenOpening;

        /// <summary>Called when the player taps a banner, which may take them out of the app.</summary>
        internal static event Action BannerClicked;

        internal static void NotifyFullscreenOpening() => FullscreenOpening?.Invoke();

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
            AppLaunch.OnSdkInitialized();
            AppResume.OnSdkInitialized();
            ForceAd.OnSdkInitialized();
            Rewarded.OnSdkInitialized();
            Banner.OnSdkInitialized();
            Mrec.OnSdkInitialized();
            Popup.OnSdkInitialized();
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

            forceAdGroups.Clear();
            rewardedGroup = null;
            appOpenGroup = null;
            initializeCalled = false;
            listening = false;
            adsRemoved = null;
            Initialized = null;
            FullscreenOpening = null;
            BannerClicked = null;
            Config = new HDCAdsConfig();
            CoreConfig = new HDCAdCoreConfig();
            IsInitialized = false;
            LastFullscreenAdTime = 0f;

            ForceAd = new HDCForceAds();
            Rewarded = new HDCRewardedAds();
            AppLaunch = new HDCAppLaunchAds();
            AppResume = new HDCAppResumeAds();
            Banner = new HDCBannerAds();
            Mrec = new HDCMrecAds();
            Popup = new HDCPopupAds();
        }
#endif

        private static void Listen()
        {
            if (listening)
                return;
            listening = true;
            HDCAdsSdk.AdEvent += adEvent =>
            {
                bool fullscreen = adEvent.format == HDCAdFormat.Interstitial || adEvent.format == HDCAdFormat.Fullscreen
                    || adEvent.format == HDCAdFormat.Rewarded || adEvent.format == HDCAdFormat.AppOpen;
                if (fullscreen && adEvent.type == HDCAdEventType.Closed)
                    LastFullscreenAdTime = Time.realtimeSinceStartup;
                else if (adEvent.type == HDCAdEventType.Clicked
                    && (adEvent.format == HDCAdFormat.Banner || adEvent.format == HDCAdFormat.BannerView))
                    BannerClicked?.Invoke();
            };
        }
    }
}
