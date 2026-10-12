using System;

namespace HDC.Ads
{
    public interface IForceAds
    {
        bool IgnoreAds { get; set; }

        int TotalImpressionCount { get; }

        bool Show(string position, Action onDone = null);

        bool CanShow(string position);

        bool IsGroupReady(string groupName);

        void Initialize(string groupName);

        bool Reinitialize(string groupName);

        int ImpressionCount(string position);
    }
}
