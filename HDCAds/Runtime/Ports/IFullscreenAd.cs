using System;
using HDC.Ads.Domain;

namespace HDC.Ads.Ports
{
    internal interface IFullscreenAd
    {
        string Id { get; }

        string Format { get; }

        string AdUnitId { get; }

        bool IsReady { get; }

        event Action<HDCAdEvent> Event;

        void Load();

        bool Show();

        void Destroy();
    }
}
