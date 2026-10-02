using System;
using HDC.Ads.Domain;

namespace HDC.Ads.Logic
{
    /// <summary>The rewarded channel behind <see cref="IRewardedAds"/>.</summary>
    internal sealed partial class HDCRewardedAds : IRewardedAds, IAdChannel
    {
        private readonly HDCAdsContext context;

        internal HDCRewardedAds(HDCAdsContext context)
        {
            this.context = context;
        }

        /// <summary>While true, shows skip the ad and grant the reward at once.</summary>
        public bool IgnoreAds { get; set; }

        public bool CanShow => IsEnabled && context.Groups.RewardedGroup().IsReady;

        public int ImpressionCount => context.Count(HDCAdNames.RewardedCountKey);

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
                () => context.Store.SetInt(HDCAdNames.RewardedCountKey, ImpressionCount + 1),
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

        string IAdChannel.Key => "RW";

        string IAdChannel.Title => "Rewarded";

        void IAdChannel.OnSdkInitialized()
        {
            if (IsEnabled && AutoInit)
                context.Groups.RewardedGroup().Initialize();
        }

        void IAdChannel.InitializeAll() => Initialize();

        // Ad removal leaves rewarded ads on: the player chooses to watch them.
        void IAdChannel.OnAdsRemoved()
        {
        }
    }
}
