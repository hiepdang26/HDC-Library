using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HDC.Ads.DebugUI
{
    /// <summary>
    /// The ads debug overlay: a panel over the game with the ad systems workspace. Open it with the small Ads
    /// button, F10, or three taps in the top-left corner; close it with Close. A debug tool: leave it out of
    /// release builds.
    /// </summary>
    public sealed class HDCAdsDebugPanel : MonoBehaviour
    {
        // Share of the screen, from the top-left corner, that counts taps.
        private const float CornerSize = 0.15f;
        private const int OpeningTaps = 3;
        private const float TapWindowSeconds = 1f;

        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button openButton;
        [Tooltip("Open the panel when the scene starts.")]
        [SerializeField] private bool startOpen = true;
        [Tooltip("Keep the panel when another scene loads.")]
        [SerializeField] private bool keepAcrossScenes;

        private int taps;
        private float firstTapTime;

        public bool IsOpen => panelRoot.activeSelf;

        private void Awake()
        {
            FillMissingFonts();
            EnsureEventSystem();
            closeButton.onClick.AddListener(Close);
            openButton.onClick.AddListener(Open);
            if (keepAcrossScenes)
                DontDestroyOnLoad(gameObject);
            SetOpen(startOpen);
        }

#if ENABLE_LEGACY_INPUT_MANAGER
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F10))
                Toggle();
            if (Input.GetMouseButtonDown(0) && InCorner(Input.mousePosition))
                CountTap();
        }
#endif

        public void Open() => SetOpen(true);

        public void Close() => SetOpen(false);

        public void Toggle() => SetOpen(!IsOpen);

        private void SetOpen(bool open)
        {
            panelRoot.SetActive(open);
            openButton.gameObject.SetActive(!open);
        }

        private void CountTap()
        {
            if (taps == 0 || Time.unscaledTime - firstTapTime > TapWindowSeconds)
            {
                taps = 0;
                firstTapTime = Time.unscaledTime;
            }

            if (++taps < OpeningTaps)
                return;
            taps = 0;
            Toggle();
        }

        private static bool InCorner(Vector3 point) =>
            point.x <= Screen.width * CornerSize && point.y >= Screen.height * (1f - CornerSize);

        // The prefab's texts use the editor's built-in font, which another Unity version may name differently.
        private void FillMissingFonts()
        {
            Font font = null;
            foreach (Text text in GetComponentsInChildren<Text>(true))
            {
                if (text.font != null)
                    continue;
                if (font == null)
                    font = BuiltinFont();
                text.font = font;
            }
        }

        private static Font BuiltinFont()
        {
#if UNITY_2022_2_OR_NEWER
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#else
            return Resources.GetBuiltinResource<Font>("Arial.ttf");
#endif
        }

        private static void EnsureEventSystem()
        {
#if UNITY_2023_1_OR_NEWER
            bool found = FindFirstObjectByType<EventSystem>() != null;
#else
            bool found = FindObjectOfType<EventSystem>() != null;
#endif
            if (found)
                return;
#if ENABLE_LEGACY_INPUT_MANAGER
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
#else
            Debug.LogWarning("[HDCAds] The debug panel needs an EventSystem with an input module in the scene.");
#endif
        }
    }
}
