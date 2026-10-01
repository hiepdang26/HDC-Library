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
    /// The ads page of the debug panel. Pick a channel, then a group and a position from the configs HDCAds runs
    /// on, and call the HDCAds API with the action buttons. Detail Information shows only the selected group: its
    /// settings, then each of its ad units with its state, its last errors explained, and its counts. The group's
    /// recent events and the channel's system state open on demand. Everything refreshes every second and on
    /// every ad event.
    /// </summary>
    public sealed class HDCAdsDebugWorkspace : HDCDebugPage
    {
        private const string RewardedPosition = "debug_rw";
        private const int EventRows = 15;
        private const int FullScreenEvents = 40;
        private const float CopiedSeconds = 1.5f;

        private static readonly string[] MrecPositions = { "TopLeft", "Top", "TopRight", "Center", "BottomLeft", "Bottom", "BottomRight" };

        [Serializable]
        private sealed class ChannelTab
        {
            public string key;
            public Button button;
            public Image dot;
        }

        [Header("Channels")]
        [SerializeField] private ChannelTab[] tabs = new ChannelTab[0];

        [Header("Actions")]
        [SerializeField] private Text channelTitleText;
        [SerializeField] private Text selectionHintText;
        [SerializeField] private Button groupButton;
        [SerializeField] private Button positionButton;
        [SerializeField] private Button initButton;
        [SerializeField] private Button showButton;
        [SerializeField] private Button hideButton;
        [SerializeField] private Button utilityPrimaryButton;
        [SerializeField] private Button utilitySecondaryButton;
        [SerializeField] private Text lastCallText;

        [Header("Detail information")]
        [SerializeField] private Text groupTitleText;
        [SerializeField] private HDCKeyValueList groupList;
        [SerializeField] private Text unitsNoticeText;
        [SerializeField] private GameObject unitTemplate;
        [SerializeField] private Button copyReportButton;

        [Header("Recent events")]
        [SerializeField] private Button eventsToggleButton;
        [SerializeField] private Button eventsFullScreenButton;
        [SerializeField] private GameObject eventsBody;
        [SerializeField] private Text eventsNoticeText;
        [SerializeField] private GameObject eventTemplate;

        [Header("System")]
        [SerializeField] private Text systemTitleText;
        [SerializeField] private Button systemToggleButton;
        [SerializeField] private GameObject systemBody;
        [SerializeField] private HDCKeyValueList systemList;

        [Header("Helpers")]
        [SerializeField] private HDCOptionPicker optionPicker;
        [SerializeField] private HDCAdsDebugViewer viewer;
        [Tooltip("Where popups show. Visible while the popup channel is selected.")]
        [SerializeField] private RectTransform popupArea;

        private readonly List<GameObject> unitRows = new List<GameObject>();
        private readonly List<GameObject> eventRows = new List<GameObject>();
        private string channel = "FA";
        private string group = "";
        private string position = "";
        private string lastCall = "";
        private float copiedUntil;
        private List<HDCDebugGroup> groups = new List<HDCDebugGroup>();
        private HDCDebugGroup selected;

        private void Awake()
        {
            unitTemplate.SetActive(false);
            eventTemplate.SetActive(false);
            eventsBody.SetActive(false);
            systemBody.SetActive(false);

            foreach (ChannelTab tab in tabs)
            {
                string key = tab.key;
                tab.button.onClick.AddListener(() =>
                {
                    channel = key;
                    Refresh();
                });
            }

            groupButton.onClick.AddListener(() => OpenPicker(true));
            positionButton.onClick.AddListener(() => OpenPicker(false));
            initButton.onClick.AddListener(Init);
            showButton.onClick.AddListener(Show);
            hideButton.onClick.AddListener(Hide);
            utilityPrimaryButton.onClick.AddListener(UtilityPrimary);
            utilitySecondaryButton.onClick.AddListener(UtilitySecondary);
            copyReportButton.onClick.AddListener(CopyReport);
            eventsToggleButton.onClick.AddListener(() => Toggle(eventsBody));
            eventsFullScreenButton.onClick.AddListener(OpenEventsFullScreen);
            systemToggleButton.onClick.AddListener(() => Toggle(systemBody));

            HDCAds.Initialized += MarkDirty;
            HDCAdsSdk.AdEvent += OnAdEvent;
        }

        private void OnDestroy()
        {
            HDCAds.Initialized -= MarkDirty;
            HDCAdsSdk.AdEvent -= OnAdEvent;
        }

        private void OnDisable() => ShowPopupArea(false);

        private void OnAdEvent(HDCAdEvent adEvent) => MarkDirty();

        protected override void Redraw()
        {
            NormalizeSelection();
            groups = HDCAdsDebugModel.Groups(channel, group, position);
            selected = HDCAdsDebugModel.Selected(groups);

            RedrawTabs();
            RedrawActions();
            RedrawDetail();
            RedrawEvents();
            RedrawSystem();
            ShowPopupArea(channel == "PU" && isActiveAndEnabled);
        }

        // Selection: the group first, then the positions of that group.

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
            string title = pickGroup ? $"Select Group · {channel}" : $"Select {PositionLabel()} · {channel}";
            string subtitle = pickGroup
                ? "Chọn group: Detail Information chỉ hiện group này."
                : channel == "BN" ? "Chọn vị trí banner." : "Chọn position để Show, Hide hay đặt vị trí.";
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

        // Tabs and actions

        private void RedrawTabs()
        {
            foreach (ChannelTab tab in tabs)
            {
                HDCDebugStyle.Highlight(tab.button, tab.key == channel);
                if (tab.dot == null)
                    continue;
                HDCUnitTone tone = HDCAdsDebugModel.Tone(tab.key == channel ? groups : HDCAdsDebugModel.Groups(tab.key, "", ""));
                tab.dot.color = HDCDebugStyle.BadgeColor(tone);
            }
        }

        private void RedrawActions()
        {
            channelTitleText.text = $"{Title(channel)} · {channel}";
            bool supported = channel != "CL";
            bool hasGroups = Groups().Length > 0;
            bool hasPositions = Positions().Length > 0;
            if (!supported)
                selectionHintText.text = "Collapsible banner chưa có trong HDC Ads.";
            else if (!HDCAds.IsInitialized)
                selectionHintText.text = "HDCAds chưa khởi tạo: group và position lấy từ config HDCAds đang chạy. Bấm Init SDK ở trên.";
            else
                selectionHintText.text = Hint() + " Các nút gọi thẳng API của HDCAds.";

            HDCDebugStyle.SetVisible(groupButton, hasGroups);
            HDCDebugStyle.SetLabel(groupButton, "Group: " + (hasGroups ? group : "-"));
            HDCDebugStyle.SetVisible(positionButton, hasPositions);
            HDCDebugStyle.SetLabel(positionButton, $"{PositionLabel()}: {(hasPositions ? position : "-")}");

            HDCDebugStyle.SetVisible(initButton, supported);
            HDCDebugStyle.SetVisible(showButton, supported && channel != "AL" && channel != "AR");
            HDCDebugStyle.SetLabel(showButton, channel == "BN" || channel == "MREC" ? "Activate" : "Show");
            HDCDebugStyle.SetVisible(hideButton, channel == "BN" || channel == "MREC" || channel == "PU");
            HDCDebugStyle.SetVisible(utilityPrimaryButton, channel == "MREC" || channel == "PU");
            HDCDebugStyle.SetLabel(utilityPrimaryButton, "UpdatePos");
            HDCDebugStyle.SetVisible(utilitySecondaryButton, channel == "MREC");
            HDCDebugStyle.SetLabel(utilitySecondaryButton, "GetSize");
            lastCallText.text = "Last call: " + (lastCall.Length > 0 ? lastCall : "-");
        }

        private string Hint()
        {
            switch (channel)
            {
                case "FA":
                case "PU":
                    return "Chọn group, rồi position của group đó.";
                case "BN":
                    return "Chọn placement: một trong sáu vị trí banner.";
                case "MREC":
                    return "Chọn vị trí MREC trên màn hình cho UpdatePos.";
                case "RW":
                    return $"Show dùng position {RewardedPosition}.";
                default:
                    return "Kênh này không cần chọn group hay position.";
            }
        }

        // Detail information: the selected group and its ad units.

        private void RedrawDetail()
        {
            if (copiedUntil > 0f && Time.unscaledTime >= copiedUntil)
                copiedUntil = 0f;
            HDCDebugStyle.SetLabel(copyReportButton, copiedUntil > 0f ? "Copied" : "Copy Report");

            groupList.Begin();
            if (selected != null)
                HDCDebugStyle.Fill(groupList, selected.Details);
            groupList.End();

            groupTitleText.text = selected != null ? $"{selected.Kind}: {selected.Name}" : Title(channel);
            HDCDebugStyle.SetVisible(groupList, selected != null);
            if (!HDCAds.IsInitialized)
                unitsNoticeText.text = "Đang chờ HDCAds khởi tạo: ad unit lấy từ ad core config HDCAds đang chạy.";
            else if (channel == "CL")
                unitsNoticeText.text = "Collapsible banner chưa có trong HDC Ads nên kênh này không có ad unit.";
            else if (selected == null)
                unitsNoticeText.text = "Ad core config không có group nào cho kênh này.";
            else if (selected.Units.Count == 0)
                unitsNoticeText.text = "Group này không có ad unit nào trong ad core config.";
            else
                unitsNoticeText.text = $"{selected.Units.Count} ad unit, theo thứ tự group thử. Lỗi hiện mã lỗi của SDK và ý nghĩa.";

            int used = 0;
            foreach (HDCDebugUnit unit in selected?.Units ?? new List<HDCDebugUnit>())
                FillUnit(HDCDebugStyle.Take(unitRows, unitTemplate, ref used), unit);
            HDCDebugStyle.HideRest(unitRows, used);
        }

        private static void FillUnit(GameObject card, HDCDebugUnit unit)
        {
            string status = unit.Status(out HDCUnitTone tone);
            Transform badge = card.transform.Find("Top/Badge");
            badge.GetComponent<Image>().color = HDCDebugStyle.BadgeColor(tone);
            badge.GetComponentInChildren<Text>(true).text = status;
            card.transform.Find("Top/Title").GetComponent<Text>().text = $"#{unit.Index}  {HDCAdsDebugModel.UnitName(unit.Format, unit.Id)}";

            var list = card.transform.Find("Info").GetComponent<HDCKeyValueList>();
            list.Begin();
            HDCAdRecord record = unit.Record;
            list.Row("Instance ID", unit.Id);
            list.Row("Ad Unit ID", string.IsNullOrEmpty(unit.AdUnitId) ? "(none)" : unit.AdUnitId, string.IsNullOrEmpty(unit.AdUnitId) ? HDCDebugStyle.BadColor : HDCDebugStyle.TextColor);
            if (!string.IsNullOrEmpty(record?.AdSource))
                list.Row("Ad Source", record.AdSource);
            if (!string.IsNullOrEmpty(record?.Adapter))
                list.Row("Adapter", record.Adapter.Substring(record.Adapter.LastIndexOf('.') + 1));
            if (!string.IsNullOrEmpty(record?.Layout))
                list.Row("Layout", record.Layout);
            if (!string.IsNullOrEmpty(unit.NativeState))
                list.Row("Native State", unit.NativeState);

            AddError(list, "LOAD ERROR", record?.LastLoadError, false);
            AddError(list, "SHOW ERROR", record?.LastShowError, true);

            if (record == null)
            {
                list.Note(HDCDebugStyle.Colored(HDCDebugStyle.MutedHex, !unit.Created
                    ? "Chưa tạo: bấm Init, hoặc Show một position của group này."
                    : unit.Started ? "Đã chạy nhưng chưa thấy lần load nào." : "Backup: chỉ chạy khi các ad unit trước nó load lỗi."));
            }
            else
            {
                list.Header("Stats");
                list.Row("Requests", Count(record.Requests));
                list.Row("Loaded", Count(record.Loads), record.Loads > 0 ? HDCDebugStyle.GoodColor : HDCDebugStyle.TextColor);
                list.Row("Load Failed", Count(record.LoadFailures), record.LoadFailures > 0 ? HDCDebugStyle.BadColor : HDCDebugStyle.TextColor);
                list.Row("Shows", Count(record.Shows));
                list.Row("Show Failed", Count(record.ShowFailures), record.ShowFailures > 0 ? HDCDebugStyle.BadColor : HDCDebugStyle.TextColor);
                list.Row("Impressions", Count(record.Impressions));
                list.Row("Clicks", Count(record.Clicks));
                if (record.LoadSeconds >= 0f)
                    list.Row("Last Load Took", record.LoadSeconds.ToString("0.0", CultureInfo.InvariantCulture) + " s");
                if (record.Revenue > 0d)
                    list.Row("Revenue", record.Revenue.ToString("0.######", CultureInfo.InvariantCulture) + " " + record.Currency, HDCDebugStyle.WarnColor);
                float retryIn = record.RetryAt - Time.realtimeSinceStartup;
                if (retryIn > 0f)
                    list.Row("Next Retry", $"#{record.RetryAttempt} in {Mathf.CeilToInt(retryIn)} s", HDCDebugStyle.WarnColor);
                list.Row("Updated", record.UpdatedClock.ToString("HH:mm:ss", CultureInfo.InvariantCulture), HDCDebugStyle.MutedColor);
            }

            list.End();
        }

        // An error on lines of its own: code and name, what it means, what to check, then the SDK's message.
        private static void AddError(HDCKeyValueList list, string label, HDCAdError error, bool show)
        {
            if (error == null)
                return;
            list.Note($"<color={HDCDebugStyle.BadHex}><b>{label} · {HDCAdErrorGuide.Title(error, show)}</b></color>   "
                + HDCDebugStyle.Colored(HDCDebugStyle.MutedHex, error.Clock.ToString("HH:mm:ss", CultureInfo.InvariantCulture)));
            foreach (string line in HDCAdErrorGuide.Explain(error, show).Split('\n'))
            {
                const string hint = "Gợi ý: ";
                list.Note(line.StartsWith(hint, StringComparison.Ordinal)
                    ? HDCDebugStyle.Colored(HDCDebugStyle.WarnHex, "Gợi ý: ") + line.Substring(hint.Length)
                    : line);
            }

            if (!string.IsNullOrEmpty(error.AdUnitId))
                list.Note(HDCDebugStyle.Colored(HDCDebugStyle.MutedHex, "Ad unit: " + error.AdUnitId));
            if (!string.IsNullOrEmpty(error.Message))
                list.Note(HDCDebugStyle.Colored(HDCDebugStyle.MutedHex, "Message: " + error.Message));
        }

        private static string Count(int value) => value.ToString(CultureInfo.InvariantCulture);

        // Recent events of the selected group

        private void RedrawEvents()
        {
            HDCDebugStyle.SetLabel(eventsToggleButton, eventsBody.activeSelf ? "Collapse" : "Expand");
            if (!eventsBody.activeSelf)
                return;

            List<HDCTrackedEvent> picked = GroupEvents(EventRows);
            eventsNoticeText.text = picked.Count == 0
                ? "Chưa có sự kiện nào của " + Scope() + "."
                : $"{picked.Count} sự kiện mới nhất của {Scope()}, mới nhất ở trên.";
            int used = 0;
            foreach (HDCTrackedEvent tracked in picked)
                HDCDebugStyle.Take(eventRows, eventTemplate, ref used).GetComponent<Text>().text = HDCEventText.Line(tracked, false, false);
            HDCDebugStyle.HideRest(eventRows, used);
        }

        private List<HDCTrackedEvent> GroupEvents(int limit)
        {
            HashSet<string> ids = HDCAdsDebugModel.InstanceIds(selected != null ? new[] { selected } : (IEnumerable<HDCDebugGroup>)groups);
            var picked = new List<HDCTrackedEvent>();
            IReadOnlyList<HDCTrackedEvent> events = HDCAdsTracker.Events;
            for (int i = events.Count - 1; i >= 0 && picked.Count < limit; i--)
            {
                if (ids.Contains(events[i].Event.id ?? string.Empty))
                    picked.Add(events[i]);
            }

            return picked;
        }

        private void OpenEventsFullScreen()
        {
            viewer.Open("Events · " + Scope(), () =>
            {
                List<HDCTrackedEvent> picked = GroupEvents(FullScreenEvents);
                return picked.Count == 0
                    ? "Chưa có sự kiện nào của " + Scope() + "."
                    : string.Join("\n\n", picked.Select(tracked => HDCEventText.Line(tracked, false, true)));
            }, true);
        }

        private string Scope() => selected != null ? $"{selected.Kind.ToLowerInvariant()} {selected.Name}" : "kênh " + channel;

        // System: only the selected channel's configs, runtime state and gates, and its API.

        private void RedrawSystem()
        {
            systemTitleText.text = "System · " + Title(channel);
            HDCDebugStyle.SetLabel(systemToggleButton, systemBody.activeSelf ? "Collapse" : "Expand");
            if (!systemBody.activeSelf)
                return;

            systemList.Begin();
            if (!HDCAds.IsInitialized)
                systemList.Note(HDCDebugStyle.Colored(HDCDebugStyle.MutedHex, "Đang chờ HDCAds khởi tạo: thông tin kênh có sau khi config được áp dụng."));
            else if (channel == "CL")
                systemList.Note(HDCDebugStyle.Colored(HDCDebugStyle.MutedHex, "Collapsible banner chưa có trong HDC Ads."));
            else
                HDCDebugStyle.Fill(systemList, ChannelInfo());
            systemList.Header("API");
            foreach (KeyValuePair<string, string> call in Api())
                systemList.Row(call.Key, call.Value, HDCDebugStyle.MutedColor);
            systemList.End();
        }

        private HDCDebugInfo ChannelInfo()
        {
            switch (channel)
            {
                case "AL": return HDCAds.AppLaunch.Describe();
                case "AR": return HDCAds.AppResume.Describe();
                case "RW": return HDCAds.Rewarded.Describe();
                case "FA": return HDCAds.ForceAd.Describe(position, group);
                case "BN": return HDCAds.Banner.Describe(Slot());
                case "MREC": return HDCAds.Mrec.Describe();
                default: return HDCAds.Popup.Describe(group, position);
            }
        }

        private IEnumerable<KeyValuePair<string, string>> Api()
        {
            switch (channel)
            {
                case "AL":
                    yield return Call("Init", "HDCAds.AppLaunch.Initialize()");
                    yield return Call("Done", "HDCAds.AppLaunch.Completed, IsCompleted");
                    break;
                case "AR":
                    yield return Call("Init", "HDCAds.AppResume.Initialize()");
                    yield return Call("Skip Next", "HDCAds.AppResume.Block()");
                    break;
                case "RW":
                    yield return Call("Init", "HDCAds.Rewarded.Initialize()");
                    yield return Call("Show", "HDCAds.Rewarded.Show(position, onRewarded, onClosed)");
                    yield return Call("Ready", "HDCAds.Rewarded.CanShow");
                    break;
                case "FA":
                    yield return Call("Init", "HDCAds.ForceAd.Initialize(group)");
                    yield return Call("Show", "HDCAds.ForceAd.Show(position, onDone)");
                    yield return Call("Ready", "HDCAds.ForceAd.CanShow(position)");
                    break;
                case "BN":
                    yield return Call("Init", "HDCAds.Banner.Initialize(slot)");
                    yield return Call("Show / Hide", "HDCAds.Banner.Show(slot), Hide(slot)");
                    break;
                case "MREC":
                    yield return Call("Init", "HDCAds.Mrec.Initialize()");
                    yield return Call("Show / Hide", "HDCAds.Mrec.Show(), Hide()");
                    yield return Call("Place", "HDCAds.Mrec.Move(position), SizeInPixels");
                    break;
                case "PU":
                    yield return Call("Init", "HDCAds.Popup.Initialize(group)");
                    yield return Call("Place", "HDCAds.Popup.Move(position, area)");
                    yield return Call("Show / Hide", "HDCAds.Popup.Show(position), Hide(position)");
                    break;
                default:
                    yield return Call("-", "Not in HDC Ads");
                    break;
            }
        }

        private static KeyValuePair<string, string> Call(string label, string api) => new KeyValuePair<string, string>(label, api);

        // Copy report: the selected group's units, events and system state, as plain text.

        private void CopyReport()
        {
            var text = new StringBuilder()
                .Append("HDC Ads debug report · ").AppendLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture))
                .Append("Channel: ").Append(Title(channel)).Append(" (").Append(channel).AppendLine(")")
                .Append("Group: ").Append(Dash(group)).Append(" · ").Append(PositionLabel()).Append(": ").AppendLine(Dash(position))
                .AppendLine();
            if (selected != null)
            {
                text.Append("== ").Append(selected.Kind).Append(' ').AppendLine(selected.Name).AppendLine(selected.Details.ToText());
                foreach (HDCDebugUnit unit in selected.Units)
                {
                    text.AppendLine().Append("#").Append(unit.Index).Append(' ').Append(HDCAdsDebugModel.UnitName(unit.Format, unit.Id))
                        .Append(" · ").AppendLine(unit.Status(out _))
                        .Append("Instance ID: ").AppendLine(unit.Id)
                        .Append("Ad Unit ID: ").AppendLine(Dash(unit.AdUnitId));
                    AppendError(text, "Load error", unit.Record?.LastLoadError, false);
                    AppendError(text, "Show error", unit.Record?.LastShowError, true);
                    HDCAdRecord record = unit.Record;
                    if (record != null)
                        text.Append("Requests ").Append(record.Requests).Append(", loaded ").Append(record.Loads).Append(", load failed ").Append(record.LoadFailures)
                            .Append(", shows ").Append(record.Shows).Append(", show failed ").Append(record.ShowFailures)
                            .Append(", impressions ").Append(record.Impressions).Append(", clicks ").AppendLine(record.Clicks.ToString(CultureInfo.InvariantCulture));
                }
            }

            text.AppendLine().AppendLine("== Events");
            foreach (HDCTrackedEvent tracked in GroupEvents(FullScreenEvents))
                text.AppendLine(HDCDebugStyle.StripTags(HDCEventText.Line(tracked, false, false)));
            if (HDCAds.IsInitialized && channel != "CL")
                text.AppendLine().AppendLine("== System").AppendLine(ChannelInfo().ToText());

            GUIUtility.systemCopyBuffer = text.ToString();
            copiedUntil = Time.unscaledTime + CopiedSeconds;
            Record("debug report copied to the clipboard");
        }

        private static void AppendError(StringBuilder text, string label, HDCAdError error, bool show)
        {
            if (error == null)
                return;
            text.Append(label).Append(' ').Append(HDCAdErrorGuide.Title(error, show)).Append(" at ")
                .AppendLine(error.Clock.ToString("HH:mm:ss", CultureInfo.InvariantCulture))
                .AppendLine(HDCAdErrorGuide.Explain(error, show));
            if (!string.IsNullOrEmpty(error.Message))
                text.Append("Message: ").AppendLine(error.Message);
        }

        // Actions call the HDCAds API directly.

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
            if (channel == "MREC")
                Record($"Mrec.SizeInPixels -> {HDCAds.Mrec.SizeInPixels}");
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
            MarkDirty();
        }

        private void Toggle(GameObject body)
        {
            body.SetActive(!body.activeSelf);
            Refresh();
        }

        private HDCBannerSlot Slot() => Enum.TryParse(position, out HDCBannerSlot slot) ? slot : HDCBannerSlot.FullBottom;

        private string PositionLabel() => channel == "BN" ? "Placement" : "Position";

        private void ShowPopupArea(bool visible)
        {
            if (popupArea != null && popupArea.gameObject.activeSelf != visible)
                popupArea.gameObject.SetActive(visible);
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

        private static string Dash(string value) => string.IsNullOrEmpty(value) ? "-" : value;

        private static string[] Distinct(IEnumerable<string> values) =>
            values.Where(value => !string.IsNullOrEmpty(value)).Distinct().ToArray();
    }
}
