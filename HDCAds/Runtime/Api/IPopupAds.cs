using UnityEngine;

namespace HDC.Ads
{
    public interface IPopupAds
    {
        bool Show(string position);

        void Hide(string position);

        void Move(string position, Rect screenRect);

        void Move(string position, RectTransform area, Camera camera = null);

        bool CanShow(string position);

        bool IsGroupReady(string groupName);

        bool Reinitialize(string groupName);

        void Initialize(string groupName);
    }
}
