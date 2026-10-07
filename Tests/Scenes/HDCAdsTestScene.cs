using System;
using System.Collections.Generic;
using HDC.Ads.DebugUI;
using UnityEngine;

namespace HDC.Ads.TestScenes
{
    [AddComponentMenu("HDC/HDC Ads Test Scene")]
    public sealed class HDCAdsTestScene : MonoBehaviour
    {
        private const int MaxLogLines = 100;
        private const float Margin = 8f;
        private const float RowHeight = 44f;
        private const float StatusHeight = 76f;
        private const float WideScreen = 560f;
        private const float DragThreshold = 8f;
        private const float RefreshSeconds = 1f;
        private const float LogShare = 0.4f;
        private const string RewardedPosition = "test_scene";

        private static readonly string[] Tabs = { "Force ad", "Rewarded", "Launch", "Banner", "MREC", "Popup", "Info" };
        private static readonly string[] BannerSlots = Enum.GetNames(typeof(HDCBannerSlot));
        private static readonly HDCAdPosition[] MrecPositions = { HDCAdPosition.Bottom, HDCAdPosition.BottomLeft, HDCAdPosition.BottomRight };
        private static readonly string[] MrecPositionNames = Array.ConvertAll(MrecPositions, position => position.ToString());
        private static readonly string[] NoNames = new string[0];

        private readonly List<string> log = new List<string>();
        private readonly Dictionary<string, bool> forceReady = new Dictionary<string, bool>();
        private readonly Dictionary<string, bool> popupReady = new Dictionary<string, bool>();
        private IReadOnlyList<string> forceGroups = NoNames;
        private IReadOnlyList<string> forcePositions = NoNames;
        private IReadOnlyList<string> popupGroups = NoNames;
        private IReadOnlyList<string> popupPositions = NoNames;
        private int tab;
        private int bannerSlot;
        private int mrecPosition;
        private Rect bottomArea;
        private Vector2 contentScroll;
        private Vector2 logScroll;
        private float dragDistance;
        private float nextRefresh;
        private float nextPanelSearch;
        private HDCAdsDebugPanel panel;

        private void OnEnable()
        {
            HDCAds.Initialized += OnInitialized;
            HDCAds.Revenue += OnRevenue;
            HDCAds.AppLaunch.Completed += OnLaunchCompleted;
            HDCCustomConfig.Updated += OnCustomConfigUpdated;
            HDCAdjust.AttributionChanged += OnAttributionChanged;
            Append(HDCAds.IsInitialized ? "HDCAds is initialized" : "HDCAds is not initialized yet: start from the HDCAdsTestBoot scene");
        }

        private void OnDisable()
        {
            HDCAds.Initialized -= OnInitialized;
            HDCAds.Revenue -= OnRevenue;
            HDCAds.AppLaunch.Completed -= OnLaunchCompleted;
            HDCCustomConfig.Updated -= OnCustomConfigUpdated;
            HDCAdjust.AttributionChanged -= OnAttributionChanged;
        }

        private void Update()
        {
            if (Time.unscaledTime < nextRefresh || IsDebugPanelOpen)
                return;
            nextRefresh = Time.unscaledTime + RefreshSeconds;
            forceGroups = HDCAds.Testing.Groups(HDCAdChannel.ForceAd);
            forcePositions = HDCAds.Testing.Positions(HDCAdChannel.ForceAd);
            popupGroups = HDCAds.Testing.Groups(HDCAdChannel.Popup);
            popupPositions = HDCAds.Testing.Positions(HDCAdChannel.Popup);
            if (tab == 0)
                ReadReadiness(forceGroups, forceReady, HDCAds.ForceAd.IsGroupReady);
            else if (tab == 5)
                ReadReadiness(popupGroups, popupReady, HDCAds.Popup.IsGroupReady);
        }

