using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HDC.Ads.Diagnostics;
using HDC.Ads.Logic;
using UnityEngine;
using UnityEngine.UI;

namespace HDC.Ads.DebugUI
{
    public sealed class HDCRemoteConfigPage : HDCDebugPage
    {
        private const string FoldAllOption = "__all__";
        private const float CopiedSeconds = 1.5f;

        private enum Mode
        {
            Remote,
            Saved,
            Default,
            Applied,
        }

        private enum Part
        {
            Ads,
            Core,
            Keys,
        }

        [Header("Status")]
        [SerializeField] private HDCKeyValueList statusList;
        [SerializeField] private HDCKeyValueList checkList;

        [Header("Viewer")]
        [SerializeField] private Button remoteButton;
        [SerializeField] private Button savedButton;
        [SerializeField] private Button defaultButton;
        [SerializeField] private Button appliedButton;
        [SerializeField] private Button adsButton;
        [SerializeField] private Button coreButton;
        [SerializeField] private Button keysButton;
        [SerializeField] private Button optionTemplate;
        [Tooltip("Says which source the config HDCAds runs on came from: Remote, Saved, Default or Direct.")]
        [SerializeField] private Text appliedNoteText;
        [SerializeField] private Text viewerInfoText;
        [SerializeField] private Text bodyText;
        [SerializeField] private Button copyButton;
        [SerializeField] private Button fullScreenButton;

        [Header("Ad units map")]
        [SerializeField] private Button mapToggleButton;
        [SerializeField] private GameObject mapBody;
        [SerializeField] private HDCKeyValueList mapList;

        [Header("Helpers")]
        [SerializeField] private HDCAdsDebugViewer viewer;

        private readonly List<GameObject> options = new List<GameObject>();
        private readonly Dictionary<string, HashSet<string>> folded = new Dictionary<string, HashSet<string>>();
        private Mode mode = Mode.Remote;
        private Part part = Part.Ads;
        private string key = string.Empty;
        private float copiedUntil;
        private string plainBody = string.Empty;

        private void Awake()
        {
            optionTemplate.gameObject.SetActive(false);
            mapBody.SetActive(false);
            remoteButton.onClick.AddListener(() => SetMode(Mode.Remote));
            savedButton.onClick.AddListener(() => SetMode(Mode.Saved));
            defaultButton.onClick.AddListener(() => SetMode(Mode.Default));
            appliedButton.onClick.AddListener(() => SetMode(Mode.Applied));
            adsButton.onClick.AddListener(() => SetPart(Part.Ads));
            coreButton.onClick.AddListener(() => SetPart(Part.Core));
            keysButton.onClick.AddListener(() => SetPart(Part.Keys));
            copyButton.onClick.AddListener(Copy);
            fullScreenButton.onClick.AddListener(() => viewer.Open(ViewerTitle(), BuildBody, true));
            mapToggleButton.onClick.AddListener(() =>
            {
                mapBody.SetActive(!mapBody.activeSelf);
                Refresh();
            });
        }

        protected override void Redraw()
        {
            RedrawStatus();
            RedrawCheck();
            RedrawViewer();
            RedrawMap();
        }

        private void RedrawStatus()
        {
            statusList.Begin();
            statusList.Row("Configs From", ConfigsFrom(out HDCDebugTone tone), HDCDebugStyle.ToneColor(tone));
            if (HDCConfigReport.Started)
            {
                statusList.Row("Firebase", Dash(HDCConfigReport.Firebase), HDCConfigReport.Firebase == "Available" ? HDCDebugStyle.GoodColor : HDCDebugStyle.WarnColor);
                if (!HDCConfigReport.DefaultsOnly)
                {
                    statusList.Row("Fetch", Dash(HDCConfigReport.FetchStatus),
                        HDCConfigReport.FetchStatus.StartsWith("Success") ? HDCDebugStyle.GoodColor : HDCDebugStyle.WarnColor);
                    statusList.Row("Last Successful Fetch", Clock(HDCConfigReport.FetchTime));
                    if (HDCConfigReport.ThrottledUntil.HasValue && HDCConfigReport.ThrottledUntil.Value > DateTime.Now)
                        statusList.Row("Throttled Until", Clock(HDCConfigReport.ThrottledUntil), HDCDebugStyle.WarnColor);
                    statusList.Row("Activation", Dash(HDCConfigReport.Activation));
                }

                statusList.Row("Load Started", HDCConfigReport.StartClock.ToString("HH:mm:ss", CultureInfo.InvariantCulture));
                statusList.Row("Load Time", HDCConfigReport.LoadSeconds < 0f ? "Loading..." : HDCConfigReport.LoadSeconds.ToString("0.0", CultureInfo.InvariantCulture) + " s");
                statusList.Row("Ad Core Key", Dash(HDCConfigReport.CoreKey));
                if (!HDCConfigReport.DefaultsOnly)
                    statusList.Row("Keys On Remote Config", HDCConfigReport.RemoteValueCount.ToString(CultureInfo.InvariantCulture));
            }

            if (HDCConfigReport.AppliedAds != null)
            {
                foreach (Part target in new[] { Part.Ads, Part.Core })
                {
                    string source = AppliedFrom(target, out _, out HDCDebugTone sourceTone);
                    statusList.Row("Applied: " + KeyOf(target), source + " · " + Size(AppliedJson(target)), HDCDebugStyle.ToneColor(sourceTone));
                }

                statusList.Row("Applied At", HDCConfigReport.AppliedClock.ToString("HH:mm:ss", CultureInfo.InvariantCulture));
            }

            statusList.End();
        }

