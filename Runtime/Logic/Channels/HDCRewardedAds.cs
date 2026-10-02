using System;
using HDC.Ads.Domain;

namespace HDC.Ads.Logic
{
    internal sealed partial class HDCRewardedAds : IRewardedAds, IAdChannel
    {
        private readonly HDCAdsContext context;

        internal HDCRewardedAds(HDCAdsContext context)
        {
            this.context = context;
        }

        public bool IgnoreAds { get; set; }

        public bool CanShow => IsEnabled && context.Groups.RewardedGroup().IsReady;

        public int ImpressionCount => context.Count(HDCAdNames.RewardedCountKey);

        private bool IsEnabled => context.Config.rewardedChannel?.isEnabled ?? false;

        private bool AutoInit => context.Config.rewardedChannel?.autoInit ?? false;

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

        void IAdChannel.OnAdsRemoved()
        {
        }
    }
}
