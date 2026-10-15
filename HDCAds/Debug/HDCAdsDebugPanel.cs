using System;
using System.Collections.Generic;
using HDC.Ads.Composition;
using HDC.Ads.Diagnostics;
using HDC.Ads.Logic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if HDC_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

namespace HDC.Ads.DebugUI
{
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
        [SerializeField] private Canvas windowCanvas;
        [SerializeField] private GraphicRaycaster windowRaycaster;
        [SerializeField] private RectTransform safeArea;
        [SerializeField] private CanvasScaler scaler;
        [SerializeField] private Button closeButton;
        [SerializeField] private HDCOptionPicker picker;
        [SerializeField] private HDCAdsDebugViewer viewer;

        [Header("Header")]
        [SerializeField] private Text statusText;
        [SerializeField] private Button initSdkButton;
        [SerializeField] private Button refreshButton;
        [SerializeField] private Button expandAllButton;
        [SerializeField] private Button collapseAllButton;

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

        private static HDCAdsDebugPanel current;
        private int taps;
        private float lastTapTime = float.MinValue;
        private float lastToggleTime = float.MinValue;
        private Rect appliedSafeArea;
        private Vector2Int appliedScreen;
        private int currentPage;
        private float nextStatus;

        public bool IsOpen => windowCanvas.enabled;

        public int CurrentPage => currentPage;

        private void Awake()
        {
            if (current != null && current != this)
            {
                if (!keepAcrossScenes || current.keepAcrossScenes)
                {
                    Destroy(gameObject);
                    return;
                }

                Destroy(current.gameObject);
            }

            current = this;
            if (keepAcrossScenes)
            {
                if (transform.parent != null)
                    transform.SetParent(null, false);
                DontDestroyOnLoad(gameObject);
            }

            LoadFonts();
            EnsureEventSystem();
            closeButton.onClick.AddListener(Close);
            initSdkButton.onClick.AddListener(InitializeSdk);
            refreshButton.onClick.AddListener(() => pages[currentPage].page.Refresh());
            expandAllButton.onClick.AddListener(() => pages[currentPage].page.SetAllExpanded(true));
            collapseAllButton.onClick.AddListener(() => pages[currentPage].page.SetAllExpanded(false));
            for (int i = 0; i < pages.Length; i++)
            {
                int index = i;
                pages[i].button.onClick.AddListener(() => ShowPage(index));
            }

            if (!window.activeSelf)
                window.SetActive(true);
            SetOpen(false);
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
            if (current == this)
                current = null;
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
            EnsureEventSystem();
            taps = 0;
            lastToggleTime = Time.unscaledTime;
            SetOpen(true);
            RefreshStatus();
        }

        public void ShowPage(int index)
        {
            currentPage = Mathf.Clamp(index, 0, pages.Length - 1);
            for (int i = 0; i < pages.Length; i++)
                HDCDebugStyle.Highlight(pages[i].button, i == currentPage);
            PresentPages();

            bool folds = pages[currentPage].page.HasFoldouts;
            expandAllButton.interactable = folds;
            collapseAllButton.interactable = folds;
        }

        private void RefreshStatus()
        {
            nextStatus = Time.unscaledTime + 1f;
            bool adsBlocked = HDCAds.Consent.IsDone && !HDCAds.Consent.CanRequestAds;
            bool configLoading = HDCConfigReport.Started && !HDCConfigReport.Finished && HDCConfigReport.Plan == HDCConfigPlan.RemoteConfig;
            bool initializing = !HDCAds.IsInitialized && HDCAdsRuntime.Current.Context.InitializeCalled;
            initSdkButton.gameObject.SetActive(!HDCAds.IsInitialized && !adsBlocked && !configLoading && !initializing);
            if (adsBlocked)
            {
                statusText.text = HDCDebugStyle.Colored(HDCDebugStyle.BadHex, "Consent không cho phép quảng cáo")
                    + " · không load quảng cáo trong phiên này, xem thẻ Consent & ATT ở trang Device.";
                return;
            }

            if (configLoading)
            {
                statusText.text = HDCDebugStyle.Colored(HDCDebugStyle.WarnHex, "Đang tải Remote Config")
                    + $" · {HDCConfigReport.ElapsedSeconds:0} s, tối đa {HDCConfigReport.TimeoutSeconds:0} s game chạy, quảng cáo bắt đầu khi tải xong.";
                return;
            }

            if (!HDCAds.IsInitialized && HDCAdsRuntime.Current.Context.Consent.Current != HDCConsentGate.Step.NotStarted && !HDCAds.Consent.IsDone)
            {
                statusText.text = HDCDebugStyle.Colored(HDCDebugStyle.WarnHex, "Đang chờ consent và ATT") + " · quảng cáo bắt đầu khi người dùng trả lời xong.";
                return;
            }

            if (initializing)
            {
                statusText.text = HDCDebugStyle.Colored(HDCDebugStyle.WarnHex, "Đang khởi tạo HDCAds") + " · quảng cáo bắt đầu khi SDK sẵn sàng.";
                return;
            }

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
            SetOpen(false);
        }

        private void SetOpen(bool open)
        {
            windowCanvas.enabled = open;
            windowRaycaster.enabled = open;
            PresentPages();
        }

        private void PresentPages()
        {
            for (int i = 0; i < pages.Length; i++)
            {
                if (i != currentPage)
                    pages[i].page.Present(false, IsOpen);
            }

            pages[currentPage].page.Present(true, IsOpen);
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

        private void LoadFonts()
        {
            Font builtin = null;
            var loaded = new HashSet<Font>();
            foreach (Text text in GetComponentsInChildren<Text>(true))
            {
                if (text.font == null)
                {
                    if (builtin == null)
                        builtin = BuiltinFont();
                    text.font = builtin;
                }

                if (text.font.dynamic && loaded.Add(text.font))
                    text.font.RequestCharactersInTexture("A", text.fontSize, text.fontStyle);
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
