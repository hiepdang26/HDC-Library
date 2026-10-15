using System;
using HDC.Ads.Ports;

namespace HDC.Ads.Tests
{
    internal sealed class HDCFakeMainThread : IMainThread
    {
        public event Action Ticked;

        public event Action LateTicked;

        public event Action<bool> ApplicationPaused;

        internal void Tick() => Ticked?.Invoke();

        internal void LateTick() => LateTicked?.Invoke();

        internal void Pause(bool paused) => ApplicationPaused?.Invoke(paused);
    }
}