        private static string ConfigsFrom(out HDCDebugTone tone)
        {
            tone = HDCDebugTone.Normal;
            if (!HDCConfigReport.Started)
            {
                tone = HDCConfigReport.AppliedAds == null ? HDCDebugTone.Warn : HDCDebugTone.Normal;
                return HDCConfigReport.AppliedAds == null ? "Not loaded: HDCAds is not initialized" : "Given to HDCAds.Initialize directly";
            }

            if (!HDCConfigReport.Finished)
                return "Loading...";
            if (HDCConfigReport.DefaultsOnly)
                return "Project defaults (Editor)";

            HDCConfigEntry ads = HDCConfigReport.Find(HDCRemoteConfigKeys.Ads);
            bool remote = HDCConfigReport.Entries.Count > 0 && HDCConfigReport.Entries.All(entry => entry.Source == HDCConfigSource.Remote);
            tone = remote ? HDCDebugTone.Good : HDCDebugTone.Warn;
            string from = remote ? "Remote Config" : ads != null && ads.Source == HDCConfigSource.Remote ? "Remote Config, partly saved or default" : "Saved or default values";
            return from + " · " + HDCConfigReport.Outcome;
        }

        private static string AppliedFrom(Part target, out string reason, out HDCDebugTone tone)
        {
            string applied = AppliedJson(target);
            string key = KeyOf(target);
            if (applied == null)
            {
                tone = HDCDebugTone.Muted;
                reason = "HDCAds chưa khởi tạo nên chưa áp dụng config nào.";
                return "-";
            }

            HDCConfigEntry entry = HDCConfigReport.Find(key);
            if (entry == null)
            {
                tone = HDCDebugTone.Normal;
                reason = "Config được truyền thẳng vào HDCAds.Initialize, không qua Remote Config của HDC.";
                return "Direct";
            }

            if (Compact(entry.Used) != Compact(applied))
            {
                tone = HDCDebugTone.Warn;
                reason = $"HDCAds.Initialize nhận config khác với giá trị HDCRemoteConfig chọn ({entry.Source}).";
                return "Direct";
            }

            switch (entry.Source)
            {
                case HDCConfigSource.Remote:
                    tone = HDCDebugTone.Good;
                    reason = HDCConfigReport.Outcome == "fetched"
                        ? $"{key} lấy từ Remote Config, vừa fetch xong."
                        : $"{key} lấy từ Remote Config: giá trị Firebase đã kích hoạt từ lần trước (lần tải này: {HDCConfigReport.Outcome}).";
                    return "Remote";
                case HDCConfigSource.Saved:
                    tone = HDCDebugTone.Warn;
                    reason = $"Remote Config không có giá trị cho {key}, nên dùng giá trị lần trước lưu trên máy.";
                    return "Saved";
                default:
                    tone = HDCConfigReport.DefaultsOnly ? HDCDebugTone.Normal : HDCDebugTone.Warn;
                    reason = HDCConfigReport.DefaultsOnly
                        ? $"Trong Editor HDC dùng config mặc định cho {key}, không fetch Firebase."
                        : $"Remote Config không có giá trị cho {key} và máy chưa lưu giá trị nào, nên dùng config mặc định của project.";
                    return "Default";
            }
        }

