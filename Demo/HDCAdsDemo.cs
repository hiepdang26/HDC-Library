using System.Collections.Generic;
using UnityEngine;

namespace HDC.Ads.Demo
{
    /// <summary>
    /// On-screen buttons that call the HDCAds API the way game code does: a row per channel, then the testing
    /// switches. Add it to a GameObject in a scene without HDCAdsSetup: it starts HDCAds with the small configs
    /// below. Their ad unit ids are placeholders that Google's test ad units replace
    /// (<see cref="HDCAds.Testing.UseTestAdUnits"/>).
    /// </summary>
    public sealed class HDCAdsDemo : MonoBehaviour
    {
        private const int MaxLogLines = 80;
        private const string Position = "demo";
        private const string ForceAdGroup = "demo_force";
        private const string PopupGroup = "demo_popup";

        // Every channel on, none loading on its own: the buttons start them.
        private const string AdsConfig = @"{ ""selectedAdCoreName"": ""demo"",
  ""appLaunchChannel"": { ""isEnabled"": true, ""minWaitSeconds"": 1, ""timeoutSeconds"": 15 },
  ""appResumeChannel"": { ""isEnabled"": true, ""adUnitId"": ""demo-native"", ""layoutGroup"": ""demo"" },
  ""forceAdChannel"": { ""isEnabled"": true, ""positionConfigs"": [ { ""positionName"": ""demo"", ""canShow"": true } ] },
  ""rewardedChannel"": { ""isEnabled"": true },
  ""bannerChannel"": { ""isEnabled"": true, ""fullBottom"": { ""isEnabled"": true } },
  ""mrecChannel"": { ""isEnabled"": true },
  ""popupChannel"": { ""isEnabled"": true, ""positionConfigs"": [ { ""positionName"": ""demo"", ""isEnabled"": true } ] } }";

