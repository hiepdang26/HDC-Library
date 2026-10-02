using System.Collections.Generic;
using HDC.Ads.Domain;
using HDC.Ads.Infrastructure;

namespace HDC.Ads.Logic
{
    /// <summary>
    /// The full-screen groups of the ad core config, made the first time a channel asks for one and kept for
    /// the session. A group holds a slot's ad units in show order.
    /// </summary>
    internal sealed class HDCAdGroups
    {
        // Priority values of force ads, rewarded and the bottom banner.
        internal const int PluginUnit = 0;
        internal const int NativeUnit = 1;
        private const int RemovedNetwork = 2;

        private readonly HDCAdsContext context;
        private readonly Dictionary<string, HDCFullscreenGroup> forceAdGroups = new Dictionary<string, HDCFullscreenGroup>();
        private HDCFullscreenGroup rewardedGroup;
        private HDCFullscreenGroup appOpenGroup;

        internal HDCAdGroups(HDCAdsContext context)
        {
            this.context = context;
        }

        /// <summary>The rewarded group once a channel made it, for the debug panel, which must not make any.</summary>
        internal HDCFullscreenGroup ExistingRewardedGroup => rewardedGroup;

        private HDCAdCoreConfig CoreConfig => context.CoreConfig;

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

        /// <summary>A force ad group once a channel made it, for the debug panel, which must not make any.</summary>
        internal HDCFullscreenGroup ExistingForceAdGroup(string groupName) =>
            !string.IsNullOrEmpty(groupName) && forceAdGroups.TryGetValue(groupName, out HDCFullscreenGroup group) ? group : null;

        internal HDCFullscreenGroup ForceAdGroup(string groupName)
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

            group = new HDCFullscreenGroup(context, groupName, sources, config.useBackup, config.maxShowCount);
            forceAdGroups[groupName] = group;
            return group;
        }

        /// <summary>Drops a force ad group, with its ads and show count, and loads it again. False while it shows.</summary>
        internal bool ReinitializeForceAdGroup(string groupName)
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

        internal HDCFullscreenGroup RewardedGroup()
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

            rewardedGroup = new HDCFullscreenGroup(context, "rewarded", sources, config.useBackup, 0);
            return rewardedGroup;
        }

        internal HDCFullscreenGroup AppOpenGroup()
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

            appOpenGroup = new HDCFullscreenGroup(context, "app_open", sources, config.useBackup, 0);
            return appOpenGroup;
        }

        private HDCFullscreenSource NativeForceAdSource(string groupName, HDCAdCoreConfig.ForceAdGroup config)
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
    }
}
