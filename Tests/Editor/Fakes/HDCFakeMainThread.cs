using System;
using HDC.Ads.Ports;

namespace HDC.Ads.Tests
{
    /// <summary>A main thread whose frames and app pauses the test runs by hand.</summary>
    internal sealed class HDCFakeMainThread : IMainThread
    {
        public event Action Ticked;

        public event Action<bool> ApplicationPaused;

        internal void Tick() => Ticked?.Invoke();

        internal void Pause(bool paused) => ApplicationPaused?.Invoke(paused);
    }
}