        // Native units first with AdMob plugin units as backups; the app launch shows the app open ad.
        private const string CoreConfig = @"{
  ""comebackChannel"": { ""launchAdType"": 1 },
  ""forceAdLayoutConfig"": { ""layoutGroups"": [ { ""groupName"": ""demo"", ""layouts"": [ { ""layout"": ""fs_single_cls_01"", ""layoutTime"": 5 } ] } ] },
  ""forceAdGroups"": [ { ""groupName"": ""demo_force"", ""positionNames"": [ ""demo"" ], ""mediationPriority"": 1, ""useBackup"": true,
      ""androidUnit"": { ""id"": ""demo-native"", ""layoutGroupName"": ""demo"" }, ""admobUnit"": { ""id"": ""demo-interstitial"" } } ],
  ""rewardedUnit"": { ""mediationPriority"": 0, ""admobUnit"": { ""id"": ""demo-rewarded"" } },
  ""appOpenUnit"": { ""mediationPriority"": 0, ""admobUnit"": { ""id"": ""demo-app-open"" } },
  ""mrecUnit"": { ""mediationPriority"": 0, ""admobUnit"": { ""id"": ""demo-mrec"" } },
  ""bannerUnit"": { ""fullBottom"": { ""mediationPriority"": 1, ""useBackup"": true,
      ""androidUnit"": { ""id"": ""demo-native"", ""layouts"": [ ""bn_single_transparent_01"" ] }, ""admobUnit"": { ""id"": ""demo-banner"" } } },
  ""popupGroups"": [ { ""groupName"": ""demo_popup"", ""positionNames"": [ ""demo"" ],
      ""androidUnit"": { ""id"": ""demo-native"", ""layout"": ""popup_single_manual_01"", ""timeShow"": 3 } } ] }";

        [Tooltip("Loads Google's test ad units in place of the placeholder ids in the configs.")]
        [SerializeField] private bool useTestAdUnits = true;

        private readonly List<string> log = new List<string>();
        private Vector2 buttonsScroll;
        private Vector2 logScroll;

        private void Awake()
        {
            HDCAds.Testing.DebugLog = true;
            HDCAds.Testing.UseTestAdUnits = useTestAdUnits;
            HDCAds.Revenue += OnRevenue;
            HDCAds.AppLaunch.Completed += OnLaunchCompleted;
        }

        private void OnDestroy()
        {
            HDCAds.Revenue -= OnRevenue;
            HDCAds.AppLaunch.Completed -= OnLaunchCompleted;
        }

        private void OnRevenue(HDCAdRevenue revenue) => Append("revenue: " + revenue);

        private void OnLaunchCompleted() => Append("app launch completed");

        private void OnGUI()
        {
            float scale = Mathf.Max(1f, Screen.dpi / 160f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float width = Screen.width / scale - 20f;
            float height = Screen.height / scale - 20f;
            GUILayout.BeginArea(new Rect(10f, 10f, width, height));

            buttonsScroll = GUILayout.BeginScrollView(buttonsScroll, GUILayout.Height(height * 0.6f));
            DrawSetup();
            DrawForceAd();
            DrawRewarded();
            DrawLaunchAndResume();
            DrawBanner();
            DrawMrec();
            DrawPopup();
            DrawTesting();
            GUILayout.EndScrollView();

            logScroll = GUILayout.BeginScrollView(logScroll);
            for (int i = log.Count - 1; i >= 0; i--)
                GUILayout.Label(log[i]);
            GUILayout.EndScrollView();

            GUILayout.EndArea();
        }

        private void DrawSetup()
        {
            GUILayout.Label(HDCAds.IsInitialized ? "HDCAds: initialized" : "HDCAds: not initialized");
            GUILayout.BeginHorizontal();
            if (Button("Initialize"))
                HDCAds.Initialize(AdsConfig, CoreConfig, () => Append("initialized"));
            if (Button(HDCAds.IsAdsRemoved ? "Restore ads" : "Remove ads"))
                HDCAds.SetAdsRemoved(!HDCAds.IsAdsRemoved);
            GUILayout.EndHorizontal();
        }

        private void DrawForceAd()
        {
            GUILayout.Label("Force ad at \"" + Position + "\"");
            GUILayout.BeginHorizontal();
            if (Button("Init"))
                HDCAds.ForceAd.Initialize(ForceAdGroup);
            if (Button("Can show?"))
                Append("force ad can show=" + HDCAds.ForceAd.CanShow(Position));
            if (Button("Show"))
                Append("force ad show=" + HDCAds.ForceAd.Show(Position, () => Append("force ad done")));
            GUILayout.EndHorizontal();
        }

        private void DrawRewarded()
        {
            GUILayout.Label("Rewarded");
            GUILayout.BeginHorizontal();
            if (Button("Init"))
                HDCAds.Rewarded.Initialize();
            if (Button("Can show?"))
                Append("rewarded can show=" + HDCAds.Rewarded.CanShow);
            if (Button("Show"))
                Append("rewarded show=" + HDCAds.Rewarded.Show(Position, () => Append("reward earned"), () => Append("rewarded closed")));
            GUILayout.EndHorizontal();
        }

        private void DrawLaunchAndResume()
        {
            GUILayout.Label("App launch and resume");
            GUILayout.BeginHorizontal();
            if (Button("Launch"))
                HDCAds.AppLaunch.Initialize();
            if (Button("Resume on"))
                HDCAds.AppResume.Initialize();
            if (Button("Skip next resume"))
                HDCAds.AppResume.Block();
            GUILayout.EndHorizontal();
        }

        private void DrawBanner()
        {
            GUILayout.Label("Banner at the bottom");
            GUILayout.BeginHorizontal();
            if (Button("Init"))
                HDCAds.Banner.Initialize();
            if (Button("Show"))
                Append("banner show=" + HDCAds.Banner.Show());
            if (Button("Expand"))
                Append("banner expand=" + HDCAds.Banner.Expand());
            if (Button("Hide"))
                HDCAds.Banner.Hide();
            GUILayout.EndHorizontal();
        }

        private void DrawMrec()
        {
            GUILayout.Label("MREC");
            GUILayout.BeginHorizontal();
            if (Button("Init"))
                HDCAds.Mrec.Initialize();
            if (Button("Show"))
                Append("mrec show=" + HDCAds.Mrec.Show());
            if (Button("Center"))
                HDCAds.Mrec.Move(HDCAdPosition.Center);
            if (Button("Hide"))
                HDCAds.Mrec.Hide();
            GUILayout.EndHorizontal();
        }

        private void DrawPopup()
        {
            GUILayout.Label("Popup at \"" + Position + "\"");
            GUILayout.BeginHorizontal();
            if (Button("Init"))
                HDCAds.Popup.Initialize(PopupGroup);
            if (Button("Place"))
            {
                // A popup shows only once placed: here over the middle of the screen, in screen pixels.
                HDCAds.Popup.Move(Position, new Rect(Screen.width * 0.1f, Screen.height * 0.3f, Screen.width * 0.8f, Screen.height * 0.4f));
            }

            if (Button("Show"))
                Append("popup show=" + HDCAds.Popup.Show(Position));
            if (Button("Hide"))
                HDCAds.Popup.Hide(Position);
            GUILayout.EndHorizontal();
        }

        private void DrawTesting()
        {
            GUILayout.Label("Testing: test device " + (HDCAds.Testing.IsTestDevice ? "on" : "off")
                + ", test ad units " + (HDCAds.Testing.UseTestAdUnits ? "on" : "off"));
            GUILayout.BeginHorizontal();
            if (Button("Test device"))
                HDCAds.Testing.EnableTestDevice();
            if (Button("Meta test on"))
                Append("meta test=" + HDCAds.Testing.EnableMetaTestMode() + " hash=" + HDCAds.Testing.MetaTestDeviceHash);
            if (Button("Meta test off"))
                HDCAds.Testing.DisableMetaTestMode();
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
