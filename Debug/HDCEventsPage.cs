using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace HDC.Ads.DebugUI
{
    /// <summary>
    /// The events page: every ad event of every channel, newest first or counted by kind, filtered by channel and
    /// event type, optionally with what each one means. Copy takes the list; Clear forgets it.
    /// </summary>
    public sealed class HDCEventsPage : HDCDebugPage
    {
        private const float CopiedSeconds = 1.5f;

        // A Text draws at most about 16,000 characters, so the list on screen keeps to the newest events.
        private const int ShownEvents = 80;
        private const int ShownExplainedEvents = 40;

        [SerializeField] private HDCKeyValueList summaryList;
        [SerializeField] private Button sequentialButton;
        [SerializeField] private Button countButton;
        [SerializeField] private Button channelButton;
        [SerializeField] private Button typeButton;
        [SerializeField] private Button explainButton;
        [SerializeField] private Button copyButton;
        [SerializeField] private Button clearButton;
        [SerializeField] private Button fullScreenButton;
        [SerializeField] private Text noticeText;
        [SerializeField] private Text listText;

        [Header("Helpers")]
        [SerializeField] private HDCOptionPicker optionPicker;
        [SerializeField] private HDCAdsDebugViewer viewer;

        private bool counting;
        private bool explain;
        private string channel = HDCEventText.AllOption;
        private string type = HDCEventText.AllOption;
        private float copiedUntil;

        private void Awake()
        {
            sequentialButton.onClick.AddListener(() => SetCounting(false));
            countButton.onClick.AddListener(() => SetCounting(true));
            channelButton.onClick.AddListener(() => Pick("Chọn channel", "Chỉ hiện sự kiện của channel này.",
                new[] { HDCEventText.AllOption }.Concat(HDCEventText.Channels), channel, picked => channel = picked));
            typeButton.onClick.AddListener(() => Pick("Chọn loại sự kiện", "Chỉ hiện một loại sự kiện.",
                new[] { HDCEventText.AllOption }.Concat(HDCEventText.Types), type, picked => type = picked));
            explainButton.onClick.AddListener(() =>
            {
                explain = !explain;
                Refresh();
            });
            copyButton.onClick.AddListener(() =>
            {
                GUIUtility.systemCopyBuffer = HDCDebugStyle.StripTags(Body(int.MaxValue));
                copiedUntil = Time.unscaledTime + CopiedSeconds;
                Refresh();
            });
            clearButton.onClick.AddListener(() =>
            {
                HDCAdsTracker.ClearEvents();
                Refresh();
            });
            fullScreenButton.onClick.AddListener(() => viewer.Open(Title(), ShownBody, true));
            HDCAdsSdk.AdEvent += OnAdEvent;
        }

        private void OnDestroy() => HDCAdsSdk.AdEvent -= OnAdEvent;

        private void OnAdEvent(HDCAdEvent adEvent) => MarkDirty();

        /// <summary>Opens the events full screen, filtered to one channel.</summary>
        internal void OpenFullScreen(string channelFilter)
        {
            channel = channelFilter;
            type = HDCEventText.AllOption;
            viewer.Open(Title(), ShownBody, true);
        }

        protected override void Redraw()
        {
            RedrawSummary();
            HDCDebugStyle.Highlight(sequentialButton, !counting);
            HDCDebugStyle.Highlight(countButton, counting);
            HDCDebugStyle.SetLabel(channelButton, "Channel: " + channel);
            HDCDebugStyle.SetLabel(typeButton, "Event: " + type);
            HDCDebugStyle.SetLabel(explainButton, explain ? "Explain: On" : "Explain: Off");
            HDCDebugStyle.Highlight(explainButton, explain);
            if (copiedUntil > 0f && Time.unscaledTime >= copiedUntil)
                copiedUntil = 0f;
            HDCDebugStyle.SetLabel(copyButton, copiedUntil > 0f ? "Copied" : "Copy");

            int shown = Filtered().Count;
            noticeText.text = shown == 0
                ? "Không có sự kiện nào khớp bộ lọc."
                : counting ? $"{shown} sự kiện, gộp theo loại và ad, nhiều nhất trước." : $"{shown} sự kiện, mới nhất ở trên.";
            listText.text = shown == 0 ? string.Empty : ShownBody();
        }

        private void RedrawSummary()
        {
            IReadOnlyList<HDCTrackedEvent> events = HDCAdsTracker.Events;
            summaryList.Begin();
            summaryList.Row("Total Events", HDCAdsTracker.TotalEvents.ToString(CultureInfo.InvariantCulture));
            summaryList.Row("Kept", $"{events.Count} (last {HDCAdsTracker.EventCapacity})");
            summaryList.Row("Latest", events.Count == 0 ? "-" : $"{events[events.Count - 1].Event.type} · {events[events.Count - 1].Event.id}");
            foreach (string eventType in HDCEventText.Types)
            {
                int count = events.Count(tracked => tracked.Event.type == eventType);
                if (count == 0)
                    continue;
                string value = count.ToString(CultureInfo.InvariantCulture);
                if (eventType == HDCAdEventType.Paid)
                    value += " · " + events.Where(tracked => tracked.Event.type == eventType).Sum(tracked => tracked.Event.Revenue).ToString("0.######", CultureInfo.InvariantCulture);
                summaryList.Row(eventType, value, eventType == HDCAdEventType.LoadFailed || eventType == HDCAdEventType.ShowFailed ? HDCDebugStyle.BadColor : HDCDebugStyle.TextColor);
            }

            summaryList.End();
        }

        private void SetCounting(bool value)
        {
            counting = value;
            Refresh();
        }

        private void Pick(string title, string subtitle, IEnumerable<string> values, string selected, System.Action<string> onPicked) =>
            optionPicker.Open(title, subtitle, values, selected, picked =>
            {
                onPicked(picked);
                Refresh();
            });

        private string Title() =>
            $"Events · {(counting ? "Count" : "Sequential")} · Channel {channel} · Event {type}";

        private List<HDCTrackedEvent> Filtered()
        {
            var picked = new List<HDCTrackedEvent>();
            IReadOnlyList<HDCTrackedEvent> events = HDCAdsTracker.Events;
            for (int i = events.Count - 1; i >= 0; i--)
            {
                if (HDCEventText.Matches(events[i].Event, channel, type))
                    picked.Add(events[i]);
            }

            return picked;
        }

        private string ShownBody() => Body(explain ? ShownExplainedEvents : ShownEvents);

        private string Body(int limit)
        {
            List<HDCTrackedEvent> events = Filtered();
            if (events.Count == 0)
                return "Không có sự kiện nào khớp bộ lọc.";

            var text = new StringBuilder();
            if (!counting)
            {
                if (events.Count > limit)
                    text.AppendLine(HDCDebugStyle.Colored(HDCDebugStyle.MutedHex, $"{limit} sự kiện mới nhất trong {events.Count}; Copy lấy tất cả.")).AppendLine();
                foreach (HDCTrackedEvent tracked in events.Take(limit))
                    text.AppendLine(HDCEventText.Line(tracked, true, explain));
                return text.ToString().TrimEnd();
            }

            foreach (var group in events.GroupBy(tracked => tracked.Event.type + "|" + tracked.Event.id)
                         .Select(group => new { Event = group.First().Event, Count = group.Count() })
                         .OrderByDescending(group => group.Count))
            {
                text.Append("<b>").Append(group.Count).Append("×</b>   ")
                    .Append("<color=").Append(HDCEventText.TypeHex(group.Event.type)).Append("><b>").Append(group.Event.type).Append("</b></color>   ")
                    .Append(HDCDebugStyle.Colored(HDCDebugStyle.AccentHex, HDCEventText.Channel(group.Event))).Append("   ")
                    .Append(string.IsNullOrEmpty(group.Event.id) ? "-" : group.Event.id);
                if (explain)
                    text.Append('\n').Append("          ").Append(HDCDebugStyle.Colored(HDCDebugStyle.MutedHex, HDCEventText.Explain(group.Event)));
                text.AppendLine();
            }

            return text.ToString().TrimEnd();
        }
    }
}
