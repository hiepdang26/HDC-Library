using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if HDC_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

namespace HDC.Ads.DebugUI
{
    /// <summary>
    /// The ads debug overlay: the Ads, Remote Config, Events and Device pages under a header with the ads' state.
    /// It stays hidden until opened with three quick taps in a top corner of the screen, or with F10, and closes
    /// with its close button. It keeps to the screen's safe area and switches its scale to landscape screens.
    /// A debug tool: leave it out of release builds.
    /// </summary>
    public sealed class HDCAdsDebugPanel : MonoBehaviour
    {
        private static readonly Vector2 PortraitResolution = new Vector2(1080f, 1920f);
        private static readonly Vector2 LandscapeResolution = new Vector2(1920f, 1080f);

        private enum Corner
        {
            TopLeft,
            TopRight,
        }

        [Serializable]
        private sealed class PageTab
        {
            public Button button;
            public HDCDebugPage page;
        }

        [SerializeField] private GameObject window;
        [SerializeField] private RectTransform safeArea;
        [SerializeField] private CanvasScaler scaler;
        [SerializeField] private Button closeButton;
        [SerializeField] private HDCOptionPicker picker;
        [SerializeField] private HDCAdsDebugViewer viewer;

        [Header("Header")]
        [SerializeField] private Text statusText;
        [SerializeField] private Button initSdkButton;
        [SerializeField] private Button refreshButton;

        [Header("Pages")]
        [SerializeField] private PageTab[] pages = new PageTab[0];

        [Header("Opening")]
        [Tooltip("Open the panel when the scene starts.")]
        [SerializeField] private bool startOpen;
        [SerializeField] private Corner activationCorner = Corner.TopLeft;
        [Tooltip("Share of the screen's width and height, from the corner, that counts taps.")]
        [SerializeField, Range(0.05f, 0.3f)] private float activationSize = 0.14f;
        [SerializeField, Min(2)] private int requiredTaps = 3;
        [Tooltip("Seconds the taps must fall within.")]
        [SerializeField, Range(0.25f, 1.5f)] private float tapWindowSeconds = 0.9f;
        [Tooltip("Seconds after opening or closing during which taps are ignored.")]
        [SerializeField, Range(0f, 1f)] private float cooldownSeconds = 0.25f;
        [SerializeField] private bool f10Toggles = true;
        [Tooltip("Keep the panel when another scene loads.")]
        [SerializeField] private bool keepAcrossScenes;

        private static HDCAdsDebugPanel kept;
        private int taps;
        private float lastTapTime = float.MinValue;
        private float lastToggleTime = float.MinValue;
        private Rect appliedSafeArea;
        private Vector2Int appliedScreen;
        private int currentPage;
        private float nextStatus;

        public bool IsOpen => window.activeSelf;

        /// <summary>The page shown: 0 Ads, 1 Remote Config, 2 Events, 3 Device.</summary>
        public int CurrentPage => currentPage;

        private void Awake()
        {
            if (keepAcrossScenes)
            {
                if (kept != null && kept != this)
                {
                    Destroy(gameObject);
                    return;
                }

                kept = this;
                DontDestroyOnLoad(gameObject);
            }

            FillMissingFonts();
            EnsureEventSystem();
            closeButton.onClick.AddListener(Close);
            initSdkButton.onClick.AddListener(InitializeSdk);
            refreshButton.onClick.AddListener(() => pages[currentPage].page.Refresh());
            for (int i = 0; i < pages.Length; i++)
            {
                int index = i;
                pages[i].button.onClick.AddListener(() => ShowPage(index));
            }

            window.SetActive(false);
            ShowPage(0);
            FitScreen();
        }

        private void Start()
        {
            if (startOpen)
                Open();
        }

        private void OnDestroy()
        {
            if (kept == this)
                kept = null;
        }

        private void Update()
        {
            FitScreen();
            if (f10Toggles && F10Pressed())
            {
                Toggle();
                return;
            }

            if (IsOpen && Time.unscaledTime >= nextStatus)
                RefreshStatus();
            if (!IsOpen && TapBegan(out Vector2 point) && InCorner(point))
                CountTap();
        }

        public void Open()
        {
            taps = 0;
            lastToggleTime = Time.unscaledTime;
            window.SetActive(true);
            RefreshStatus();
        }

        /// <summary>Shows one page: 0 Ads, 1 Remote Config, 2 Events, 3 Device.</summary>
        public void ShowPage(int index)
        {
            currentPage = Mathf.Clamp(index, 0, pages.Length - 1);
            for (int i = 0; i < pages.Length; i++)
            {
                bool shown = i == currentPage;
                HDCDebugStyle.Highlight(pages[i].button, shown);
                if (pages[i].page.gameObject.activeSelf != shown)
                    pages[i].page.gameObject.SetActive(shown);
            }
        }

