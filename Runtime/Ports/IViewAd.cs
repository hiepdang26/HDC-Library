using System;
using HDC.Ads.Domain;
using UnityEngine;

namespace HDC.Ads.Ports
{
    /// <summary>One banner or MREC view, which stays on screen until hidden.</summary>
    internal interface IViewAd
    {
        /// <summary>The instance id its events carry.</summary>
        string Id { get; }

        /// <summary>One of the <see cref="HDCAdFormat"/> values.</summary>
        string Format { get; }

        string AdUnitId { get; }

        /// <summary>The view's size in screen pixels, zero until it exists.</summary>
        Vector2 SizeInPixels { get; }

        /// <summary>This view's events: loaded, failed, shown, clicked, paid and the others.</summary>
        event Action<HDCAdEvent> Event;

        void Load();

        /// <summary>Shows the view now, or as soon as it loads.</summary>
        void Show();

        void Hide();

        /// <summary>Expands the view; false for views that cannot.</summary>
        bool Expand(bool enableClick);

        /// <summary>Moves the view to a preset position; views that cannot move stay.</summary>
        void Move(HDCAdPosition position);

        /// <summary>Centers the view on a point in Unity screen pixels; views that cannot move stay.</summary>
        void Move(Vector2 screenPoint);
    }
}