        private static string AppliedJson(Part target) => target == Part.Ads ? HDCConfigReport.AppliedAds : HDCConfigReport.AppliedCore;

        private static string KeyOf(Part target)
        {
            if (target == Part.Ads)
                return HDCRemoteConfigKeys.Ads;
            string coreKey = !string.IsNullOrEmpty(HDCConfigReport.CoreKey) ? HDCConfigReport.CoreKey : HDCAds.Config.selectedAdCoreName;
            return string.IsNullOrEmpty(coreKey) ? "ad core config" : coreKey;
        }

        private static string Compact(string json) => new string((json ?? string.Empty).Where(c => !char.IsWhiteSpace(c)).ToArray());

        private void RedrawCheck()
        {
            List<HDCConfigFinding> findings = HDCConfigCheck.Run();
            checkList.Begin();
            int errors = findings.Count(finding => finding.Level == HDCConfigLevel.Error);
            int warnings = findings.Count(finding => finding.Level == HDCConfigLevel.Warning);
            if (errors == 0 && warnings == 0 && HDCConfigReport.AppliedAds != null)
                checkList.Note(HDCDebugStyle.Colored(HDCDebugStyle.GoodHex, "<b>OK</b>") + "   Không thấy lỗi nào trong config.");
            foreach (HDCConfigFinding finding in findings.OrderBy(finding => finding.Level))
                checkList.Note(Tag(finding.Level) + "   " + finding.Text);
            checkList.End();
        }

        private static string Tag(HDCConfigLevel level)
        {
            switch (level)
            {
                case HDCConfigLevel.Error: return HDCDebugStyle.Colored(HDCDebugStyle.BadHex, "<b>ERROR</b>");
                case HDCConfigLevel.Warning: return HDCDebugStyle.Colored(HDCDebugStyle.WarnHex, "<b>WARNING</b>");
                default: return HDCDebugStyle.Colored(HDCDebugStyle.InfoHex, "<b>INFO</b>");
            }
        }

        private void SetMode(Mode value)
        {
            mode = value;
            Refresh();
        }

        private void SetPart(Part value)
        {
            part = value;
            Refresh();
        }

        private void RedrawViewer()
        {
            HDCDebugStyle.Highlight(remoteButton, mode == Mode.Remote);
            HDCDebugStyle.Highlight(savedButton, mode == Mode.Saved);
            HDCDebugStyle.Highlight(defaultButton, mode == Mode.Default);
            HDCDebugStyle.Highlight(appliedButton, mode == Mode.Applied);
            HDCDebugStyle.Highlight(adsButton, part == Part.Ads);
            HDCDebugStyle.Highlight(coreButton, part == Part.Core);
            HDCDebugStyle.Highlight(keysButton, part == Part.Keys);
            RedrawAppliedSource();

            string text = BuildBody();
            bodyText.text = text;
            plainBody = HDCDebugStyle.StripTags(text);
            viewerInfoText.text = ViewerInfo();
            RedrawOptions();
            if (copiedUntil > 0f && Time.unscaledTime >= copiedUntil)
                copiedUntil = 0f;
            HDCDebugStyle.SetLabel(copyButton, copiedUntil > 0f ? "Copied" : "Copy");
        }

        private void RedrawAppliedSource()
        {
            if (part == Part.Keys)
            {
                HDCDebugStyle.SetLabel(remoteButton, "Remote");
                HDCDebugStyle.SetLabel(savedButton, "Saved");
                HDCDebugStyle.SetLabel(defaultButton, "Default");
                HDCDebugStyle.SetLabel(appliedButton, "Applied");
                appliedNoteText.text = HDCConfigReport.AppliedAds == null
                    ? HDCDebugStyle.Colored(HDCDebugStyle.MutedHex, "HDCAds chưa khởi tạo nên chưa áp dụng config nào.")
                    : "HDC chỉ dùng 2 key: " + Uses(Part.Ads) + ", " + Uses(Part.Core) + ". All Keys luôn là giá trị Remote Config.";
                return;
            }

            string source = AppliedFrom(part, out string reason, out HDCDebugTone tone);
            HDCDebugStyle.SetLabel(remoteButton, source == "Remote" ? "Remote · used" : "Remote");
            HDCDebugStyle.SetLabel(savedButton, source == "Saved" ? "Saved · used" : "Saved");
            HDCDebugStyle.SetLabel(defaultButton, source == "Default" ? "Default · used" : "Default");
            HDCDebugStyle.SetLabel(appliedButton, "Applied: " + source);
            appliedNoteText.text = $"<b><color={Hex(tone)}>APPLIED = {source.ToUpperInvariant()}</color></b>   {reason}";
        }

