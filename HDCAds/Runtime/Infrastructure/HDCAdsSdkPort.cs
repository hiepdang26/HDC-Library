using System;
using HDC.Ads.Domain;
using HDC.Ads.Ports;

namespace HDC.Ads.Infrastructure
{
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
