using System;
using HDC.Ads.Domain;

namespace HDC.Ads.Ports
{
    internal interface IAdsSdk
    {
        void Initialize(Action onReady);

        event Action<HDCAdEvent> AdEvent;
    }
}
