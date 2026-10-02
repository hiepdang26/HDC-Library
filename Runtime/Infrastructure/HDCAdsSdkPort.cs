using System;
using HDC.Ads.Domain;
using HDC.Ads.Ports;

namespace HDC.Ads.Infrastructure
{
    /// <summary><see cref="HDCAdsSdk"/>: the native library and the Google Mobile Ads plugin.</summary>
    internal sealed class HDCAdsSdkPort : IAdsSdk
    {
        public void Initialize(Action onReady) => HDCAdsSdk.Initialize(onReady);

        public event Action<HDCAdEvent> AdEvent
        {
            add => HDCAdsSdk.AdEvent += value;
            remove => HDCAdsSdk.AdEvent -= value;
        }
    }
}
