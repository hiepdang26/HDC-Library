using System;
using HDC.Ads.Domain;
using UnityEngine;

namespace HDC.Ads.Ports
{
    internal interface IViewAd
    {
        string Id { get; }

        string Format { get; }

        string AdUnitId { get; }

        Vector2 SizeInPixels { get; }

        int RefreshSeconds { get; }

        event Action<HDCAdEvent> Event;

        void Load();

        void Show();

        void Hide();

        bool Expand(bool enableClick);

        void Move(HDCAdPosition position);

        void Move(Vector2 screenPoint);
    }
}
