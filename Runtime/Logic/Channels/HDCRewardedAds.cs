using System;
using HDC.Ads.Diagnostics;

namespace HDC.Ads.Logic
{
    /// <summary>The rewarded channel behind <see cref="IRewardedAds"/>.</summary>
    internal sealed class HDCRewardedAds : IRewardedAds
    {
        private const string ImpressionsKey = "rw_count";

        private readonly HDCAdsContext context;
        internal HDCRewardedAds(HDCAdsContext context)
        {
            this.context = context;
        }

        /// <summary>While true, shows skip the ad and grant the reward at once.</summary>
        public bool IgnoreAds { get; set; }

        public bool CanShow => IsEnabled && context.Groups.RewardedGroup().IsReady;

        public int ImpressionCount => context.Count(ImpressionsKey);

        private bool IsEnabled => context.Config.rewardedChannel?.isEnabled ?? false;

        private bool AutoInit => context.Config.rewardedChannel?.autoInit ?? false;

        /// <summary>
        /// Shows a rewarded ad. Once it closes, <paramref name="onRewarded"/> runs if the player earned the
        /// reward, then <paramref name="onClosed"/>. False, with neither callback, when no ad shows.
        /// </summary>
        public bool Show(string position, Action onRewarded, Action onClosed = null)
        {
            if (!IsEnabled)
            {
                context.Log.Info($"rewarded {position} blocked: channel disabled");
                return false;
            }

            if (IgnoreAds)
            {
                HDCCallbacks.Run(onRewarded);
                HDCCallbacks.Run(onClosed);
                return true;
            }

            return context.Groups.RewardedGroup().Show(
                HDCAdChannel.Rewarded,
                position,
                null,
                () => context.Store.SetInt(ImpressionsKey, ImpressionCount + 1),
                rewarded =>
                {
                    if (rewarded)
                        HDCCallbacks.Run(onRewarded);
                    HDCCallbacks.Run(onClosed);
                });
        }

        /// <summary>Starts loading when the channel does not load on its own (autoInit off).</summary>
        public void Initialize()
        {
            if (IsEnabled && !AutoInit)
                context.Groups.RewardedGroup().Initialize();
        }

        /// <summary>Configs and state, for the debug panel. It reads the group without making it.</summary>
        internal HDCDebugInfo Describe()
        {
            HDCFullscreenGroup group = context.Groups.ExistingRewardedGroup;
            HDCDebugInfo info = new HDCDebugInfo()
                .Section("Configs")
                .Needed("Enabled", IsEnabled)
                .Line("Auto Init", AutoInit)
                .Section("Runtime")
                .Line("Ignore Ads", IgnoreAds)
                .Line("Can Show", IsEnabled && group != null && group.IsReady)
                .Line("Impressions", ImpressionCount)
                .Section("Gates")
                .Gate("Disabled", !IsEnabled)
                .Section("Group");
            if (group != null)
                group.DescribeTo(info);
            else
                info.Add("State", "Not started", HDCDebugTone.Muted);
            return info;
        }

        internal void OnSdkInitialized()
        {
            if (IsEnabled && AutoInit)
                context.Groups.RewardedGroup().Initialize();
        }
    }
}
