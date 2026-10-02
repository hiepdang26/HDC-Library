using UnityEngine;

namespace HDC.Ads
{
    public interface IMrecAds
    {
        bool CanShow { get; }

        Vector2 SizeInPixels { get; }

        bool Show();

        void Hide();

        void Move(HDCAdPosition position);

        void Move(Vector2 screenPoint);

        void Move(GameObject target, Camera camera = null);

        void Initialize();
    }
}
