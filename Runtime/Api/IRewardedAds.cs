using System;

namespace HDC.Ads
{
    public interface IRewardedAds
    {
        bool IgnoreAds { get; set; }

        bool CanShow { get; }

        int ImpressionCount { get; }

        bool Show(string position, Action onRewarded, Action onClosed = null);

        void Initialize();
    }
}
