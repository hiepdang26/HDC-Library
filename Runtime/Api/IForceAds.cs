using System;

namespace HDC.Ads
{
    public interface IForceAds
    {
        bool IgnoreAds { get; set; }

        bool IsBreakAdRunning { get; }

        int TotalImpressionCount { get; }

        event Action<string, int> BreakAdNotice;

        event Action<string> BreakAdShown;

        event Action<string> BreakAdClosed;

        event Action<string, string> BreakAdShowFailed;

        bool Show(string position, Action onDone = null);

        bool CanShow(string position);

        bool IsGroupReady(string groupName);

        void Initialize(string groupName);

        bool Reinitialize(string groupName);

        int ImpressionCount(string position);

        void StartBreakAd();

        void StopBreakAd();

        void ResetBreakAd();
    }
}
