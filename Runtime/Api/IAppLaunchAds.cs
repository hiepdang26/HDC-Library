using System;

namespace HDC.Ads
{
    public interface IAppLaunchAds
    {
        event Action BeforeShow;

        event Action Completed;

        bool IgnoreAds { get; set; }

        bool IsCompleted { get; }

        bool IsBeforeShowRaised { get; }

        bool CanShow { get; }

        void Initialize();
    }
}
