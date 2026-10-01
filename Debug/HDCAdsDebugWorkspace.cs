using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace HDC.Ads.DebugUI
{
    /// <summary>
    /// The ad systems workspace of the debug panel. Pick a channel, then a group and a position from the configs
    /// HDCAds runs on, and call the HDCAds API with the action buttons. Every ad unit of the channel's groups shows
    /// its state: loading, ready, showing, or failed with the SDK's error code and what it means. The latest events
    /// of the channel and its configs and gates follow. Everything refreshes every second and on every ad event.
    /// </summary>
    public sealed class HDCAdsDebugWorkspace : MonoBehaviour
    {
        private const string RewardedPosition = "debug_rw";
        private const int EventRows = 15;

        private static readonly string[] MrecPositions = { "TopLeft", "Top", "TopRight", "Center", "BottomLeft", "Bottom", "BottomRight" };

        private static readonly Color TabColor = new Color(0.118f, 0.161f, 0.231f, 1f);
        private static readonly Color SelectedTabColor = new Color(0.31f, 0.275f, 0.898f, 1f);
        private static readonly Color IdleColor = new Color(0.278f, 0.333f, 0.412f, 1f);
        private static readonly Color BusyColor = new Color(0.851f, 0.467f, 0.024f, 1f);
        private static readonly Color GoodColor = new Color(0.086f, 0.639f, 0.290f, 1f);
        private static readonly Color LiveColor = new Color(0.145f, 0.388f, 0.922f, 1f);
        private static readonly Color BadColor = new Color(0.863f, 0.149f, 0.149f, 1f);
        private static readonly Color GroupColor = new Color(0.118f, 0.161f, 0.231f, 1f);
        private static readonly Color SelectedGroupColor = new Color(0.192f, 0.18f, 0.506f, 1f);

        [Serializable]
        private sealed class ChannelTab
        {
            public string key;
            public Button button;
            public Image dot;
        }

        [Header("Header")]
        [SerializeField] private Text statusText;
        [SerializeField] private Button initSdkButton;
        [SerializeField] private Button refreshButton;

        [Header("Channels")]
        [SerializeField] private ChannelTab[] tabs = new ChannelTab[0];

        [Header("Selection")]
        [SerializeField] private Text channelTitleText;
        [SerializeField] private Text selectionText;
        [SerializeField] private Button groupButton;
        [SerializeField] private Button positionButton;
        [SerializeField] private Button detailButton;

        [Header("Actions")]
        [SerializeField] private Button initButton;
        [SerializeField] private Button showButton;
        [SerializeField] private Button hideButton;
        [SerializeField] private Button utilityPrimaryButton;
        [SerializeField] private Button utilitySecondaryButton;
        [SerializeField] private Text lastCallText;

        [Header("Ad units")]
        [SerializeField] private GameObject groupTemplate;
        [SerializeField] private GameObject unitTemplate;
        [SerializeField] private Text unitsNoticeText;
        [SerializeField] private Button copyReportButton;

        [Header("Events")]
        [SerializeField] private GameObject eventTemplate;
        [SerializeField] private Text eventsNoticeText;

        [Header("System")]
        [SerializeField] private Button systemToggleButton;
        [SerializeField] private GameObject systemBody;
        [SerializeField] private Text summaryText;
        [SerializeField] private Text systemText;

        [Header("Helpers")]
        [SerializeField] private HDCOptionPicker optionPicker;
        [SerializeField] private HDCAdsDebugViewer viewer;
        [Tooltip("Where popups show. Visible while the popup channel is selected.")]
        [SerializeField] private RectTransform popupArea;
        [Tooltip("Seconds between refreshes.")]
        [SerializeField] private float refreshSeconds = 1f;

        private readonly List<GameObject> groupRows = new List<GameObject>();
        private readonly List<GameObject> unitRows = new List<GameObject>();
        private readonly List<GameObject> eventRows = new List<GameObject>();
        private string channel = "FA";
        private string group = "";
        private string position = "";
        private string lastCall = "";
        private float nextRefresh;
        private bool dirty;
        private List<HDCDebugGroup> groups = new List<HDCDebugGroup>();

        private void Awake()
        {
            groupTemplate.SetActive(false);
            unitTemplate.SetActive(false);
            eventTemplate.SetActive(false);

            foreach (ChannelTab tab in tabs)
            {
                string key = tab.key;
                tab.button.onClick.AddListener(() =>
                {
                    channel = key;
                    NormalizeSelection();
                    Refresh();
                });
            }

            initSdkButton.onClick.AddListener(InitializeSdk);
            refreshButton.onClick.AddListener(Refresh);
            groupButton.onClick.AddListener(() => OpenPicker(true));
            positionButton.onClick.AddListener(() => OpenPicker(false));
            detailButton.onClick.AddListener(OnDetailClicked);
            initButton.onClick.AddListener(Init);
            showButton.onClick.AddListener(Show);
            hideButton.onClick.AddListener(Hide);
            utilityPrimaryButton.onClick.AddListener(UtilityPrimary);
            utilitySecondaryButton.onClick.AddListener(UtilitySecondary);
            copyReportButton.onClick.AddListener(CopyReport);
            systemToggleButton.onClick.AddListener(() =>
            {
                systemBody.SetActive(!systemBody.activeSelf);
                Refresh();
            });

            HDCAds.Initialized += OnAdsInitialized;
            HDCAdsSdk.AdEvent += OnAdEvent;
        }

        private void OnDestroy()
        {
            HDCAds.Initialized -= OnAdsInitialized;
            HDCAdsSdk.AdEvent -= OnAdEvent;
        }

        private void OnEnable() => Refresh();

        private void OnDisable() => ShowPopupArea(false);

        private void Update()
        {
            if (!dirty && Time.unscaledTime < nextRefresh)
                return;
            Refresh();
        }

        /// <summary>Redraws everything from the current state of HDCAds.</summary>
        public void Refresh()
        {
            dirty = false;
            nextRefresh = Time.unscaledTime + Mathf.Max(0.2f, refreshSeconds);
            NormalizeSelection();
            groups = HDCAdsDebugModel.Groups(channel, group, position);

            RefreshHeader();
            RefreshTabs();
            RefreshSelection();
            RefreshButtons();
            RefreshUnits();
            RefreshEvents();
            RefreshSystem();
            ShowPopupArea(channel == "PU" && isActiveAndEnabled);
        }

        private void OnAdsInitialized() => dirty = true;

        private void OnAdEvent(HDCAdEvent adEvent) => dirty = true;

        // Selection, as in the channel workspace: the group first, then the positions of that group.

        private void NormalizeSelection()
        {
            string[] groupOptions = Groups();
            group = groupOptions.Length == 0 ? "" : Array.IndexOf(groupOptions, group) >= 0 ? group : groupOptions[0];
            string[] positionOptions = Positions();
            position = positionOptions.Length == 0 ? "" : Array.IndexOf(positionOptions, position) >= 0 ? position : positionOptions[0];
        }

        private string[] Groups()
        {
            if (!HDCAds.IsInitialized)
                return new string[0];
            switch (channel)
            {
                case "FA":
                    return Distinct((HDCAds.CoreConfig.forceAdGroups ?? new HDCAdCoreConfig.ForceAdGroup[0]).Select(g => g?.groupName));
                case "PU":
                    return Distinct((HDCAds.CoreConfig.popupGroups ?? new HDCAdCoreConfig.PopupGroup[0]).Select(g => g?.groupName));
                default:
                    return new string[0];
            }
        }

        private string[] Positions()
        {
            switch (channel)
            {
                case "FA":
                    if (!HDCAds.IsInitialized)
                        return new string[0];
                    return Distinct((HDCAds.Config.forceAdChannel?.positionConfigs ?? new HDCAdsConfig.ForceAdPosition[0])
                        .Select(p => p?.positionName)
                        .Where(p => string.IsNullOrEmpty(group) || HDCAds.CoreConfig.ForceAdGroupAt(p) == group));
                case "PU":
                    if (!HDCAds.IsInitialized)
                        return new string[0];
                    return Distinct((HDCAds.Config.popupChannel?.positionConfigs ?? new HDCAdsConfig.PopupPosition[0])
                        .Select(p => p?.positionName)
                        .Where(p => string.IsNullOrEmpty(group) || HDCAds.CoreConfig.PopupGroupAt(p) == group));
                case "RW":
                    return new[] { RewardedPosition };
                case "BN":
                    return HDCAdsDebugModel.BannerSlots;
                case "MREC":
                    return MrecPositions;
                default:
                    return new string[0];
            }
        }

        private void OpenPicker(bool pickGroup)
        {
            string label = PositionLabel();
            string title = pickGroup ? $"Select Group - {channel}" : $"Select {label} - {channel}";
            string subtitle = pickGroup
                ? "Choose a group to filter the current channel."
                : channel == "BN" ? "Choose a banner placement to continue." : "Choose a position to continue.";
            optionPicker.Open(title, subtitle, pickGroup ? Groups() : Positions(), pickGroup ? group : position, picked =>
            {
                if (pickGroup)
                {
                    group = picked;
                    position = "";
                }
                else
                {
                    position = picked;
                }

                Refresh();
            });
        }

        // Header, tabs and selection

        private void RefreshHeader()
        {
            initSdkButton.gameObject.SetActive(!HDCAds.IsInitialized);
            if (!HDCAds.IsInitialized)
            {
                statusText.text = "HDCAds is not initialized. Start from the scene with HDCAdsSetup, or press Init SDK.";
                return;
            }

            HDCAdCoreConfig core = HDCAds.CoreConfig;
            string coreName = string.IsNullOrEmpty(HDCAds.Config.selectedAdCoreName) ? "(none)" : HDCAds.Config.selectedAdCoreName;
            statusText.text = $"SDK ready · {Platform} · ad core: {coreName} · force ad groups: {core.forceAdGroups?.Length ?? 0}"
                + $" · popup groups: {core.popupGroups?.Length ?? 0}" + (HDCAds.IsAdsRemoved ? " · ads removed" : string.Empty);
        }

        private void RefreshTabs()
        {
            foreach (ChannelTab tab in tabs)
            {
                tab.button.image.color = tab.key == channel ? SelectedTabColor : TabColor;
                if (tab.dot == null)
                    continue;
                HDCUnitTone tone = tab.key == channel
                    ? HDCAdsDebugModel.Tone(groups)
                    : HDCAdsDebugModel.Tone(HDCAdsDebugModel.Groups(tab.key, "", ""));
                tab.dot.color = ToneColor(tone);
            }
        }

        private void RefreshSelection()
        {
            channelTitleText.text = $"{Title(channel)} · {channel}";
            if (channel == "CL")
            {
                selectionText.text = "Collapsible banners are not in HDC Ads yet.";
                return;
            }

            string positionText = string.IsNullOrEmpty(position) ? "-" : position;
            string selection = Groups().Length > 0
                ? $"Group: {group} | {PositionLabel()}: {positionText}"
                : $"{PositionLabel()}: {positionText}";
            selectionText.text = $"Channel: {channel} | {Title(channel)}\n{selection}\nThe actions below call the HDCAds API directly.";
        }

        private void RefreshButtons()
        {
            bool hasGroups = Groups().Length > 0;
            SetVisible(groupButton, hasGroups);
            SetLabel(groupButton, $"Group: {(hasGroups ? group : "-")}");

            bool hasPositions = Positions().Length > 0;
            SetVisible(positionButton, hasPositions);
            SetLabel(positionButton, $"{PositionLabel()}: {(hasPositions ? position : "-")}");

            SetLabel(detailButton, channel == "FA" ? "BreakAd Debug" : "Refresh Detail");

            bool supported = channel != "CL";
            SetVisible(initButton, supported);
            SetVisible(showButton, supported && channel != "AL" && channel != "AR");
            SetLabel(showButton, channel == "BN" || channel == "MREC" ? "Activate" : "Show");
            SetVisible(hideButton, channel == "BN" || channel == "MREC" || channel == "PU");

            string primary = channel == "FA" ? "Start BreakAd" : channel == "MREC" || channel == "PU" ? "UpdatePos" : "";
            string secondary = channel == "FA" ? "Stop BreakAd" : channel == "MREC" ? "GetSize" : "";
            SetVisible(utilityPrimaryButton, primary.Length > 0);
            SetLabel(utilityPrimaryButton, primary);
            SetVisible(utilitySecondaryButton, secondary.Length > 0);
            SetLabel(utilitySecondaryButton, secondary);

            lastCallText.text = lastCall.Length > 0 ? "Last call: " + lastCall : "Last call: -";
        }

        // Ad units

        private void RefreshUnits()
        {
            int groupIndex = 0;
            int unitIndex = 0;
            if (!HDCAds.IsInitialized)
                unitsNoticeText.text = "Waiting for HDCAds initialization: the ad units come from the ad core config it runs on.";
            else if (channel == "CL")
                unitsNoticeText.text = "Collapsible banners are not in HDC Ads, so this channel has no ad units.";
            else if (groups.Count == 0)
                unitsNoticeText.text = "The ad core config has no group for this channel.";
            else
                unitsNoticeText.text = "Each card is one ad unit, in the order its group tries them. Errors show the SDK's code and what it means.";

            foreach (HDCDebugGroup debugGroup in groups)
            {
                GameObject header = Row(groupRows, groupTemplate, groupIndex++);
                header.transform.SetAsLastSibling();
                header.GetComponent<Image>().color = debugGroup.Selected ? SelectedGroupColor : GroupColor;
                SetText(header, "Title", (debugGroup.Selected && groups.Count > 1 ? "▶ " : string.Empty) + debugGroup.Name);
                SetText(header, "Info", debugGroup.Info);

                foreach (HDCDebugUnit unit in debugGroup.Units)
                {
                    GameObject row = Row(unitRows, unitTemplate, unitIndex++);
                    row.transform.SetAsLastSibling();
                    FillUnit(row, unit);
                }
            }

            Hide(groupRows, groupIndex);
            Hide(unitRows, unitIndex);
        }

        private static void FillUnit(GameObject row, HDCDebugUnit unit)
        {
            string status = unit.Status(out HDCUnitTone tone);
            Transform badge = row.transform.Find("Top/Badge");
            badge.GetComponent<Image>().color = ToneColor(tone);
            badge.GetComponentInChildren<Text>(true).text = status;
            SetText(row, "Top/Title", $"#{unit.Index} · {HDCAdsDebugModel.FormatName(unit.Format)} · {unit.Id}");
            SetText(row, "Unit", UnitLine(unit));

            string errors = ErrorLines(unit.Record);
            Transform errorText = row.transform.Find("Errors");
            errorText.gameObject.SetActive(errors.Length > 0);
            errorText.GetComponent<Text>().text = errors;
            SetText(row, "Stats", StatsLine(unit));
        }

        private static string UnitLine(HDCDebugUnit unit)
        {
            var text = new StringBuilder("Ad unit: ").Append(string.IsNullOrEmpty(unit.AdUnitId) ? "(none)" : unit.AdUnitId);
            HDCAdRecord record = unit.Record;
            if (!string.IsNullOrEmpty(record?.AdSource))
                text.Append(" · source: ").Append(record.AdSource);
            if (!string.IsNullOrEmpty(record?.Adapter))
                text.Append(" · adapter: ").Append(record.Adapter.Substring(record.Adapter.LastIndexOf('.') + 1));
            if (!string.IsNullOrEmpty(record?.Layout))
                text.Append(" · layout: ").Append(record.Layout);
            if (!string.IsNullOrEmpty(unit.NativeState))
                text.Append(" · native state: ").Append(unit.NativeState);
            return text.ToString();
        }

        private static string ErrorLines(HDCAdRecord record)
        {
            if (record == null)
                return string.Empty;
            var text = new StringBuilder();
            AppendError(text, "Load error", record.LastLoadError, false);
            AppendError(text, "Show error", record.LastShowError, true);
            return text.ToString().TrimEnd();
        }

        private static void AppendError(StringBuilder text, string label, HDCAdError error, bool show)
        {
            if (error == null)
                return;
            text.Append("<color=#F87171><b>").Append(label).Append(' ').Append(HDCAdErrorGuide.Title(error, show)).Append("</b></color>")
                .Append("  <color=#94A3B8>").Append(error.Clock.ToString("HH:mm:ss", CultureInfo.InvariantCulture))
                .Append(string.IsNullOrEmpty(error.AdUnitId) ? string.Empty : " · " + error.AdUnitId).AppendLine("</color>")
                .AppendLine(HDCAdErrorGuide.Explain(error, show));
            if (!string.IsNullOrEmpty(error.Message))
                text.Append("<color=#94A3B8>Message: ").Append(error.Message).AppendLine("</color>");
            text.AppendLine();
        }

        private static string StatsLine(HDCDebugUnit unit)
        {
            HDCAdRecord record = unit.Record;
            if (record == null)
                return unit.Created
                    ? unit.Started ? "No load seen yet." : "Starts only after the units before it fail."
                    : "Not created yet: press Init, or show a position of this group.";

            var text = new StringBuilder()
                .Append("Requests ").Append(record.Requests)
                .Append(" · loaded ").Append(record.Loads)
                .Append(" · load failed ").Append(record.LoadFailures)
                .Append(" · shows ").Append(record.Shows)
                .Append(" · show failed ").Append(record.ShowFailures)
                .Append(" · impressions ").Append(record.Impressions)
                .Append(" · clicks ").Append(record.Clicks);
            if (record.LoadSeconds >= 0f)
                text.Append(" · last load took ").Append(record.LoadSeconds.ToString("0.0", CultureInfo.InvariantCulture)).Append('s');
            if (record.Revenue > 0d)
                text.Append(" · revenue ").Append(record.Revenue.ToString("0.######", CultureInfo.InvariantCulture)).Append(' ').Append(record.Currency);
            float retryIn = record.RetryAt - Time.realtimeSinceStartup;
            if (retryIn > 0f)
                text.Append(" · retry #").Append(record.RetryAttempt).Append(" in ").Append(Mathf.CeilToInt(retryIn)).Append('s');
            text.Append(" · updated ").Append(record.UpdatedClock.ToString("HH:mm:ss", CultureInfo.InvariantCulture));
            return text.ToString();
        }

        // Events

        private void RefreshEvents()
        {
            HashSet<string> ids = HDCAdsDebugModel.InstanceIds(groups);
            var picked = new List<HDCTrackedEvent>();
            IReadOnlyList<HDCTrackedEvent> events = HDCAdsTracker.Events;
            for (int i = events.Count - 1; i >= 0 && picked.Count < EventRows; i--)
            {
                if (ids.Contains(events[i].Event.id ?? string.Empty))
                    picked.Add(events[i]);
            }

            eventsNoticeText.text = picked.Count == 0
                ? "No event yet for this channel's ad units."
                : $"The latest {picked.Count} events of this channel's ad units, newest first.";
            for (int i = 0; i < picked.Count; i++)
            {
                GameObject row = Row(eventRows, eventTemplate, i);
                row.transform.SetAsLastSibling();
                row.GetComponent<Text>().text = EventLine(picked[i]);
            }

            Hide(eventRows, picked.Count);
        }

        private static string EventLine(HDCTrackedEvent tracked)
        {
            HDCAdEvent adEvent = tracked.Event;
            string color = adEvent.type == HDCAdEventType.LoadFailed || adEvent.type == HDCAdEventType.ShowFailed ? "#F87171"
                : adEvent.type == HDCAdEventType.Loaded ? "#4ADE80"
                : adEvent.type == HDCAdEventType.Shown || adEvent.type == HDCAdEventType.Impression ? "#60A5FA"
                : "#CBD5E1";
            var text = new StringBuilder()
                .Append("<color=#94A3B8>").Append(tracked.Clock.ToString("HH:mm:ss", CultureInfo.InvariantCulture)).Append("</color>  ")
                .Append("<color=").Append(color).Append("><b>").Append(adEvent.type).Append("</b></color>  ")
                .Append(adEvent.id);
            if (!string.IsNullOrEmpty(adEvent.message))
                text.Append("  <color=#F87171>").Append(adEvent.code).Append(' ').Append(adEvent.message).Append("</color>");
            else if (adEvent.type == HDCAdEventType.Paid)
                text.Append("  ").Append(adEvent.Revenue.ToString("0.######", CultureInfo.InvariantCulture)).Append(' ').Append(adEvent.currency);
            else if (!string.IsNullOrEmpty(adEvent.adSource))
                text.Append("  <color=#94A3B8>").Append(adEvent.adSource).Append("</color>");
            return text.ToString();
        }

        // System: what each channel offers, then the selected channel's configs, runtime state and gates.

        private void RefreshSystem()
        {
            SetLabel(systemToggleButton, systemBody.activeSelf ? "Hide" : "Show");
            if (!systemBody.activeSelf)
                return;
            summaryText.text = Summary();
            systemText.text = SystemText();
        }

        private static string Summary()
        {
            if (!HDCAds.IsInitialized)
                return "HDCAds is not initialized yet.";
            return string.Join("\n", new[]
            {
                SummaryLine("AL", HDCAds.Config.appLaunchChannel?.isEnabled, "AppLaunch.Initialize()"),
                SummaryLine("AR", HDCAds.Config.appResumeChannel?.isEnabled, "AppResume.Initialize(), AppResume.Block()"),
                SummaryLine("RW", HDCAds.Config.rewardedChannel?.isEnabled, "Rewarded.Initialize(), Rewarded.Show(pos)"),
                SummaryLine("FA", HDCAds.Config.forceAdChannel?.isEnabled,
                    "ForceAd.Initialize(group), ForceAd.Show(pos) | Utility: ForceAd.StartBreakAd(), ForceAd.StopBreakAd()"),
                SummaryLine("BN", HDCAds.Config.bannerChannel?.isEnabled, "Banner.Initialize(slot), Banner.Show(slot), Banner.Hide(slot)"),
                SummaryLine("MREC", HDCAds.Config.mrecChannel?.isEnabled,
                    "Mrec.Initialize(), Mrec.Show(), Mrec.Hide() | Utility: Mrec.Move(pos), Mrec.SizeInPixels"),
                "CL | Collapsible | not in HDC Ads",
                SummaryLine("PU", HDCAds.Config.popupChannel?.isEnabled,
                    "Popup.Initialize(group), Popup.Show(pos), Popup.Hide(pos) | Utility: Popup.Move(pos, area)"),
            });
        }

        private static string SummaryLine(string key, bool? enabled, string api) =>
            $"{key} | {Title(key)} | config {(enabled == true ? "on" : "off")} | API: {api}";

        private string SystemText()
        {
            if (!HDCAds.IsInitialized)
                return "Waiting for HDCAds initialization...\n\nThe channel details are available once the ads configs are applied.";

            switch (channel)
            {
                case "AL":
                    return Section("System", HDCAds.AppLaunch.Describe());
                case "AR":
                    return Section("System", HDCAds.AppResume.Describe());
                case "RW":
                    return Section("System", HDCAds.Rewarded.Describe());
                case "FA":
                    return Section("System", HDCAds.ForceAd.Describe()) + "\n\n" + Section("Selected Group", string.IsNullOrEmpty(group)
                        ? "(no group selected)"
                        : HDCAds.ExistingForceAdGroup(group)?.Describe() ?? "(not created yet: press Init)");
                case "BN":
                    return Section("Selected Placement", HDCAds.Banner.Describe(Slot()));
                case "MREC":
                    return Section("System", HDCAds.Mrec.Describe());
                case "PU":
                    return Section("Selected Group", HDCAds.Popup.Describe(group));
                default:
                    return Section("System", "Collapsible banners are not in HDC Ads yet.");
            }
        }

        private void CopyReport()
        {
            var text = new StringBuilder()
                .Append("HDC Ads debug report · ").AppendLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture))
                .AppendLine(StripTags(statusText.text))
                .AppendLine(selectionText.text)
                .AppendLine();
            foreach (HDCDebugGroup debugGroup in groups)
            {
                text.Append("== ").Append(debugGroup.Name).Append(" · ").AppendLine(debugGroup.Info);
                foreach (HDCDebugUnit unit in debugGroup.Units)
                {
                    text.Append("#").Append(unit.Index).Append(' ').Append(unit.Status(out _)).Append(" · ")
                        .Append(HDCAdsDebugModel.FormatName(unit.Format)).Append(" · ").AppendLine(unit.Id)
                        .AppendLine(UnitLine(unit));
                    string errors = StripTags(ErrorLines(unit.Record));
                    if (errors.Length > 0)
                        text.AppendLine(errors);
                    text.AppendLine(StatsLine(unit)).AppendLine();
                }
            }

            text.AppendLine("== Events");
            foreach (GameObject row in eventRows.Where(row => row.activeSelf))
                text.AppendLine(StripTags(row.GetComponent<Text>().text));
            text.AppendLine().AppendLine(Summary()).AppendLine().AppendLine(SystemText());

            GUIUtility.systemCopyBuffer = text.ToString();
            Record("debug report copied to the clipboard");
            Refresh();
        }

        // Actions, as in the channel workspace

        private void InitializeSdk()
        {
            if (!HDCAds.IsInitialized)
            {
                HDCAdsSetup.InitializeAds();
                Record("HDCAdsSetup.InitializeAds()");
            }

            Refresh();
        }

        private void OnDetailClicked()
        {
            if (channel == "FA" && HDCAds.IsInitialized)
            {
                viewer.Open("ForceAd | BreakAd Debug", () => HDCAds.ForceAd.DescribeBreakAd());
                return;
            }

            Refresh();
        }

        private void Init()
        {
            switch (channel)
            {
                case "AL":
                    HDCAds.AppLaunch.Initialize();
                    Record("AppLaunch.Initialize()");
                    break;
                case "AR":
                    HDCAds.AppResume.Initialize();
                    Record("AppResume.Initialize()");
                    break;
                case "RW":
                    HDCAds.Rewarded.Initialize();
                    Record("Rewarded.Initialize()");
                    break;
                case "FA":
                    if (string.IsNullOrEmpty(group))
                        break;
                    HDCAds.ForceAd.Initialize(group);
                    Record($"ForceAd.Initialize(\"{group}\")");
                    break;
                case "BN":
                    HDCAds.Banner.Initialize(Slot());
                    Record($"Banner.Initialize({Slot()})");
                    break;
                case "MREC":
                    HDCAds.Mrec.Initialize();
                    Record("Mrec.Initialize()");
                    break;
                case "PU":
                    if (string.IsNullOrEmpty(group))
                        break;
                    HDCAds.Popup.Initialize(group);
                    Record($"Popup.Initialize(\"{group}\")");
                    break;
            }

            Refresh();
        }

        private void Show()
        {
            switch (channel)
            {
                case "RW":
                    string rewardedAt = string.IsNullOrEmpty(position) ? RewardedPosition : position;
                    bool rewarded = HDCAds.Rewarded.Show(rewardedAt, () => Record("Rewarded: reward earned"), () => Record("Rewarded: closed"));
                    Record($"Rewarded.Show(\"{rewardedAt}\") -> {rewarded}");
                    break;
                case "FA":
                    if (string.IsNullOrEmpty(position))
                        break;
                    string shownAt = position;
                    bool shown = HDCAds.ForceAd.Show(shownAt, () => Record($"ForceAd \"{shownAt}\": done"));
                    Record($"ForceAd.Show(\"{shownAt}\") -> {shown}");
                    break;
                case "BN":
                    Record($"Banner.Show({Slot()}) -> {HDCAds.Banner.Show(Slot())}");
                    break;
                case "MREC":
                    Record($"Mrec.Show() -> {HDCAds.Mrec.Show()}");
                    break;
                case "PU":
                    if (string.IsNullOrEmpty(position))
                        break;
                    PlacePopup();
                    Record($"Popup.Show(\"{position}\") -> {HDCAds.Popup.Show(position)}");
                    break;
            }

            Refresh();
        }

        private void Hide()
        {
            switch (channel)
            {
                case "BN":
                    HDCAds.Banner.Hide(Slot());
                    Record($"Banner.Hide({Slot()})");
                    break;
                case "MREC":
                    HDCAds.Mrec.Hide();
                    Record("Mrec.Hide()");
                    break;
                case "PU":
                    if (string.IsNullOrEmpty(position))
                        break;
                    HDCAds.Popup.Hide(position);
                    Record($"Popup.Hide(\"{position}\")");
                    break;
            }

            Refresh();
        }

        private void UtilityPrimary()
        {
            switch (channel)
            {
                case "FA":
                    HDCAds.ForceAd.StartBreakAd();
                    Record("ForceAd.StartBreakAd()");
                    break;
                case "MREC":
                    if (Enum.TryParse(position, out HDCAdPosition mrecPosition))
                    {
                        HDCAds.Mrec.Move(mrecPosition);
                        Record($"Mrec.Move({mrecPosition})");
                    }

                    break;
                case "PU":
                    PlacePopup();
                    break;
            }

            Refresh();
        }

        private void UtilitySecondary()
        {
            switch (channel)
            {
                case "FA":
                    HDCAds.ForceAd.StopBreakAd();
                    Record("ForceAd.StopBreakAd()");
                    break;
                case "MREC":
                    Record($"Mrec.SizeInPixels -> {HDCAds.Mrec.SizeInPixels}");
                    break;
            }

            Refresh();
        }

        private void PlacePopup()
        {
            if (popupArea == null || string.IsNullOrEmpty(position))
                return;
            ShowPopupArea(true);
            HDCAds.Popup.Move(position, popupArea);
            Record($"Popup.Move(\"{position}\", popup area)");
        }

        // Helpers

        private void Record(string call)
        {
            lastCall = $"[{DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture)}] {call}";
            dirty = true;
        }

        private HDCBannerSlot Slot() => Enum.TryParse(position, out HDCBannerSlot slot) ? slot : HDCBannerSlot.FullBottom;

        private string PositionLabel() => channel == "BN" ? "Placement" : "Position";

        private void ShowPopupArea(bool visible)
        {
            if (popupArea != null && popupArea.gameObject.activeSelf != visible)
                popupArea.gameObject.SetActive(visible);
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

        private static string Title(string key)
        {
            switch (key)
            {
                case "AL": return "AppLaunch";
                case "AR": return "AppResume";
                case "RW": return "Rewarded";
                case "FA": return "ForceAd";
                case "BN": return "Banner";
                case "MREC": return "Mrec";
                case "CL": return "Collapsible";
                default: return "Popup";
            }
        }

        private static Color ToneColor(HDCUnitTone tone)
        {
            switch (tone)
            {
                case HDCUnitTone.Busy: return BusyColor;
                case HDCUnitTone.Good: return GoodColor;
                case HDCUnitTone.Live: return LiveColor;
                case HDCUnitTone.Bad: return BadColor;
                default: return IdleColor;
            }
        }

        private static string Section(string name, string body) =>
            $"--- {name} ---\n{(string.IsNullOrEmpty(body) ? "(empty)" : body)}";

        private static string[] Distinct(IEnumerable<string> values) =>
            values.Where(value => !string.IsNullOrEmpty(value)).Distinct().ToArray();

        private static string StripTags(string text) => System.Text.RegularExpressions.Regex.Replace(text ?? string.Empty, "<.*?>", string.Empty);

        private static GameObject Row(List<GameObject> pool, GameObject template, int index)
        {
            while (pool.Count <= index)
            {
                GameObject row = Instantiate(template, template.transform.parent, false);
                row.name = template.name.Replace(" Template", string.Empty) + " " + pool.Count;
                pool.Add(row);
            }

            GameObject picked = pool[index];
            if (!picked.activeSelf)
                picked.SetActive(true);
            return picked;
        }

        private static void Hide(List<GameObject> pool, int used)
        {
            for (int i = used; i < pool.Count; i++)
            {
                if (pool[i].activeSelf)
                    pool[i].SetActive(false);
            }
        }

        private static void SetText(GameObject row, string path, string value) => row.transform.Find(path).GetComponent<Text>().text = value;

        private static void SetVisible(Button button, bool visible)
        {
            if (button.gameObject.activeSelf != visible)
                button.gameObject.SetActive(visible);
        }

        private static void SetLabel(Button button, string label) => button.GetComponentInChildren<Text>(true).text = label;
    }
}
