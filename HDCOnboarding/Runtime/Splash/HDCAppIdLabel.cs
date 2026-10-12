using HDC.Ads.Infrastructure;
using UnityEngine;

namespace HDC.Onboarding
{
    [AddComponentMenu("HDC/Onboarding/HDC App Id Label")]
    public sealed class HDCAppIdLabel : MonoBehaviour
    {
        [Tooltip("The label is drawn over this rect. Without it the label sits in the middle of the screen.")]
        [SerializeField] private RectTransform target;

        [SerializeField] private int fontSize = 26;
        [SerializeField] private Color color = Color.white;
        [SerializeField] private TextAnchor alignment = TextAnchor.MiddleLeft;
        [Tooltip("Left, right, top and bottom insets of the label box, in pixels. A negative inset grows the box.")]
        [SerializeField] private Vector4 margin = new Vector4(0, 0, 0, -57);

        private string text = "";
        private GUIStyle style;

        public void Show(string label, RectTransform area = null)
        {
            text = label ?? "";
            if (area != null)
                target = area;
        }

        private void Start() => Show($"v{Application.version}-{new HDCDeviceRegionReader().DeviceId}");

        private void OnGUI()
        {
            if (string.IsNullOrEmpty(text))
                return;
            if (style == null)
                style = new GUIStyle(GUI.skin.label) { wordWrap = true };
            style.fontSize = fontSize;
            style.normal.textColor = color;
            style.alignment = alignment;
            GUI.Label(Inset(TargetRect(), margin), text, style);
        }

        private static Rect Inset(Rect rect, Vector4 inset) =>
            new Rect(rect.x + inset.x, rect.y + inset.z, Mathf.Max(0, rect.width - inset.x - inset.y), Mathf.Max(0, rect.height - inset.z - inset.w));

        private Rect TargetRect()
        {
            if (target == null)
                return new Rect(0, Screen.height * 0.5f - 40, Screen.width, 80);

            var corners = new Vector3[4];
            target.GetWorldCorners(corners);
            Canvas canvas = target.GetComponentInParent<Canvas>();
            Camera camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            Vector2 min = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
            Vector2 max = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
            return new Rect(Mathf.Min(min.x, max.x), Screen.height - Mathf.Max(min.y, max.y), Mathf.Abs(max.x - min.x), Mathf.Abs(max.y - min.y));
        }
    }
}