        private void RefreshStatus()
        {
            nextStatus = Time.unscaledTime + 1f;
            initSdkButton.gameObject.SetActive(!HDCAds.IsInitialized);
            if (!HDCAds.IsInitialized)
            {
                statusText.text = HDCDebugStyle.Colored(HDCDebugStyle.WarnHex, "HDCAds chưa khởi tạo")
                    + " · mở game từ scene có HDCAdsSetup, hoặc bấm Init SDK.";
                return;
            }

            string core = string.IsNullOrEmpty(HDCAds.Config.selectedAdCoreName) ? "-" : HDCAds.Config.selectedAdCoreName;
            statusText.text = HDCDebugStyle.Colored(HDCDebugStyle.GoodHex, "SDK ready") + $" · {Platform} · Ad core: {core}"
                + (HDCAds.IsAdsRemoved ? " · " + HDCDebugStyle.Colored(HDCDebugStyle.WarnHex, "Ads removed") : string.Empty);
        }

        private void InitializeSdk()
        {
            if (!HDCAds.IsInitialized)
                HDCAdsSetup.InitializeAds();
            RefreshStatus();
        }

        private static string Platform
        {
            get
            {
#if UNITY_EDITOR
                return "Editor simulation";
#elif UNITY_IOS
                return "iOS";
#elif UNITY_ANDROID
                return "Android";
#else
                return Application.platform.ToString();
#endif
            }
        }

        public void Close()
        {
            taps = 0;
            lastToggleTime = Time.unscaledTime;
            picker.Close();
            viewer.Close();
            window.SetActive(false);
        }

        public void Toggle()
        {
            if (IsOpen)
                Close();
            else
                Open();
        }

        private void CountTap()
        {
            float now = Time.unscaledTime;
            if (now - lastToggleTime < cooldownSeconds)
                return;
            if (now - lastTapTime > tapWindowSeconds)
                taps = 0;
            taps++;
            lastTapTime = now;
            if (taps >= requiredTaps)
                Open();
        }

        private bool InCorner(Vector2 point)
        {
            if (point.y < Screen.height * (1f - activationSize))
                return false;
            return activationCorner == Corner.TopLeft
                ? point.x <= Screen.width * activationSize
                : point.x >= Screen.width * (1f - activationSize);
        }

        // The safe area keeps the panel clear of notches; landscape screens get a landscape reference size.
        private void FitScreen()
        {
            var size = new Vector2Int(Screen.width, Screen.height);
            Rect area = Screen.safeArea;
            if ((size == appliedScreen && area == appliedSafeArea) || size.x <= 0 || size.y <= 0)
                return;
            appliedScreen = size;
            appliedSafeArea = area;

            safeArea.anchorMin = new Vector2(area.xMin / size.x, area.yMin / size.y);
            safeArea.anchorMax = new Vector2(area.xMax / size.x, area.yMax / size.y);
            safeArea.offsetMin = Vector2.zero;
            safeArea.offsetMax = Vector2.zero;

            bool landscape = size.x > size.y;
            scaler.referenceResolution = landscape ? LandscapeResolution : PortraitResolution;
            scaler.matchWidthOrHeight = landscape ? 1f : 0f;
        }

#if HDC_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
        private static bool F10Pressed() => Keyboard.current != null && Keyboard.current.f10Key.wasPressedThisFrame;

        private static bool TapBegan(out Vector2 point)
        {
            Touchscreen touchscreen = Touchscreen.current;
            if (touchscreen != null && touchscreen.primaryTouch.press.wasPressedThisFrame)
            {
                point = touchscreen.primaryTouch.position.ReadValue();
                return true;
            }

            Mouse mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                point = mouse.position.ReadValue();
                return true;
            }

            point = default;
            return false;
        }
#elif ENABLE_LEGACY_INPUT_MANAGER
        private static bool F10Pressed() => Input.GetKeyDown(KeyCode.F10);

        private static bool TapBegan(out Vector2 point)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                if (touch.phase == TouchPhase.Began)
                {
                    point = touch.position;
                    return true;
                }
            }

            if (Input.touchCount == 0 && Input.GetMouseButtonDown(0))
            {
                point = Input.mousePosition;
                return true;
            }

            point = default;
            return false;
        }
#else
        private static bool F10Pressed() => false;

        private static bool TapBegan(out Vector2 point)
        {
            point = default;
            return false;
        }
#endif

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
#if HDC_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
#elif ENABLE_LEGACY_INPUT_MANAGER
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
#else
            Debug.LogWarning("[HDCAds] The debug panel needs an EventSystem with an input module in the scene.");
#endif
        }
    }
}
