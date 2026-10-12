using UnityEngine;

namespace HDC.Ads.Ports
{
    internal interface IPopupAd
    {
        string Id { get; }

        string AdUnitId { get; }

        bool IsRequested { get; }

        bool IsPlaced { get; }

        bool IsReady { get; }

        bool IsDisplayable { get; }

        string State { get; }

        void Load();

        bool Reload();

        void Place(Rect screenRect);

        bool Show();

        void Hide();
    }
}
