using System;
using HDC.Ads.Domain;
using HDC.Ads.Ports;

namespace HDC.Ads.Tests
{
    /// <summary>An SDK that becomes ready and sends ad events when the test says so.</summary>
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

        /// <summary>Sends an event to everyone listening, as the real SDK does: the context and the ad it names.</summary>
        internal void Emit(HDCAdEvent adEvent) => AdEvent?.Invoke(adEvent);
    }
}
