using System;

namespace HDC.Ads
{
    public interface IBreakAds
    {
        bool IsRunning { get; }

        HDCBreakAdState State { get; }

        float CountdownSeconds { get; }

        float CountdownLeft { get; }

        float LoadingTimeout { get; set; }

        event Action<HDCBreakAdState> StateChanged;

        void Start();

        void Stop();

        void Restart();
    }
}
