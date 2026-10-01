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

        /// <summary>Configs, state and ad units, for the debug panel.</summary>
        internal string Describe() =>
            HDCAdsDebugText.Title("Rewarded (RW)")
                .Section("Configs")
                .Line("isEnabled", IsEnabled)
                .Line("autoInit", AutoInit)
                .Section("Runtime")
                .Line("IgnoreAds", IgnoreAds)
                .Line("can show", CanShow)
                .Line("impressions", ImpressionCount)
                .Section("Group")
                .Lines(HDCAds.RewardedGroup().Describe())
                .Done();

        internal void OnSdkInitialized()
        {
            if (IsEnabled && AutoInit)
                HDCAds.RewardedGroup().Initialize();
        }
    }
}
