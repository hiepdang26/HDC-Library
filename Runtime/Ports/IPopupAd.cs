using UnityEngine;

namespace HDC.Ads.Ports
{
    /// <summary>One native popup, which shows over a part of the screen the game gives it.</summary>
    internal interface IPopupAd
    {
        /// <summary>The instance id its events carry.</summary>
        string Id { get; }

        string AdUnitId { get; }

        /// <summary>A load went out and the SDK took it.</summary>
        bool IsRequested { get; }

        /// <summary>The game gave the popup its place.</summary>
        bool IsPlaced { get; }

        bool IsReady { get; }

        bool IsDisplayable { get; }

        /// <summary>The native side's state, such as "Showing"; null before a load.</summary>
        string State { get; }

        /// <summary>Starts loading, once.</summary>
        void Load();

        /// <summary>Drops the popup and loads a new one. False while it shows.</summary>
        bool Reload();

        /// <summary>Places the popup over a rectangle in Unity screen pixels.</summary>
        void Place(Rect screenRect);

        bool Show();

        void Hide();
    }
}
