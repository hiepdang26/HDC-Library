using UnityEngine;

namespace HDC.Onboarding
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    [AddComponentMenu("HDC/Onboarding/HDC Splash Ad Area")]
    public sealed class HDCSplashAdArea : MonoBehaviour
    {
        [Tooltip("The splash ad that shows in this area. HDCSplash finds the area by this kind when its own field is empty.")]
        [SerializeField] private HDCSplashAdAreaKind kind;

        [Tooltip("Picks the anchors below from the screen: portrait when it is taller than wide, landscape otherwise. The offsets stay 0, so the area fills its anchors at full width.")]
        [SerializeField] private bool followOrientation = true;

        [SerializeField] private Vector2 portraitAnchorMin = Vector2.zero;
        [SerializeField] private Vector2 portraitAnchorMax = new Vector2(1f, 0.075f);
        [SerializeField] private Vector2 landscapeAnchorMin = Vector2.zero;
        [SerializeField] private Vector2 landscapeAnchorMax = new Vector2(1f, 0.175f);

        private DrivenRectTransformTracker tracker;
        private int applied = -1;

        public HDCSplashAdAreaKind Kind => kind;

        public RectTransform Area => (RectTransform)transform;

        public bool FollowsOrientation => followOrientation;

        public bool IsPortrait => applied == 1;

        private void OnEnable()
        {
            applied = -1;
            Refresh();
        }

        private void OnDisable() => tracker.Clear();

        private void Update() => Refresh();

        private void OnRectTransformDimensionsChange() => Refresh();

        private void OnValidate() => applied = -1;

        internal static bool IsPortraitSize(Vector2 size) => size.y > size.x;

        internal void Refresh()
        {
            if (!followOrientation)
            {
                tracker.Clear();
                applied = -1;
                return;
            }

            int portrait = IsPortraitSize(ScreenSize()) ? 1 : 0;
            if (portrait == applied)
                return;
            applied = portrait;
            Apply(portrait == 1);
        }

        internal void Apply(bool portrait)
        {
            RectTransform area = Area;
            tracker.Clear();
            tracker.Add(this, area, DrivenTransformProperties.Anchors | DrivenTransformProperties.AnchoredPosition | DrivenTransformProperties.SizeDelta);
            Vector2 min = portrait ? portraitAnchorMin : landscapeAnchorMin;
            Vector2 max = portrait ? portraitAnchorMax : landscapeAnchorMax;
            if (area.anchorMin != min)
                area.anchorMin = min;
            if (area.anchorMax != max)
                area.anchorMax = max;
            if (area.offsetMin != Vector2.zero)
                area.offsetMin = Vector2.zero;
            if (area.offsetMax != Vector2.zero)
                area.offsetMax = Vector2.zero;
        }

        private Vector2 ScreenSize()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                Rect rect = ((RectTransform)canvas.rootCanvas.transform).rect;
                if (rect.width > 0f && rect.height > 0f)
                    return rect.size;
            }

            return new Vector2(Screen.width, Screen.height);
        }
    }
}
