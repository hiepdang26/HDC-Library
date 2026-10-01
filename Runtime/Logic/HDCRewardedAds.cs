using System;

namespace HDC.Ads
{
    /// <summary>Rewarded ads. Ad removal does not block them.</summary>
    public sealed class HDCRewardedAds
    {
        private const string ImpressionsKey = "rw_count";

        internal HDCRewardedAds()
        {
        }

        /// <summary>While true, shows skip the ad and grant the reward at once.</summary>
        public bool IgnoreAds { get; set; }

        public bool CanShow => IsEnabled && HDCAds.RewardedGroup().IsReady;

        public int ImpressionCount => HDCAdsLog.GetInt(ImpressionsKey);

        private static bool IsEnabled => HDCAds.Config.rewardedChannel?.isEnabled ?? false;

        private static bool AutoInit => HDCAds.Config.rewardedChannel?.autoInit ?? false;

        /// <summary>
        /// Shows a rewarded ad. Once it closes, <paramref name="onRewarded"/> runs if the player earned the
        /// reward, then <paramref name="onClosed"/>. False, with neither callback, when no ad shows.
        /// </summary>
        public bool Show(string position, Action onRewarded, Action onClosed = null)
        {
            if (!IsEnabled)
            {
                HDCAdsLog.Info($"rewarded {position} blocked: channel disabled");
                return false;
            }

            if (IgnoreAds)
            {
                HDCAdsLog.Run(onRewarded);
                HDCAdsLog.Run(onClosed);
                return true;
            }

            return HDCAds.RewardedGroup().Show(
                null,
                () => HDCAdsLog.SetInt(ImpressionsKey, ImpressionCount + 1),
                rewarded =>
                {
                    if (rewarded)
                        HDCAdsLog.Run(onRewarded);
                    HDCAdsLog.Run(onClosed);
                });
        }

        /// <summary>Starts loading when the channel does not load on its own (autoInit off).</summary>
        public void Initialize()
        {
            if (IsEnabled && !AutoInit)
                HDCAds.RewardedGroup().Initialize();
        }

        /// <summary>Configs and state, for the debug panel. It reads the group without making it.</summary>
        internal HDCDebugInfo Describe()
        {
            HDCFullscreenGroup group = HDCAds.ExistingRewardedGroup;
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
                HDCAds.RewardedGroup().Initialize();
        }
    }
}
