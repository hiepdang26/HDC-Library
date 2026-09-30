using System.Collections.Generic;
using UnityEngine;

namespace HDC.Ads.Demo
{
    /// <summary>
    /// On-screen buttons to try HDC ads on a device or in the Editor. Add it to a GameObject in any scene.
    /// The default ad units are Google's test units.
    /// </summary>
    public sealed class HDCAdsDemo : MonoBehaviour
    {
        private const int MaxLogLines = 80;
        private const string InterstitialId = "demo_interstitial";
        private const string FullscreenId = "demo_fullscreen";
        private const string PopupId = "demo_popup";
        private const string BannerId = "demo_banner";

        [SerializeField] private string[] androidInterstitialUnits = { "ca-app-pub-3940256099942544/1033173712" };
        [SerializeField] private string[] iosInterstitialUnits = { "ca-app-pub-3940256099942544/4411468910" };
        [SerializeField] private string[] androidNativeUnits = { "ca-app-pub-3940256099942544/2247696110" };
        [SerializeField] private string[] iosNativeUnits = { "ca-app-pub-3940256099942544/3986624511" };
        [SerializeField] private HDCFullscreenOptions fullscreenOptions = new HDCFullscreenOptions();
        [SerializeField] private HDCPopupOptions popupOptions = new HDCPopupOptions { x = 0.5f, y = 0.5f };
        [SerializeField] private HDCBannerOptions bannerOptions = new HDCBannerOptions();

        private readonly List<string> log = new List<string>();
        private Vector2 buttonsScroll;
        private Vector2 logScroll;

        private static bool IsIos => Application.platform == RuntimePlatform.IPhonePlayer;

        private string[] InterstitialUnits => IsIos ? iosInterstitialUnits : androidInterstitialUnits;

        private string[] NativeUnits => IsIos ? iosNativeUnits : androidNativeUnits;

        private void Awake()
        {
            HDCAdsSdk.DebugLog = true;
            HDCAdsSdk.AdEvent += OnAdEvent;
        }

        private void OnDestroy()
        {
            HDCAdsSdk.AdEvent -= OnAdEvent;
        }

        private void OnAdEvent(HDCAdEvent adEvent)
        {
            Append(adEvent.ToString());
        }

        private void OnGUI()
        {
            float scale = Mathf.Max(1f, Screen.dpi / 160f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float width = Screen.width / scale - 20f;
            float height = Screen.height / scale - 20f;
            GUILayout.BeginArea(new Rect(10f, 10f, width, height));

            buttonsScroll = GUILayout.BeginScrollView(buttonsScroll, GUILayout.Height(height * 0.6f));
            DrawSdk();
            DrawInterstitial();
            DrawFullscreen();
            DrawPopup();
            DrawBanner();
            GUILayout.EndScrollView();

            logScroll = GUILayout.BeginScrollView(logScroll);
            for (int i = log.Count - 1; i >= 0; i--)
                GUILayout.Label(log[i]);
            GUILayout.EndScrollView();

            GUILayout.EndArea();
        }

        private void DrawSdk()
        {
            GUILayout.Label("SDK");
            GUILayout.BeginHorizontal();
            if (Button("Initialize"))
                HDCAdsSdk.Initialize(() => Append("Initialize callback"));
            if (Button("Meta test on"))
                Append("meta test=" + HDCAdsSdk.EnableMetaTestMode() + " hash=" + HDCAdsSdk.GetMetaTestDeviceHash());
            if (Button("Meta test off"))
                HDCAdsSdk.DisableMetaTestMode();
            GUILayout.EndHorizontal();
        }

        private void DrawInterstitial()
        {
            GUILayout.Label("Interstitial");
            GUILayout.BeginHorizontal();
            if (Button("Load"))
                HDCAdsSdk.LoadInterstitial(InterstitialId, InterstitialUnits);
            if (Button("Ready?"))
                Append("interstitial ready=" + HDCAdsSdk.IsInterstitialReady(InterstitialId));
            if (Button("Show"))
                Append("interstitial show=" + HDCAdsSdk.ShowInterstitial(InterstitialId));
            GUILayout.EndHorizontal();
        }

        private void DrawFullscreen()
        {
            GUILayout.Label("Fullscreen native");
            GUILayout.BeginHorizontal();
            if (Button("Load"))
                HDCAdsSdk.LoadFullscreen(FullscreenId, NativeUnits);
            if (Button("Ready?"))
                Append("fullscreen ready=" + HDCAdsSdk.IsFullscreenReady(FullscreenId));
            if (Button("Show"))
                Append("fullscreen show=" + HDCAdsSdk.ShowFullscreen(FullscreenId, fullscreenOptions));
            if (Button("Hide"))
                HDCAdsSdk.HideFullscreen(FullscreenId);
            GUILayout.EndHorizontal();
        }

        private void DrawPopup()
        {
            GUILayout.Label("Popup native");
            GUILayout.BeginHorizontal();
            if (Button("Load"))
                HDCAdsSdk.LoadPopup(PopupId, NativeUnits, popupOptions);
            if (Button("State"))
                Append("popup state=" + HDCAdsSdk.GetPopupState(PopupId));
            if (Button("Show"))
                Append("popup show=" + HDCAdsSdk.ShowPopup(PopupId));
            if (Button("Close"))
                HDCAdsSdk.ClosePopup(PopupId);
            GUILayout.EndHorizontal();
        }

        private void DrawBanner()
        {
            GUILayout.Label("Banner native");
            GUILayout.BeginHorizontal();
            if (Button("Load"))
                HDCAdsSdk.LoadBanner(BannerId, NativeUnits, bannerOptions);
            if (Button("Show"))
                HDCAdsSdk.ShowBanner(BannerId);
            if (Button("Expand"))
                Append("banner expand=" + HDCAdsSdk.ExpandBanner(BannerId));
            if (Button("Hide"))
                HDCAdsSdk.HideBanner(BannerId);
            GUILayout.EndHorizontal();
        }

        private static bool Button(string text) => GUILayout.Button(text, GUILayout.Height(40f));

        private void Append(string line)
        {
            log.Add($"{Time.realtimeSinceStartup:0.0}s {line}");
            if (log.Count > MaxLogLines)
                log.RemoveAt(0);
        }
    }
}
