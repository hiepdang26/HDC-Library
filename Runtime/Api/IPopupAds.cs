using UnityEngine;

namespace HDC.Ads
{
    /// <summary>
    /// Native popups at game positions. Each position maps to a popup group of the ad core config. A popup
    /// only shows after <see cref="Move(string, Rect)"/> placed it.
    /// </summary>
    public interface IPopupAds
    {
        /// <summary>Shows the popup of <paramref name="position"/>. False when it cannot show now.</summary>
        bool Show(string position);

        void Hide(string position);

        /// <summary>Places the popup of <paramref name="position"/> over a rectangle in Unity screen pixels.</summary>
        void Move(string position, Rect screenRect);

        /// <summary>Places the popup of <paramref name="position"/> over a UI element.</summary>
        void Move(string position, RectTransform area, Camera camera = null);

        bool CanShow(string position);

        bool IsGroupReady(string groupName);

        /// <summary>Drops a group's popup and loads a new one. False while it shows.</summary>
        bool Reinitialize(string groupName);

        /// <summary>Starts loading a group whose positions do not load it on their own (autoInit off).</summary>
        void Initialize(string groupName);
    }
}
