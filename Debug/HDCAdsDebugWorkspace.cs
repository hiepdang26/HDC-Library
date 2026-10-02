using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using HDC.Ads.Diagnostics;
using HDC.Ads.Domain;
using HDC.Ads.Infrastructure;
using HDC.Ads.Logic;
using UnityEngine;
using UnityEngine.UI;

namespace HDC.Ads.DebugUI
{
    /// <summary>
    /// The ads page of the debug panel. Pick a channel, then a group and a position from the configs HDCAds runs
    /// on, and call the HDCAds API with the channel's buttons. Detail Information shows only the selected group:
    /// its settings, then each of its ad units with its state, its last errors explained, and its counts. The
    /// group's recent events and the channel's system state open on demand. Everything refreshes every second and
    /// on every ad event. The page draws what each channel's debug module gives it, a tab per channel and its
    /// buttons, groups and state, and has no code of its own for any channel.
    /// </summary>
    public sealed class HDCAdsDebugWorkspace : HDCDebugPage
    {
        // The tab the page opens on, when there is one: the channel with the most to see.
        private const string FirstChannel = "FA";
        private const int EventRows = 15;
        private const int FullScreenEvents = 40;
        private const float CopiedSeconds = 1.5f;

        [Header("Channels")]
        [Tooltip("A channel's tab, with \"Key\" and \"Name\" texts and a \"Dot\" image. The page copies it for each channel.")]
        [SerializeField] private Button tabTemplate;

        [Header("Actions")]
        [SerializeField] private Text channelTitleText;
        [SerializeField] private Text selectionHintText;
        [SerializeField] private Button groupButton;
        [SerializeField] private Button positionButton;
        [Tooltip("A button the page copies for each action of the selected channel.")]
        [SerializeField] private Button actionTemplate;
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
        [Tooltip("Where ads placed over the screen show, such as popups. Visible while a channel that uses it is selected.")]
        [SerializeField] private RectTransform popupArea;

        private readonly List<GameObject> tabs = new List<GameObject>();
        private readonly List<GameObject> actionButtons = new List<GameObject>();
        private readonly List<GameObject> unitRows = new List<GameObject>();
        private readonly List<GameObject> eventRows = new List<GameObject>();
        private IReadOnlyList<IAdChannel> tabChannels;
        private IAdChannel actionsChannel;
        private IAdChannel channel;
        private string channelKey = FirstChannel;
        private string group = "";
        private string position = "";
        private string lastCall = "";
        private float copiedUntil;
        private List<HDCDebugGroup> groups = new List<HDCDebugGroup>();
        private HDCDebugGroup selected;

        private IChannelDiagnostics Diagnostics => channel.Diagnostics;

        private void Awake()
        {
            tabTemplate.gameObject.SetActive(false);
            actionTemplate.gameObject.SetActive(false);
            unitTemplate.SetActive(false);
            eventTemplate.SetActive(false);
            eventsBody.SetActive(false);
            systemBody.SetActive(false);

            groupButton.onClick.AddListener(() => OpenPicker(true));
            positionButton.onClick.AddListener(() => OpenPicker(false));
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
            IReadOnlyList<IAdChannel> all = HDCAds.Channels.All;
            channel = all.FirstOrDefault(each => each.Key == channelKey) ?? all.FirstOrDefault();
            if (channel == null)
                return;
            channelKey = channel.Key;
            NormalizeSelection();
            groups = UnitGroups(channel, group, position, true);
            selected = groups.FirstOrDefault(each => each.Selected) ?? (groups.Count == 1 ? groups[0] : null);

            RedrawTabs(all);
            RedrawActions();
            RedrawDetail();
            RedrawEvents();
            RedrawSystem();
            ShowPopupArea(Diagnostics.UsesArea && isActiveAndEnabled);
        }

        // A channel's groups once HDCAds runs on its configs.
        private static List<HDCDebugGroup> UnitGroups(IAdChannel of, string group, string position, bool askNative) =>
            HDCAds.IsInitialized ? of.Diagnostics.UnitGroups(group, position, askNative) : new List<HDCDebugGroup>();

        // Selection: the group first, then the positions of that group.

