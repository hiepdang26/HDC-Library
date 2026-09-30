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
        private const string RewardedId = "demo_rewarded";
        private const string AppOpenId = "demo_app_open";
        private const string BannerViewId = "demo_banner_view";
        private const string MrecId = "demo_mrec";

        [SerializeField] private string[] androidInterstitialUnits = { "ca-app-pub-3940256099942544/1033173712" };
        [SerializeField] private string[] iosInterstitialUnits = { "ca-app-pub-3940256099942544/4411468910" };
        [SerializeField] private string[] androidNativeUnits = { "ca-app-pub-3940256099942544/2247696110" };
        [SerializeField] private string[] iosNativeUnits = { "ca-app-pub-3940256099942544/3986624511" };
        [SerializeField] private string androidRewardedUnit = "ca-app-pub-3940256099942544/5224354917";
        [SerializeField] private string iosRewardedUnit = "ca-app-pub-3940256099942544/1712485313";
        [SerializeField] private string androidAppOpenUnit = "ca-app-pub-3940256099942544/9257395921";
        [SerializeField] private string iosAppOpenUnit = "ca-app-pub-3940256099942544/5575463023";
        [SerializeField] private string androidAdaptiveBannerUnit = "ca-app-pub-3940256099942544/9214589741";
        [SerializeField] private string iosAdaptiveBannerUnit = "ca-app-pub-3940256099942544/2435281174";
        [SerializeField] private string androidFixedBannerUnit = "ca-app-pub-3940256099942544/6300978111";
        [SerializeField] private string iosFixedBannerUnit = "ca-app-pub-3940256099942544/2934735716";
        [SerializeField] private HDCFullscreenOptions fullscreenOptions = new HDCFullscreenOptions();
        [SerializeField] private HDCPopupOptions popupOptions = new HDCPopupOptions { x = 0.5f, y = 0.5f };
        [SerializeField] private HDCBannerOptions bannerOptions = new HDCBannerOptions();

        private readonly List<string> log = new List<string>();
        private HDCBannerViewPlacement bannerPlacement = HDCBannerViewPlacement.FullBottom;
        private Vector2 buttonsScroll;
        private Vector2 logScroll;

        private static bool IsIos => Application.platform == RuntimePlatform.IPhonePlayer;

        private string[] InterstitialUnits => IsIos ? iosInterstitialUnits : androidInterstitialUnits;

        private string[] NativeUnits => IsIos ? iosNativeUnits : androidNativeUnits;

        private string FixedBannerUnit => IsIos ? iosFixedBannerUnit : androidFixedBannerUnit;

        private string BannerViewUnit =>
            bannerPlacement == HDCBannerViewPlacement.FullBottom || bannerPlacement == HDCBannerViewPlacement.FullTop
                ? IsIos ? iosAdaptiveBannerUnit : androidAdaptiveBannerUnit
                : FixedBannerUnit;

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
            DrawRewarded();
            DrawAppOpen();
            DrawBannerView();
            DrawMrec();
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
            if (Button("Test device"))
                HDCAdsSdk.EnableTestDevice();
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

        private void DrawRewarded()
        {
            GUILayout.Label("Rewarded");
            GUILayout.BeginHorizontal();
            if (Button("Load"))
                HDCAdsSdk.LoadRewarded(RewardedId, IsIos ? iosRewardedUnit : androidRewardedUnit);
            if (Button("Ready?"))
                Append("rewarded ready=" + HDCAdsSdk.IsRewardedReady(RewardedId));
            if (Button("Show"))
                Append("rewarded show=" + HDCAdsSdk.ShowRewarded(RewardedId, rewarded => Append("rewarded closed, reward=" + rewarded)));
            GUILayout.EndHorizontal();
        }

        private void DrawAppOpen()
        {
            GUILayout.Label("App open");
            GUILayout.BeginHorizontal();
            if (Button("Load"))
                HDCAdsSdk.LoadAppOpen(AppOpenId, IsIos ? iosAppOpenUnit : androidAppOpenUnit);
            if (Button("Ready?"))
                Append("app open ready=" + HDCAdsSdk.IsAppOpenReady(AppOpenId));
            if (Button("Show"))
                Append("app open show=" + HDCAdsSdk.ShowAppOpen(AppOpenId, () => Append("app open closed")));
            GUILayout.EndHorizontal();
        }

        private void DrawBannerView()
        {
            GUILayout.Label("Banner view: " + bannerPlacement);
            GUILayout.BeginHorizontal();
            if (Button("Next"))
            {
                HDCAdsSdk.DestroyBannerView(BannerViewId);
                bannerPlacement = bannerPlacement == HDCBannerViewPlacement.BottomRight
                    ? HDCBannerViewPlacement.FullBottom
                    : bannerPlacement + 1;
            }

            if (Button("Load"))
                HDCAdsSdk.LoadBannerView(BannerViewId, BannerViewUnit, bannerPlacement);
            if (Button("Show"))
                HDCAdsSdk.ShowBannerView(BannerViewId);
            if (Button("Hide"))
                HDCAdsSdk.HideBannerView(BannerViewId);
            GUILayout.EndHorizontal();
        }

        private void DrawMrec()
        {
            GUILayout.Label("MREC");
            GUILayout.BeginHorizontal();
            if (Button("Load"))
                HDCAdsSdk.LoadBannerView(MrecId, FixedBannerUnit, HDCBannerViewPlacement.Mrec);
            if (Button("Show"))
                HDCAdsSdk.ShowBannerView(MrecId);
            if (Button("Center"))
                HDCAdsSdk.MoveBannerView(MrecId, new Vector2(Screen.width / 2f, Screen.height / 2f));
            if (Button("Hide"))
                HDCAdsSdk.HideBannerView(MrecId);
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
