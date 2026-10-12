using System;
using HDC.Ads.Domain;
using HDC.Ads.Ports;

namespace HDC.Ads.Tests
{
    internal sealed class HDCFakeSdk : IAdsSdk
    {
        private Action onReady;

        public event Action<HDCAdEvent> AdEvent;

        public void Initialize(Action onReady) => this.onReady += onReady;

        internal void BecomeReady()
        {
            Action ready = onReady;
            onReady = null;
            ready?.Invoke();
        }

        internal void Emit(HDCAdEvent adEvent) => AdEvent?.Invoke(adEvent);
    }
}
