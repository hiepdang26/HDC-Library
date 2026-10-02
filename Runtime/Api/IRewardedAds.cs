using System;

namespace HDC.Ads
{
    /// <summary>Rewarded ads. Ad removal does not block them.</summary>
    public interface IRewardedAds
    {
        /// <summary>While true, shows skip the ad and grant the reward at once.</summary>
        bool IgnoreAds { get; set; }

        bool CanShow { get; }

        /// <summary>The rewarded ads shown, across sessions.</summary>
        int ImpressionCount { get; }

        /// <summary>
        /// Shows a rewarded ad. Once it closes, <paramref name="onRewarded"/> runs if the player earned the
        /// reward, then <paramref name="onClosed"/>. False, with neither callback, when no ad shows.
        /// </summary>
        bool Show(string position, Action onRewarded, Action onClosed = null);

        /// <summary>Starts loading when the channel does not load on its own (autoInit off).</summary>
        void Initialize();
    }
}
