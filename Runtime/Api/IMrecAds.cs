using UnityEngine;

namespace HDC.Ads
{
    /// <summary>The MREC (300x250) view, from the Google Mobile Ads plugin.</summary>
    public interface IMrecAds
    {
        bool CanShow { get; }

        /// <summary>The view's size in screen pixels, zero until it exists.</summary>
        Vector2 SizeInPixels { get; }

        /// <summary>Shows the MREC now, or as soon as it loads. False when the channel is off.</summary>
        bool Show();

        void Hide();

        void Move(HDCAdPosition position);

        /// <summary>Centers the MREC on a point in Unity screen pixels.</summary>
        void Move(Vector2 screenPoint);

        /// <summary>Centers the MREC on a scene or UI object.</summary>
        void Move(GameObject target, Camera camera = null);

        /// <summary>Starts loading when the channel does not load on its own (autoInit off).</summary>
        void Initialize();
    }
}
