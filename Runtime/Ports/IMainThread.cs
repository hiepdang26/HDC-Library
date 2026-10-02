using System;

namespace HDC.Ads.Ports
{
    /// <summary>The Unity main thread's loop, for the channels' timers and the app's trips to the background.</summary>
    internal interface IMainThread
    {
        /// <summary>Every frame.</summary>
        event Action Ticked;

        /// <summary>True when the app goes to the background, false when it comes back.</summary>
        event Action<bool> ApplicationPaused;
    }
}