        private void OnGUI()
        {
            if (IsDebugPanelOpen)
                return;

            float scale = Mathf.Max(1f, Screen.dpi / 160f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            Rect safe = Screen.safeArea;
            var area = new Rect(safe.x / scale + Margin, (Screen.height - safe.yMax) / scale + Margin,
                safe.width / scale - 2f * Margin, safe.height / scale - 2f * Margin);
            int tabRows = area.width < WideScreen ? 2 : 1;
            var status = new Rect(area.x, area.y, area.width, StatusHeight);
            var tabs = new Rect(area.x, status.yMax + Margin, area.width, RowHeight * tabRows);
            float logHeight = area.height * LogShare;
            var logArea = new Rect(area.x, area.yMax - logHeight, area.width, logHeight);
            var content = new Rect(area.x, tabs.yMax + Margin, area.width, logArea.y - tabs.yMax - 2f * Margin);
            bottomArea = new Rect(logArea.x * scale, Screen.height - logArea.yMax * scale, logArea.width * scale, logArea.height * scale);

            contentScroll = DragScroll(content, contentScroll);
            logScroll = DragScroll(logArea, logScroll);

            GUILayout.BeginArea(status);
            DrawStatus();
            GUILayout.EndArea();

            int chosen = GUI.SelectionGrid(tabs, tab, Tabs, tabRows == 1 ? Tabs.Length : 4);
            if (chosen != tab)
            {
                tab = chosen;
                contentScroll = Vector2.zero;
                nextRefresh = 0f;
            }

            GUILayout.BeginArea(content);
            contentScroll = GUILayout.BeginScrollView(contentScroll);
            DrawTab();
            GUILayout.EndScrollView();
            GUILayout.EndArea();

            GUILayout.BeginArea(logArea, GUI.skin.box);
            logScroll = GUILayout.BeginScrollView(logScroll);
            for (int i = log.Count - 1; i >= 0; i--)
                GUILayout.Label(log[i]);
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawStatus()
        {
            GUILayout.Label((HDCAds.IsInitialized ? "HDCAds: initialized" : "HDCAds: not initialized")
                + (HDCAds.AppLaunch.IsCompleted ? " · app launch done" : " · app launch running")
                + (HDCAds.IsAdsRemoved ? " · ads removed" : string.Empty));
            GUILayout.BeginHorizontal();
            if (Button("Debug panel"))
                OpenDebugPanel();
            if (!HDCAds.IsInitialized && Button("Initialize"))
            {
                Append("HDCAdsSetup.InitializeAds()");
                HDCAdsSetup.InitializeAds(() => Append("HDCAdsSetup.InitializeAds: ready"));
            }

            GUILayout.EndHorizontal();
        }

        private void DrawTab()
        {
            switch (tab)
            {
                case 0:
                    DrawForceAd();
                    break;
                case 1:
                    DrawRewarded();
                    break;
                case 2:
                    DrawLaunchAndResume();
                    break;
                case 3:
                    DrawBanner();
                    break;
                case 4:
                    DrawMrec();
                    break;
                case 5:
                    DrawPopup();
                    break;
                default:
                    DrawInfo();
                    break;
            }
        }

        private void DrawForceAd()
        {
            if (forceGroups.Count == 0 && forcePositions.Count == 0)
                GUILayout.Label("No force ad group or position in the applied config.");
            foreach (string group in forceGroups)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Group " + group + Readiness(forceReady, group));
                if (Button("Init"))
                {
                    HDCAds.ForceAd.Initialize(group);
                    Append($"ForceAd.Initialize(\"{group}\")");
                }

                if (Button("Reinit"))
                    Append($"ForceAd.Reinitialize(\"{group}\") -> {HDCAds.ForceAd.Reinitialize(group)}");
                GUILayout.EndHorizontal();
            }

            foreach (string position in forcePositions)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(position + " (" + HDCAds.ForceAd.ImpressionCount(position) + " shown)");
                if (Button("Can show?"))
                    Append($"ForceAd.CanShow(\"{position}\") -> {HDCAds.ForceAd.CanShow(position)}");
                if (Button("Show"))
                {
                    bool shown = HDCAds.ForceAd.Show(position, () => Append($"ForceAd \"{position}\": onDone"));
                    Append($"ForceAd.Show(\"{position}\") -> {shown}");
                }

                GUILayout.EndHorizontal();
            }
        }

        private void DrawRewarded()
        {
            GUILayout.Label("Rewarded at \"" + RewardedPosition + "\" (" + HDCAds.Rewarded.ImpressionCount + " shown)");
            GUILayout.BeginHorizontal();
            if (Button("Init"))
            {
                HDCAds.Rewarded.Initialize();
                Append("Rewarded.Initialize()");
            }

            if (Button("Can show?"))
                Append("Rewarded.CanShow -> " + HDCAds.Rewarded.CanShow);
            if (Button("Show"))
            {
                bool shown = HDCAds.Rewarded.Show(RewardedPosition, () => Append("Rewarded: onRewarded"), () => Append("Rewarded: onClosed"));
                Append("Rewarded.Show(\"" + RewardedPosition + "\") -> " + shown);
            }

            GUILayout.EndHorizontal();
        }