        private static string Uses(Part target)
        {
            string source = AppliedFrom(target, out _, out HDCDebugTone tone);
            return $"{KeyOf(target)} = <b><color={Hex(tone)}>{source}</color></b>";
        }

        private static string Hex(HDCDebugTone tone)
        {
            switch (tone)
            {
                case HDCDebugTone.Good: return HDCDebugStyle.GoodHex;
                case HDCDebugTone.Warn: return HDCDebugStyle.WarnHex;
                case HDCDebugTone.Bad: return HDCDebugStyle.BadHex;
                case HDCDebugTone.Muted: return HDCDebugStyle.MutedHex;
                default: return "#F1F5F9";
            }
        }

        private string ViewerTitle() =>
            part == Part.Keys ? "Remote Config · " + Dash(key)
            : mode == Mode.Applied ? $"{CurrentKey()} · Applied = {AppliedFrom(part, out _, out _)}"
            : $"{CurrentKey()} · {mode}";

        private string ViewerInfo()
        {
            if (part == Part.Keys)
            {
                if (HDCConfigReport.RemoteValueCount == 0)
                    return HDCConfigReport.DefaultsOnly ? "Editor không fetch Remote Config, nên không có key nào." : "Remote Config chưa có key nào.";
                return $"Key: {Dash(key)} · Firebase: {Dash(HDCConfigReport.RemoteOrigin(key))} · {Size(HDCConfigReport.RemoteValue(key))}. Mục này luôn là giá trị Remote Config.";
            }

            string value = RawValue(out string note);
            return $"Key: {CurrentKey()} · {ModeName()} · {Size(value)}" + (string.IsNullOrEmpty(note) ? string.Empty : " · " + note);
        }

        private string ModeName()
        {
            switch (mode)
            {
                case Mode.Remote: return "giá trị trên Remote Config";
                case Mode.Saved: return "giá trị lần trước lưu trên máy";
                case Mode.Default: return "config mặc định của project";
                default: return "config HDCAds đang chạy (= " + AppliedFrom(part, out _, out _) + ")";
            }
        }

        private string CurrentKey() => KeyOf(part);

        private string BuildBody()
        {
            if (part == Part.Keys)
            {
                string[] keys = HDCConfigReport.RemoteValues.Select(pair => pair.Key).ToArray();
                if (keys.Length > 0 && Array.IndexOf(keys, key) < 0)
                    key = keys[0];
                return keys.Length == 0 ? "(Không có key nào)" : Show(HDCConfigReport.RemoteValue(key), null);
            }

            string value = RawValue(out _);
            return Show(value, FoldedKeys());
        }

        private static string Show(string value, HashSet<string> foldedKeys)
        {
            if (string.IsNullOrEmpty(value))
                return HDCDebugStyle.Colored(HDCDebugStyle.MutedHex, "(trống)");
            string pretty = HDCJsonText.Pretty(value);
            if (foldedKeys != null)
            {
                List<HDCJsonText.Section> sections = HDCJsonText.Sections(pretty);
                pretty = HDCJsonText.Fold(pretty, sections, foldedKeys);
            }

            return HDCJsonText.Colored(pretty);
        }

        private string RawValue(out string note)
        {
            note = string.Empty;
            string currentKey = CurrentKey();
            HDCConfigEntry entry = HDCConfigReport.Find(currentKey);
            switch (mode)
            {
                case Mode.Remote:
                    if (entry == null)
                    {
                        note = HDCConfigReport.Started ? "HDC không đọc key này" : "chưa tải Remote Config";
                        return HDCConfigReport.RemoteValue(currentKey) ?? string.Empty;
                    }

                    note = "Firebase: " + Dash(entry.RemoteOrigin);
                    return entry.Remote;
                case Mode.Saved:
                    if (entry != null)
                    {
                        note = "đọc lúc tải config";
                        return entry.Saved;
                    }

                    note = "đọc lúc này";
                    return Saved(currentKey);
                case Mode.Default:
                    if (entry != null)
                        return entry.Default;
                    HDCAdsSettings settings = HDCAdsSettings.Load();
                    return part == Part.Ads ? settings.AdsConfig : settings.CoreConfig;
                default:
                    if (HDCConfigReport.AppliedAds == null)
                        note = "HDCAds chưa khởi tạo";
                    return AppliedJson(part) ?? string.Empty;
            }
        }

