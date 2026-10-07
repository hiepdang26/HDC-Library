using System;
using System.Collections.Generic;
using System.Linq;
using HDC.Ads.Domain;
using HDC.Ads.Ports;

namespace HDC.Ads.Logic
{
    internal sealed class HDCAdGroups
    {
        private readonly HDCAdsContext context;
        private readonly Dictionary<string, HDCFullscreenGroup> forceAdGroups = new Dictionary<string, HDCFullscreenGroup>();
        private HDCFullscreenGroup rewardedGroup;
        private HDCFullscreenGroup appOpenGroup;

        internal HDCAdGroups(HDCAdsContext context)
        {
            this.context = context;
        }

        internal HDCFullscreenGroup ExistingRewardedGroup => rewardedGroup;

        private HDCAdCoreConfig CoreConfig => context.CoreConfig;

        internal IReadOnlyList<HDCAdPlan> ForceAdPlans(string groupName)
        {
            HDCAdCoreConfig.ForceAdGroup config = CoreConfig.ForceAdGroupNamed(groupName);
            return config == null
                ? new HDCAdPlan[0]
                : Plans(HDCAdUse.ForceAd, groupName, config.mediationPriority, config.useBackup, config.UnitFor, !config.disablePostInitReload);
        }

        internal IReadOnlyList<HDCAdPlan> RewardedPlans()
        {
            HDCAdCoreConfig.FullscreenUnit config = CoreConfig.rewardedUnit ?? new HDCAdCoreConfig.FullscreenUnit();
            return Plans(HDCAdUse.Rewarded, "", config.mediationPriority, config.useBackup, config.UnitFor, true);
        }

        internal IReadOnlyList<HDCAdPlan> AppOpenPlans()
        {
            HDCAdCoreConfig.FullscreenUnit config = CoreConfig.appOpenUnit ?? new HDCAdCoreConfig.FullscreenUnit();
            return Plans(HDCAdUse.AppOpen, "", config.mediationPriority, config.useBackup, config.UnitFor, true);
        }

        internal IReadOnlyList<HDCAdPlan> BannerPlans(HDCBannerSlot slot)
        {
            HDCAdCoreConfig.FullscreenUnit config = CoreConfig.bannerUnit?.Slot(slot) ?? new HDCAdCoreConfig.FullscreenUnit();
            return Plans(HDCAdUse.Banner, "", config.mediationPriority, config.useBackup, config.UnitFor, true, slot);
        }

        internal IReadOnlyList<HDCAdPlan> MrecPlans()
        {
            HDCAdCoreConfig.FullscreenUnit config = CoreConfig.mrecUnit ?? new HDCAdCoreConfig.FullscreenUnit();
            return Plans(HDCAdUse.Mrec, "", config.mediationPriority, config.useBackup, config.UnitFor, true);
        }

        internal HDCAdPlan PopupPlan(string groupName)
        {
            HDCAdCoreConfig.PopupGroup config = CoreConfig.PopupGroupNamed(groupName);
            return config == null ? null : FirstPlan(HDCAdUse.Popup, groupName, config.UnitFor, !config.disablePostInitReload);
        }

        internal HDCAdPlan ResumePlan() =>
            FirstPlan(HDCAdUse.AppResume, "", (context.Config.appResumeChannel ?? new HDCAdsConfig.AppResumeChannel()).UnitFor, false);

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

            group = new HDCFullscreenGroup(context, groupName, FullscreenSources(ForceAdPlans(groupName), context.Log), config.useBackup, config.maxShowCount);
            forceAdGroups[groupName] = group;
            return group;
        }

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
            List<HDCFullscreenSource> sources = FullscreenSources(RewardedPlans(), context.Log);
            foreach (HDCFullscreenSource source in sources)
                source.RewardsOnClose = source.Format != HDCAdFormat.Rewarded;
            rewardedGroup = new HDCFullscreenGroup(context, "rewarded", sources, config.useBackup, 0);
            return rewardedGroup;
        }

        internal HDCFullscreenGroup AppOpenGroup()
        {
            if (appOpenGroup != null)
                return appOpenGroup;

            HDCAdCoreConfig.FullscreenUnit config = CoreConfig.appOpenUnit ?? new HDCAdCoreConfig.FullscreenUnit();
            appOpenGroup = new HDCFullscreenGroup(context, "app_open", FullscreenSources(AppOpenPlans(), context.Log), config.useBackup, 0);
            return appOpenGroup;
        }

        internal static List<HDCFullscreenSource> FullscreenSources(IEnumerable<HDCAdPlan> plans, IAdsLog log) =>
            plans.Select(plan => new HDCFullscreenSource(plan.Network.CreateFullscreen(plan), plan.Network, log)).ToList();

        internal static List<HDCRectSource> ViewSources(IEnumerable<HDCAdPlan> plans) =>
            plans.Select(plan => new HDCRectSource(plan.Network.CreateView(plan), plan.Network)).ToList();

        private List<HDCAdPlan> Plans(HDCAdUse use, string slotName, int priority, bool useBackup, Func<string, object> unitFor,
            bool reloadAfterShow, HDCBannerSlot bannerSlot = HDCBannerSlot.FullBottom)
        {
            var plans = new List<HDCAdPlan>();
            foreach (string key in context.Order.Order(priority, useBackup))
            {
                HDCAdPlan plan = Plan(context.Network(key), use, slotName, unitFor, reloadAfterShow, bannerSlot);
                if (plan != null)
                    plans.Add(plan);
            }

            return plans;
        }

        private HDCAdPlan FirstPlan(HDCAdUse use, string slotName, Func<string, object> unitFor, bool reloadAfterShow) =>
            context.Networks.Select(network => Plan(network, use, slotName, unitFor, reloadAfterShow, HDCBannerSlot.FullBottom))
                .FirstOrDefault(plan => plan != null);

        private HDCAdPlan Plan(IAdNetwork network, HDCAdUse use, string slotName, Func<string, object> unitFor, bool reloadAfterShow,
            HDCBannerSlot bannerSlot)
        {
            object unit = network == null ? null : unitFor(network.UnitKey);
            return unit == null ? null : network.Plan(use, new HDCAdUnitSpec(slotName, unit, reloadAfterShow, CoreConfig, bannerSlot));
        }
    }
}
