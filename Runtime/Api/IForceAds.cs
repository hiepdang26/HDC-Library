using System;

namespace HDC.Ads
{
    /// <summary>
    /// Force ads: full-screen ads at game positions. Each position maps to a force ad group of the ad core
    /// config and has its own capping: the time since the last full-screen ad must reach its capping time,
    /// minus a decrease per impression already shown there, never below the minimum. The first force ad of a
    /// session also waits for the launch capping. The break ad shows the ad of one position on a timer.
    /// </summary>
    public interface IForceAds
    {
        /// <summary>Blocks every force ad while true.</summary>
        bool IgnoreAds { get; set; }

        bool IsBreakAdRunning { get; }

        /// <summary>The force ads shown, across sessions.</summary>
        int TotalImpressionCount { get; }

        /// <summary>The break ad shows in <c>seconds</c>: (position, seconds).</summary>
        event Action<string, int> BreakAdNotice;

        /// <summary>The break ad is about to show at the position.</summary>
        event Action<string> BreakAdShown;

        /// <summary>The break ad at the position closed.</summary>
        event Action<string> BreakAdClosed;

        /// <summary>The break ad could not show: (position, reason). Its timer starts over.</summary>
        event Action<string, string> BreakAdShowFailed;

        /// <summary>
        /// Shows the force ad of <paramref name="position"/> if the position may show one now.
        /// <paramref name="onDone"/> runs once the ad closes, or right away when none shows.
        /// </summary>
        bool Show(string position, Action onDone = null);

        /// <summary>True when the position may show an ad now and its group has one ready.</summary>
        bool CanShow(string position);

        bool IsGroupReady(string groupName);

        /// <summary>Starts loading a group whose positions do not load it on their own (autoInit off).</summary>
        void Initialize(string groupName);

        /// <summary>
        /// Drops a group's ads and show count and loads it again, for groups that load once
        /// (disablePostInitReload) or ran out of shows. False while the group shows an ad.
        /// </summary>
        bool Reinitialize(string groupName);

        /// <summary>The impressions shown at <paramref name="position"/>, across sessions.</summary>
        int ImpressionCount(string position);

        /// <summary>Starts the break ad timer, if the channel's break ad is enabled.</summary>
        void StartBreakAd();

        void StopBreakAd();

        /// <summary>Starts the running break ad's timer over.</summary>
        void ResetBreakAd();
    }
}