        private static string Saved(string currentKey)
        {
            try
            {
                return PlayerPrefs.GetString(currentKey, string.Empty);
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        private HashSet<string> FoldedKeys()
        {
            string state = CurrentKey() + "/" + mode;
            if (folded.TryGetValue(state, out HashSet<string> keys))
                return keys;
            keys = new HashSet<string>(HDCJsonText.Sections(HDCJsonText.Pretty(RawValue(out _))).Select(section => section.Key));
            folded[state] = keys;
            return keys;
        }

        private void RedrawOptions()
        {
            int used = 0;
            if (part == Part.Keys)
            {
                foreach (KeyValuePair<string, string> pair in HDCConfigReport.RemoteValues)
                {
                    string picked = pair.Key;
                    Option(ref used, picked, picked == key, () =>
                    {
                        key = picked;
                        Refresh();
                    });
                }
            }
            else
            {
                List<HDCJsonText.Section> sections = HDCJsonText.Sections(HDCJsonText.Pretty(RawValue(out _)));
                HashSet<string> keys = FoldedKeys();
                if (sections.Count > 0)
                {
                    bool allFolded = sections.All(section => keys.Contains(section.Key));
                    Option(ref used, allFolded ? "Expand All" : "Collapse All", false, () => Fold(FoldAllOption));
                }

                foreach (HDCJsonText.Section section in sections)
                {
                    string sectionKey = section.Key;
                    bool isFolded = keys.Contains(sectionKey);
                    Option(ref used, (isFolded ? "+ " : "- ") + sectionKey, !isFolded, () => Fold(sectionKey));
                }
            }

            HDCDebugStyle.HideRest(options, used);
        }

        private void Option(ref int used, string label, bool selected, Action onClick)
        {
            GameObject option = HDCDebugStyle.Take(options, optionTemplate.gameObject, ref used);
            var button = option.GetComponent<Button>();
            HDCDebugStyle.SetLabel(button, label);
            HDCDebugStyle.Highlight(button, selected);
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick());
        }

        private void Fold(string sectionKey)
        {
            HashSet<string> keys = FoldedKeys();
            List<HDCJsonText.Section> sections = HDCJsonText.Sections(HDCJsonText.Pretty(RawValue(out _)));
            if (sectionKey == FoldAllOption)
            {
                bool allFolded = sections.All(section => keys.Contains(section.Key));
                keys.Clear();
                if (!allFolded)
                    keys.UnionWith(sections.Select(section => section.Key));
            }
            else if (!keys.Remove(sectionKey))
            {
                keys.Add(sectionKey);
            }

            Refresh();
        }

        private void Copy()
        {
            GUIUtility.systemCopyBuffer = part == Part.Keys
                ? HDCConfigReport.RemoteValue(key) ?? string.Empty
                : HDCJsonText.Pretty(RawValue(out _));
            copiedUntil = Time.unscaledTime + CopiedSeconds;
            Refresh();
        }

        private void RedrawMap()
        {
            HDCDebugStyle.SetLabel(mapToggleButton, mapBody.activeSelf ? "Collapse" : "Expand");
            if (!mapBody.activeSelf)
                return;

            mapList.Begin();
            if (!HDCAds.IsInitialized)
            {
                mapList.Note(HDCDebugStyle.Colored(HDCDebugStyle.MutedHex, "HDCAds chưa khởi tạo: bản đồ lấy từ config HDCAds đang chạy."));
                mapList.End();
                return;
            }

            var map = new HDCDebugInfo();
            foreach (IAdChannel channel in HDCAds.Channels.All)
                channel.Diagnostics.MapUnits(map);
            HDCDebugStyle.Fill(mapList, map);
            mapList.End();
        }

        private static string Size(string value) =>
            string.IsNullOrEmpty(value) ? "empty" : value.Length.ToString("#,0", CultureInfo.InvariantCulture) + " chars";

        private static string Clock(DateTime? time) => time.HasValue ? time.Value.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) : "-";

        private static string Dash(string value) => string.IsNullOrEmpty(value) ? "-" : value;
    }

    internal static class HDCRemoteConfigKeys
    {
        internal const string Ads = "ads_config";
    }
}
