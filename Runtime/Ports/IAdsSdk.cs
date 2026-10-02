using System;
using HDC.Ads.Domain;

namespace HDC.Ads.Ports
{
    /// <summary>The ad SDKs under every network: started once, then a stream of every ad's events.</summary>
    internal interface IAdsSdk
    {
        /// <summary>Starts the SDKs; <paramref name="onReady"/> runs once they are ready.</summary>
        void Initialize(Action onReady);

        /// <summary>Every event of every ad: loaded, failed, shown, impression, clicked, paid, closed.</summary>
        event Action<HDCAdEvent> AdEvent;
    }
}
