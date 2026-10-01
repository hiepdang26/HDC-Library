using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace HDC.Ads.DebugUI
{
    /// <summary>
    /// The ad systems workspace of the debug panel: pick a channel, pick a group and a position from the ads
    /// configs fetched from Remote Config, then call the HDCAds API with the action buttons. The detail area
    /// shows the channel's configs and state, refreshed every second.
    /// </summary>
    public sealed class HDCAdSystemsSection : MonoBehaviour
    {
        private const string RewardedPosition = "debug_rw";

        private static readonly Color ChannelColor = new Color(0.31f, 0.31f, 0.31f, 1f);
        private static readonly Color SelectedChannelColor = new Color(0.86f, 0.53f, 0.08f, 1f);
        private static readonly string[] MrecPositions = { "TopLeft", "Top", "TopRight", "Center", "BottomLeft", "Bottom", "BottomRight" };
        private static readonly string[] BannerSlots = Enum.GetNames(typeof(HDCBannerSlot));

        [Header("Channels")]
        [SerializeField] private Button appLaunchButton;
        [SerializeField] private Button appResumeButton;
        [SerializeField] private Button rewardedButton;
        [SerializeField] private Button forceAdButton;
        [SerializeField] private Button bannerButton;
        [SerializeField] private Button mrecButton;
        [SerializeField] private Button collapsibleButton;
        [SerializeField] private Button popupButton;

        [Header("Texts")]
        [SerializeField] private Text summaryText;
        [SerializeField] private Text channelTitleText;
        [SerializeField] private Text selectionText;
        [SerializeField] private Text detailText;

        [Header("Actions")]
        [SerializeField] private Button refreshButton;
        [SerializeField] private Button groupButton;
        [SerializeField] private Button positionButton;
        [SerializeField] private Button detailButton;
        [SerializeField] private Button initButton;
        [SerializeField] private Button showButton;
        [SerializeField] private Button hideButton;
        [SerializeField] private Button utilityPrimaryButton;
        [SerializeField] private Button utilitySecondaryButton;

        [Header("Helpers")]
        [SerializeField] private HDCOptionPicker optionPicker;
        [Tooltip("Where popups show. Visible while the popup channel is selected.")]
        [SerializeField] private RectTransform popupArea;
        [Tooltip("Seconds between refreshes of the detail area.")]
        [SerializeField] private float refreshSeconds = 1f;

        private readonly Dictionary<Channel, Button> channelButtons = new Dictionary<Channel, Button>();
        private Channel channel = Channel.FA;
        private string group = "";
        private string position = "";
        private string lastCall = "";
        private string extraDetail = "";
        private float nextRefresh;

        private enum Channel
        {
            AL,
            AR,
            RW,
            FA,
            BN,
            MREC,
            CL,
            PU,
        }

        private void Awake()
        {
            BindChannel(appLaunchButton, Channel.AL);
            BindChannel(appResumeButton, Channel.AR);
            BindChannel(rewardedButton, Channel.RW);
            BindChannel(forceAdButton, Channel.FA);
            BindChannel(bannerButton, Channel.BN);
            BindChannel(mrecButton, Channel.MREC);
            BindChannel(collapsibleButton, Channel.CL);
            BindChannel(popupButton, Channel.PU);

            refreshButton.onClick.AddListener(OnRefreshClicked);
            groupButton.onClick.AddListener(() => OpenPicker(true));
            positionButton.onClick.AddListener(() => OpenPicker(false));
            detailButton.onClick.AddListener(OnDetailClicked);
            initButton.onClick.AddListener(Init);
            showButton.onClick.AddListener(Show);
            hideButton.onClick.AddListener(Hide);
            utilityPrimaryButton.onClick.AddListener(UtilityPrimary);
            utilitySecondaryButton.onClick.AddListener(UtilitySecondary);

            HDCAds.Initialized += Refresh;
        }

        private void OnDestroy() => HDCAds.Initialized -= Refresh;

        private void OnEnable() => Refresh();

        private void OnDisable() => ShowPopupArea(false);

        private void Update()
        {
            if (Time.unscaledTime < nextRefresh)
                return;
            nextRefresh = Time.unscaledTime + Mathf.Max(0.2f, refreshSeconds);
            Refresh();
        }

        /// <summary>Redraws the summary, the selection, the buttons and the detail area.</summary>
        public void Refresh()
        {
            NormalizeSelection();
            summaryText.text = Summary();
            HighlightChannel();
            UpdateSelection();
            UpdateButtons();
            detailText.text = Detail();
            ShowPopupArea(channel == Channel.PU && isActiveAndEnabled);
        }

        private void BindChannel(Button button, Channel value)
        {
            channelButtons[value] = button;
            button.onClick.AddListener(() =>
            {
                channel = value;
                extraDetail = "";
                Refresh();
            });
        }

        // Selection

        private void NormalizeSelection()
        {
            string[] groups = Groups();
            group = groups.Length == 0 ? "" : Array.IndexOf(groups, group) >= 0 ? group : groups[0];
            string[] positions = Positions();
            position = positions.Length == 0 ? "" : Array.IndexOf(positions, position) >= 0 ? position : positions[0];
        }

        private string[] Groups()
        {
            if (!HDCAds.IsInitialized)
                return new string[0];

            switch (channel)
            {
                case Channel.FA:
                    return Distinct((HDCAds.CoreConfig.forceAdGroups ?? new HDCAdCoreConfig.ForceAdGroup[0]).Select(g => g?.groupName));
                case Channel.PU:
                    return Distinct((HDCAds.CoreConfig.popupGroups ?? new HDCAdCoreConfig.PopupGroup[0]).Select(g => g?.groupName));
                default:
                    return new string[0];
            }
        }

        private string[] Positions()
        {
            switch (channel)
            {
                case Channel.FA:
                    if (!HDCAds.IsInitialized)
                        return new string[0];
                    return Distinct((HDCAds.Config.forceAdChannel?.positionConfigs ?? new HDCAdsConfig.ForceAdPosition[0])
                        .Select(p => p?.positionName)
                        .Where(p => string.IsNullOrEmpty(group) || HDCAds.CoreConfig.ForceAdGroupAt(p) == group));
                case Channel.PU:
                    if (!HDCAds.IsInitialized)
                        return new string[0];
                    return Distinct((HDCAds.Config.popupChannel?.positionConfigs ?? new HDCAdsConfig.PopupPosition[0])
                        .Select(p => p?.positionName)
                        .Where(p => string.IsNullOrEmpty(group) || HDCAds.CoreConfig.PopupGroupAt(p) == group));
                case Channel.RW:
                    return new[] { RewardedPosition };
                case Channel.BN:
                    return BannerSlots;
                case Channel.MREC:
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
                : channel == Channel.BN ? "Choose a banner placement to continue." : "Choose a position to continue.";
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

        // Display

        private string Summary()
        {
            if (!HDCAds.IsInitialized)
                return "HDCAds is not initialized yet. Start the game from the scene with HDCAdsSetup, or press Init SDK.";

            HDCAdCoreConfig core = HDCAds.CoreConfig;
            string coreName = string.IsNullOrEmpty(HDCAds.Config.selectedAdCoreName) ? "(none)" : HDCAds.Config.selectedAdCoreName;
            return string.Join("\n", new[]
            {
                $"Ad core: {coreName} | force ad groups: {core.forceAdGroups?.Length ?? 0} | popup groups: {core.popupGroups?.Length ?? 0}",
                SummaryLine(Channel.AL, "AppLaunch", HDCAds.Config.appLaunchChannel?.isEnabled, "AppLaunch.Initialize()"),
                SummaryLine(Channel.AR, "AppResume", HDCAds.Config.appResumeChannel?.isEnabled, "AppResume.Initialize(), AppResume.Block()"),
                SummaryLine(Channel.RW, "Rewarded", HDCAds.Config.rewardedChannel?.isEnabled, "Rewarded.Initialize(), Rewarded.Show(pos)"),
                SummaryLine(Channel.FA, "ForceAd", HDCAds.Config.forceAdChannel?.isEnabled,
                    "ForceAd.Initialize(group), ForceAd.Show(pos) | Utility: ForceAd.StartBreakAd(), ForceAd.StopBreakAd()"),
                SummaryLine(Channel.BN, "Banner", HDCAds.Config.bannerChannel?.isEnabled, "Banner.Initialize(slot), Banner.Show(slot), Banner.Hide(slot)"),
                SummaryLine(Channel.MREC, "Mrec", HDCAds.Config.mrecChannel?.isEnabled,
                    "Mrec.Initialize(), Mrec.Show(), Mrec.Hide() | Utility: Mrec.Move(pos), Mrec.SizeInPixels"),
                "CL | Collapsible | not in HDC Ads",
                SummaryLine(Channel.PU, "Popup", HDCAds.Config.popupChannel?.isEnabled,
                    "Popup.Initialize(group), Popup.Show(pos), Popup.Hide(pos) | Utility: Popup.Move(pos, area)"),
            });
        }

        private static string SummaryLine(Channel key, string title, bool? enabled, string api) =>
            $"{key} | {title} | {(enabled == true ? "on" : "off")} | API: {api}";

        private void HighlightChannel()
        {
            foreach (KeyValuePair<Channel, Button> item in channelButtons)
                item.Value.image.color = item.Key == channel ? SelectedChannelColor : ChannelColor;
        }

        private void UpdateSelection()
        {
            channelTitleText.text = $"Selected channel: {channel}";
            string positionText = string.IsNullOrEmpty(position) ? "-" : position;
            string selection = Groups().Length > 0
                ? $"Group: {group} | {PositionLabel()}: {positionText}"
                : $"{PositionLabel()}: {positionText}";
            selectionText.text = $"Channel: {channel} | {Title(channel)}\n{selection}\nThe actions below call the HDCAds API directly.";
        }

        private void UpdateButtons()
        {
            SetLabel(refreshButton, HDCAds.IsInitialized ? "Refresh" : "Init SDK");

            bool hasGroups = Groups().Length > 0;
            SetVisible(groupButton, hasGroups);
            SetLabel(groupButton, $"Group: {(hasGroups ? group : "-")}");

            bool hasPositions = Positions().Length > 0;
            SetVisible(positionButton, hasPositions);
            SetLabel(positionButton, $"{PositionLabel()}: {(hasPositions ? position : "-")}");

            SetLabel(detailButton, channel == Channel.FA ? "BreakAd Debug" : "Refresh Detail");

            bool supported = channel != Channel.CL;
            SetVisible(initButton, supported);
            SetVisible(showButton, supported && channel != Channel.AL && channel != Channel.AR);
            SetLabel(showButton, channel == Channel.BN || channel == Channel.MREC ? "Activate" : "Show");
            SetVisible(hideButton, channel == Channel.BN || channel == Channel.MREC || channel == Channel.PU);

            string primary = channel == Channel.FA ? "Start BreakAd" : channel == Channel.MREC || channel == Channel.PU ? "UpdatePos" : "";
            string secondary = channel == Channel.FA ? "Stop BreakAd" : channel == Channel.MREC ? "GetSize" : "";
            SetVisible(utilityPrimaryButton, primary.Length > 0);
            SetLabel(utilityPrimaryButton, primary);
            SetVisible(utilitySecondaryButton, secondary.Length > 0);
            SetLabel(utilitySecondaryButton, secondary);
        }

        private string Detail()
        {
            var sections = new List<string>();
            if (lastCall.Length > 0)
                sections.Add(Section("Last call", lastCall));
            if (extraDetail.Length > 0)
                sections.Add(extraDetail);

            if (!HDCAds.IsInitialized)
            {
                sections.Add("Waiting for HDCAds initialization...\n\nThe channel details are available once the ads configs are applied.");
                return string.Join("\n\n", sections);
            }

            switch (channel)
            {
                case Channel.AL:
                    sections.Add(Section("System", HDCAds.AppLaunch.Describe()));
                    break;
                case Channel.AR:
                    sections.Add(Section("System", HDCAds.AppResume.Describe()));
                    break;
                case Channel.RW:
                    sections.Add(Section("System", HDCAds.Rewarded.Describe()));
                    break;
                case Channel.FA:
                    sections.Add(Section("System", HDCAds.ForceAd.Describe()));
                    sections.Add(Section("Selected Group", string.IsNullOrEmpty(group)
                        ? "(no group selected)"
                        : HDCAds.ForceAdGroup(group)?.Describe() ?? "(no unit in this group)"));
                    break;
                case Channel.BN:
                    sections.Add(Section("Selected Placement", HDCAds.Banner.Describe(Slot())));
                    break;
                case Channel.MREC:
                    sections.Add(Section("System", HDCAds.Mrec.Describe()));
                    break;
                case Channel.CL:
                    sections.Add(Section("System", "Collapsible banners are not in HDC Ads yet."));
                    break;
                case Channel.PU:
                    sections.Add(Section("Selected Group", HDCAds.Popup.Describe(group)));
                    break;
            }

            return string.Join("\n\n", sections);
        }

        // Actions

        private void OnRefreshClicked()
        {
            // For testing a scene on its own: the setup of the first scene normally initializes the ads.
            if (!HDCAds.IsInitialized)
            {
                HDCAdsSetup.InitializeAds();
                Record("Init SDK: HDCAdsSetup.InitializeAds()");
            }

            Refresh();
        }

        private void OnDetailClicked()
        {
            extraDetail = channel == Channel.FA && HDCAds.IsInitialized ? Section("BreakAd Debug", HDCAds.ForceAd.DescribeBreakAd()) : "";
            Refresh();
        }

        private void Init()
        {
            switch (channel)
            {
                case Channel.AL:
                    HDCAds.AppLaunch.Initialize();
                    Record("AppLaunch.Initialize()");
                    break;
                case Channel.AR:
                    HDCAds.AppResume.Initialize();
                    Record("AppResume.Initialize()");
                    break;
                case Channel.RW:
                    HDCAds.Rewarded.Initialize();
                    Record("Rewarded.Initialize()");
                    break;
                case Channel.FA:
                    HDCAds.ForceAd.Initialize(group);
                    Record($"ForceAd.Initialize(\"{group}\")");
                    break;
                case Channel.BN:
                    HDCAds.Banner.Initialize(Slot());
                    Record($"Banner.Initialize({Slot()})");
                    break;
                case Channel.MREC:
                    HDCAds.Mrec.Initialize();
                    Record("Mrec.Initialize()");
                    break;
                case Channel.PU:
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
                case Channel.RW:
                    string rewardedAt = string.IsNullOrEmpty(position) ? RewardedPosition : position;
                    bool rewarded = HDCAds.Rewarded.Show(rewardedAt, () => Record("Rewarded: reward earned"), () => Record("Rewarded: closed"));
                    Record($"Rewarded.Show(\"{rewardedAt}\") -> {rewarded}");
                    break;
                case Channel.FA:
                    bool shown = HDCAds.ForceAd.Show(position, () => Record($"ForceAd \"{position}\": done"));
                    Record($"ForceAd.Show(\"{position}\") -> {shown}");
                    break;
                case Channel.BN:
                    Record($"Banner.Show({Slot()}) -> {HDCAds.Banner.Show(Slot())}");
                    break;
                case Channel.MREC:
                    Record($"Mrec.Show() -> {HDCAds.Mrec.Show()}");
                    break;
                case Channel.PU:
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
                case Channel.BN:
                    HDCAds.Banner.Hide(Slot());
                    Record($"Banner.Hide({Slot()})");
                    break;
                case Channel.MREC:
                    HDCAds.Mrec.Hide();
                    Record("Mrec.Hide()");
                    break;
                case Channel.PU:
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
                case Channel.FA:
                    HDCAds.ForceAd.StartBreakAd();
                    Record("ForceAd.StartBreakAd()");
                    break;
                case Channel.MREC:
                    if (Enum.TryParse(position, out HDCAdPosition mrecPosition))
                    {
                        HDCAds.Mrec.Move(mrecPosition);
                        Record($"Mrec.Move({mrecPosition})");
                    }

                    break;
                case Channel.PU:
                    PlacePopup();
                    break;
            }

            Refresh();
        }

        private void UtilitySecondary()
        {
            switch (channel)
            {
                case Channel.FA:
                    HDCAds.ForceAd.StopBreakAd();
                    Record("ForceAd.StopBreakAd()");
                    break;
                case Channel.MREC:
                    Record($"Mrec.SizeInPixels -> {HDCAds.Mrec.SizeInPixels}");
                    break;
            }

            Refresh();
        }

        private void PlacePopup()
        {
            if (popupArea == null || string.IsNullOrEmpty(position))
                return;
            HDCAds.Popup.Move(position, popupArea);
            Record($"Popup.Move(\"{position}\", popup area)");
        }

        // Helpers

        private void Record(string call) => lastCall = $"[{DateTime.Now:HH:mm:ss}] {call}";

        private HDCBannerSlot Slot() => Enum.TryParse(position, out HDCBannerSlot slot) ? slot : HDCBannerSlot.FullBottom;

        private string PositionLabel() => channel == Channel.BN ? "Placement" : "Position";

        private void ShowPopupArea(bool visible)
        {
            if (popupArea != null && popupArea.gameObject.activeSelf != visible)
                popupArea.gameObject.SetActive(visible);
        }

        private static string Title(Channel value)
        {
            switch (value)
            {
                case Channel.AL: return "AppLaunch";
                case Channel.AR: return "AppResume";
                case Channel.RW: return "Rewarded";
                case Channel.FA: return "ForceAd";
                case Channel.BN: return "Banner";
                case Channel.MREC: return "Mrec";
                case Channel.CL: return "Collapsible";
                default: return "Popup";
            }
        }

        private static string Section(string name, string body) =>
            $"--- {name} ---\n{(string.IsNullOrEmpty(body) ? "(empty)" : body)}";

        private static string[] Distinct(IEnumerable<string> values) =>
            values.Where(value => !string.IsNullOrEmpty(value)).Distinct().ToArray();

        private static void SetVisible(Button button, bool visible)
        {
            if (button.gameObject.activeSelf != visible)
                button.gameObject.SetActive(visible);
        }

        private static void SetLabel(Button button, string label) =>
            button.GetComponentInChildren<Text>(true).text = label;
    }
}