        private void NormalizeSelection()
        {
            IReadOnlyList<string> groupOptions = GroupOptions();
            group = groupOptions.Count == 0 ? "" : groupOptions.Contains(group) ? group : groupOptions[0];
            IReadOnlyList<string> positionOptions = Diagnostics.Positions(group);
            position = positionOptions.Count == 0 ? "" : positionOptions.Contains(position) ? position : positionOptions[0];
        }

        private IReadOnlyList<string> GroupOptions() => HDCAds.IsInitialized ? Diagnostics.Groups() : new string[0];

        private void OpenPicker(bool pickGroup)
        {
            string title = pickGroup ? $"Select Group · {channel.Key}" : $"Select {Diagnostics.PositionLabel} · {channel.Key}";
            string subtitle = pickGroup ? "Chọn group: Detail Information chỉ hiện group này." : Diagnostics.PositionHint;
            optionPicker.Open(title, subtitle, pickGroup ? GroupOptions() : Diagnostics.Positions(group), pickGroup ? group : position, picked =>
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

        // Tabs and actions: one tab per channel, and the selected channel's buttons.

        private void RedrawTabs(IReadOnlyList<IAdChannel> all)
        {
            if (!ReferenceEquals(all, tabChannels))
            {
                tabChannels = all;
                int used = 0;
                foreach (IAdChannel each in all)
                {
                    GameObject tab = HDCDebugStyle.Take(tabs, tabTemplate.gameObject, ref used);
                    tab.name = each.Key;
                    tab.transform.Find("Key").GetComponent<Text>().text = each.Key;
                    tab.transform.Find("Name").GetComponent<Text>().text = each.Title;
                    string key = each.Key;
                    Button button = tab.GetComponent<Button>();
                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(() =>
                    {
                        channelKey = key;
                        Refresh();
                    });
                }

                HDCDebugStyle.HideRest(tabs, used);
            }

            for (int i = 0; i < all.Count; i++)
            {
                HDCDebugStyle.Highlight(tabs[i].GetComponent<Button>(), all[i] == channel);
                List<HDCDebugGroup> tabGroups = all[i] == channel ? groups : UnitGroups(all[i], "", "", false);
                tabs[i].transform.Find("Dot").GetComponent<Image>().color = HDCDebugStyle.BadgeColor(Tone(tabGroups));
            }
        }

        private void RedrawActions()
        {
            channelTitleText.text = $"{channel.Title} · {channel.Key}";
            selectionHintText.text = HDCAds.IsInitialized
                ? Diagnostics.Hint + " Các nút gọi thẳng API của HDCAds."
                : "HDCAds chưa khởi tạo: group và position lấy từ config HDCAds đang chạy. Bấm Init SDK ở trên.";

            bool hasGroups = GroupOptions().Count > 0;
            bool hasPositions = Diagnostics.Positions(group).Count > 0;
            HDCDebugStyle.SetVisible(groupButton, hasGroups);
            HDCDebugStyle.SetLabel(groupButton, "Group: " + (hasGroups ? group : "-"));
            HDCDebugStyle.SetVisible(positionButton, hasPositions);
            HDCDebugStyle.SetLabel(positionButton, $"{Diagnostics.PositionLabel}: {(hasPositions ? position : "-")}");

            if (actionsChannel != channel)
            {
                actionsChannel = channel;
                int used = 0;
                foreach (HDCDebugAction action in Diagnostics.Actions)
                {
                    GameObject made = HDCDebugStyle.Take(actionButtons, actionTemplate.gameObject, ref used);
                    made.name = action.Name + " Button";
                    Button button = made.GetComponent<Button>();
                    HDCDebugStyle.SetLabel(button, action.Label);
                    button.image.color = action.Primary ? HDCDebugStyle.PrimaryColor : HDCDebugStyle.ButtonColor;
                    HDCDebugAction picked = action;
                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(() => Run(picked));
                }

                HDCDebugStyle.HideRest(actionButtons, used);
            }

            lastCallText.text = "Last call: " + (lastCall.Length > 0 ? lastCall : "-");
        }

        // Calls the channel's API with the selected group and position.
        private void Run(HDCDebugAction action)
        {
            if (Diagnostics.UsesArea)
                ShowPopupArea(true);
            string call = action.Run(new HDCDebugSelection(group, position, popupArea, Record));
            if (!string.IsNullOrEmpty(call))
                Record(call);
            Refresh();
        }

        /// <summary>A channel's overall tone: live or ready if any unit is, then loading, then failing.</summary>
        private static HDCUnitTone Tone(IEnumerable<HDCDebugGroup> of)
        {
            var tones = new List<HDCUnitTone>();
            foreach (HDCDebugUnit unit in of.SelectMany(each => each.Units))
            {
                unit.Status(out HDCUnitTone tone);
                tones.Add(tone);
            }

            if (tones.Contains(HDCUnitTone.Live))
                return HDCUnitTone.Live;
            if (tones.Contains(HDCUnitTone.Good))
                return HDCUnitTone.Good;
            if (tones.Contains(HDCUnitTone.Busy))
                return HDCUnitTone.Busy;
            return tones.Contains(HDCUnitTone.Bad) ? HDCUnitTone.Bad : HDCUnitTone.Idle;
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

            groupTitleText.text = selected != null ? $"{selected.Kind}: {selected.Name}" : channel.Title;
            HDCDebugStyle.SetVisible(groupList, selected != null);
            if (!HDCAds.IsInitialized)
                unitsNoticeText.text = "Đang chờ HDCAds khởi tạo: ad unit lấy từ ad core config HDCAds đang chạy.";
            else if (selected == null)
                unitsNoticeText.text = "Ad core config không có group nào cho kênh này.";
            else if (selected.Units.Count == 0)
                unitsNoticeText.text = "Group này không có ad unit nào trong ad core config.";
            else
                unitsNoticeText.text = $"{selected.Units.Count} ad unit, theo thứ tự group thử. Lỗi hiện mã lỗi của SDK và ý nghĩa."
                    + (HDCAds.Testing.UseTestAdUnits ? " Đang dùng ad unit test của Google thay cho ad unit trong config." : string.Empty);

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
            card.transform.Find("Top/Title").GetComponent<Text>().text = $"#{unit.Index}  {unit.Name}";

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
            var ids = new HashSet<string>((selected != null ? new[] { selected } : (IEnumerable<HDCDebugGroup>)groups)
                .SelectMany(each => each.Units).Select(unit => unit.Id).Where(id => !string.IsNullOrEmpty(id)));
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

        private string Scope() => selected != null ? $"{selected.Kind.ToLowerInvariant()} {selected.Name}" : "kênh " + channel.Key;

        // System: only the selected channel's configs, runtime state and gates, and its API.

        private void RedrawSystem()
        {
            systemTitleText.text = "System · " + channel.Title;
            HDCDebugStyle.SetLabel(systemToggleButton, systemBody.activeSelf ? "Collapse" : "Expand");
            if (!systemBody.activeSelf)
                return;

            systemList.Begin();
            if (HDCAds.IsInitialized)
                HDCDebugStyle.Fill(systemList, Diagnostics.Describe(group, position));
            else
                systemList.Note(HDCDebugStyle.Colored(HDCDebugStyle.MutedHex, "Đang chờ HDCAds khởi tạo: thông tin kênh có sau khi config được áp dụng."));
            systemList.Header("API");
            foreach ((string label, string call) in Diagnostics.Api)
                systemList.Row(label, call, HDCDebugStyle.MutedColor);
            systemList.End();
        }

        // Copy report: the selected group's units, events and system state, as plain text.

        private void CopyReport()
        {
            var text = new StringBuilder()
                .Append("HDC Ads debug report · ").AppendLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture))
                .Append("Channel: ").Append(channel.Title).Append(" (").Append(channel.Key).AppendLine(")")
                .Append("Group: ").Append(Dash(group)).Append(" · ").Append(Diagnostics.PositionLabel).Append(": ").AppendLine(Dash(position))
                .AppendLine();
            if (selected != null)
            {
                text.Append("== ").Append(selected.Kind).Append(' ').AppendLine(selected.Name).AppendLine(selected.Details.ToText());
                foreach (HDCDebugUnit unit in selected.Units)
                {
                    text.AppendLine().Append("#").Append(unit.Index).Append(' ').Append(unit.Name)
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
            if (HDCAds.IsInitialized)
                text.AppendLine().AppendLine("== System").AppendLine(Diagnostics.Describe(group, position).ToText());

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

        private void ShowPopupArea(bool visible)
        {
            if (popupArea != null && popupArea.gameObject.activeSelf != visible)
                popupArea.gameObject.SetActive(visible);
        }

        private static string Dash(string value) => string.IsNullOrEmpty(value) ? "-" : value;
    }
}