        private void DrawLaunchAndResume()
        {
            GUILayout.Label("App launch: " + (HDCAds.AppLaunch.IsCompleted ? "completed" : "running"));
            GUILayout.BeginHorizontal();
            if (Button("Init"))
            {
                HDCAds.AppLaunch.Initialize();
                Append("AppLaunch.Initialize()");
            }

            if (Button("Can show?"))
                Append("AppLaunch.CanShow -> " + HDCAds.AppLaunch.CanShow);
            GUILayout.EndHorizontal();

            GUILayout.Label("App resume: " + (HDCAds.AppResume.IsInitialized ? "on" : "off"));
            GUILayout.BeginHorizontal();
            if (Button("Init"))
            {
                HDCAds.AppResume.Initialize();
                Append("AppResume.Initialize()");
            }

            if (Button("Skip next"))
            {
                HDCAds.AppResume.Block();
                Append("AppResume.Block()");
            }

            GUILayout.EndHorizontal();
        }

        private void DrawBanner()
        {
            bannerSlot = GUILayout.SelectionGrid(bannerSlot, BannerSlots, 3, GUILayout.Height(RowHeight * 2f));
            var slot = (HDCBannerSlot)bannerSlot;
            GUILayout.BeginHorizontal();
            if (Button("Init"))
            {
                HDCAds.Banner.Initialize(slot);
                Append($"Banner.Initialize({slot})");
            }

            if (Button("Show"))
                Append($"Banner.Show({slot}) -> {HDCAds.Banner.Show(slot)}");
            if (Button("Hide"))
            {
                HDCAds.Banner.Hide(slot);
                Append($"Banner.Hide({slot})");
            }

            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (Button("Expand"))
                Append($"Banner.Expand({slot}) -> {HDCAds.Banner.Expand(slot)}");
            if (Button("Can show?"))
                Append($"Banner.CanShow({slot}) -> {HDCAds.Banner.CanShow(slot)}");
            GUILayout.EndHorizontal();
        }

        private void DrawMrec()
        {
            GUILayout.Label("The MREC shows over the log, so these buttons stay free.");
            mrecPosition = GUILayout.SelectionGrid(mrecPosition, MrecPositionNames, MrecPositionNames.Length, GUILayout.Height(RowHeight));
            HDCAdPosition position = MrecPositions[mrecPosition];
            GUILayout.BeginHorizontal();
            if (Button("Init"))
            {
                HDCAds.Mrec.Initialize();
                Append("Mrec.Initialize()");
            }

            if (Button("Show"))
            {
                HDCAds.Mrec.Move(position);
                Append($"Mrec.Move({position}), Mrec.Show() -> {HDCAds.Mrec.Show()}");
            }

            if (Button("Hide"))
            {
                HDCAds.Mrec.Hide();
                Append("Mrec.Hide()");
            }

            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (Button("Move"))
            {
                HDCAds.Mrec.Move(position);
                Append($"Mrec.Move({position})");
            }

            if (Button("Can show?"))
                Append("Mrec.CanShow -> " + HDCAds.Mrec.CanShow);
            if (Button("Size"))
                Append("Mrec.SizeInPixels -> " + HDCAds.Mrec.SizeInPixels);
            GUILayout.EndHorizontal();
        }

        private void DrawPopup()
        {
            if (popupGroups.Count == 0 && popupPositions.Count == 0)
                GUILayout.Label("No popup group or position in the applied config.");
            else
                GUILayout.Label("Show places the popup over the log, so these buttons stay free.");
            foreach (string group in popupGroups)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Group " + group + Readiness(popupReady, group));
                if (Button("Init"))
                {
                    HDCAds.Popup.Initialize(group);
                    Append($"Popup.Initialize(\"{group}\")");
                }

                if (Button("Reinit"))
                    Append($"Popup.Reinitialize(\"{group}\") -> {HDCAds.Popup.Reinitialize(group)}");
                GUILayout.EndHorizontal();
            }

            foreach (string position in popupPositions)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(position);
                if (Button("Show"))
                {
                    HDCAds.Popup.Move(position, PopupArea());
                    Append($"Popup.Move(\"{position}\"), Popup.Show(\"{position}\") -> {HDCAds.Popup.Show(position)}");
                }

                if (Button("Hide"))
                {
                    HDCAds.Popup.Hide(position);
                    Append($"Popup.Hide(\"{position}\")");
                }

                if (Button("Can show?"))
                    Append($"Popup.CanShow(\"{position}\") -> {HDCAds.Popup.CanShow(position)}");
                GUILayout.EndHorizontal();
            }
        }

        private void DrawInfo()
        {
            GUILayout.Label("Google test device: " + OnOff(HDCAds.Testing.IsTestDevice)
                + " · Google test ad units: " + OnOff(HDCAds.Testing.UseTestAdUnits)
                + " · Meta test mode: " + OnOff(HDCAds.Testing.IsMetaTestMode));
            GUILayout.BeginHorizontal();
            if (Button(HDCAds.IsAdsRemoved ? "Restore ads" : "Remove ads"))
            {
                HDCAds.SetAdsRemoved(!HDCAds.IsAdsRemoved);
                Append("SetAdsRemoved(" + HDCAds.IsAdsRemoved + ")");
            }

            if (Button("Meta test on"))
                Append("Testing.EnableMetaTestMode() -> " + HDCAds.Testing.EnableMetaTestMode() + ", hash " + HDCAds.Testing.MetaTestDeviceHash);
            if (Button("Meta test off"))
            {
                HDCAds.Testing.DisableMetaTestMode();
                Append("Testing.DisableMetaTestMode()");
            }

            GUILayout.EndHorizontal();

            GUILayout.Label("Custom keys: " + (HDCCustomConfig.IsReady ? "ready" : "not ready"));
            foreach (string key in HDCCustomConfig.Keys)
                GUILayout.Label("  " + key + " = " + HDCCustomConfig.Get(key));

            GUILayout.Label("Adjust: " + (HDCAdjust.IsAttributionReady ? "network " + HDCAdjust.Network : "no attribution yet"));
            if (HDCAdjust.IsAttributionReady)
                GUILayout.Label("  " + HDCAdjust.Attribution);
        }

        private Vector2 DragScroll(Rect area, Vector2 scroll)
        {
            Event current = Event.current;
            if (current.type == EventType.MouseDown && area.Contains(current.mousePosition))
                dragDistance = 0f;
            if (current.type != EventType.MouseDrag || !area.Contains(current.mousePosition))
                return scroll;
            dragDistance += Mathf.Abs(current.delta.y);
            if (dragDistance < DragThreshold)
                return scroll;
            GUIUtility.hotControl = 0;
            current.Use();
            return new Vector2(scroll.x, Mathf.Max(0f, scroll.y - current.delta.y));
        }

        private HDCAdsDebugPanel DebugPanel
        {
            get
            {
                if (panel == null && Time.unscaledTime >= nextPanelSearch)
                {
                    nextPanelSearch = Time.unscaledTime + RefreshSeconds;
#if UNITY_2023_1_OR_NEWER
                    panel = FindAnyObjectByType<HDCAdsDebugPanel>();
#else
                    panel = FindObjectOfType<HDCAdsDebugPanel>();
#endif
                }

                return panel;
            }
        }

        private bool IsDebugPanelOpen
        {
            get
            {
                HDCAdsDebugPanel debugPanel = DebugPanel;
                return debugPanel != null && debugPanel.IsOpen;
            }
        }

        private void OpenDebugPanel()
        {
            HDCAdsDebugPanel debugPanel = DebugPanel;
            if (debugPanel != null)
                debugPanel.Open();
            else
                Append("No HDCAdsDebugPanel in the open scenes");
        }

        private static void ReadReadiness(IReadOnlyList<string> groups, Dictionary<string, bool> ready, Func<string, bool> isReady)
        {
            ready.Clear();
            if (!HDCAds.IsInitialized)
                return;
            foreach (string group in groups)
                ready[group] = isReady(group);
        }

        private static string Readiness(Dictionary<string, bool> ready, string group) =>
            !ready.TryGetValue(group, out bool isReady) ? string.Empty : isReady ? ": ready" : ": not ready";

        private Rect PopupArea() =>
            bottomArea.width > 0f ? bottomArea : new Rect(0f, 0f, Screen.width, Screen.height * LogShare);

        private static string OnOff(bool on) => on ? "on" : "off";

        private static bool Button(string text) => GUILayout.Button(text, GUILayout.Height(RowHeight));

        private void OnInitialized() => Append("HDCAds.Initialized");

        private void OnRevenue(HDCAdRevenue revenue) => Append("HDCAds.Revenue: " + revenue);

        private void OnLaunchCompleted() => Append("AppLaunch.Completed");

        private void OnCustomConfigUpdated() => Append("HDCCustomConfig.Updated: " + HDCCustomConfig.Keys.Count + " keys");

        private void OnAttributionChanged(HDCAdjustAttribution attribution) => Append("HDCAdjust.AttributionChanged: " + attribution);

        private void Append(string line)
        {
            Debug.Log("[HDCAdsTestScene] " + line);
            log.Add($"{Time.realtimeSinceStartup:0.0}s {line}");
            if (log.Count > MaxLogLines)
                log.RemoveAt(0);
        }
    }
}
