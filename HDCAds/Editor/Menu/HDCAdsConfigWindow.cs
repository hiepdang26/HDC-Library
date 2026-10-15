using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace HDC.Ads.Editor
{
    internal sealed class HDCAdsConfigWindow : EditorWindow
    {
        internal const int AdsAndroidPage = 0;
        internal const int CoreAndroidPage = 2;
        internal const int CoreIosPage = 3;
        internal const int CountryAdsAndroidPage = 4;
        internal const int CustomKeysPage = 8;
        internal const int CountryCheckPage = 9;
        internal const int AdsCard = 0;
        internal const int CoreCard = 1;
        internal const int KeysCard = 2;
        internal const int CountryCard = 3;
        internal const int CardCount = 4;
        internal const string WindowTitle = "HDC Ads Settings";
        internal const string WidthKey = "HDC.AdsSettings.JsonWidth";
        internal const float MinJsonWidth = 260f;
        internal const float DefaultWidth = 960f;
        internal const float MinWidth = 700f;
        internal const int AndroidPlatform = 0;
        internal const int IosPlatform = 1;

        private const float MaxBoxHeight = 520f;
        private const float HandleWidth = 10f;
        private const int KeyBox = 8;
        private const int BoxCount = 9;
        private const string KeysProperty = "customKeys";
        private const string CountryProperty = "countryCheck";
        private const string SourceAndroidProperty = "configSourceAndroid";
        private const string SourceIosProperty = "configSourceIos";

        internal static readonly Color Blue = new Color(0.30f, 0.55f, 1.00f, 1f);
        internal static readonly Color Purple = new Color(0.65f, 0.45f, 1.00f, 1f);
        internal static readonly Color Teal = new Color(0.18f, 0.77f, 0.71f, 1f);
        internal static readonly Color Orange = new Color(1.00f, 0.62f, 0.26f, 1f);
        internal static readonly Color Green = new Color(0.20f, 0.76f, 0.42f, 1f);
        internal static readonly Color Amber = new Color(1.00f, 0.71f, 0.28f, 1f);
        internal static readonly Color Red = new Color(1.00f, 0.37f, 0.34f, 1f);
        internal static readonly Color Gray = new Color(0.55f, 0.55f, 0.55f, 1f);
        internal static readonly Color Sky = new Color(0.31f, 0.70f, 1.00f, 1f);

        private static readonly string[] Platforms = { "Android", "iOS" };
        private static readonly string[] Variants = { "Default", "Country" };
        private static readonly string[] ValueFields = { "android", "ios", "country" };
        private static readonly string[] ValueHelp =
        {
            "Used on Android when Remote Config gives no value for the key, or when Config Source is Default.",
            "Used on iOS when Remote Config gives no value for the key, or when Config Source is Default. Empty: the Android value.",
            "Used in country mode, in place of Remote Config. Empty: the value of the platform.",
        };

        private static readonly string[] CardTitles = { "Ads config", "Ad core", "Custom keys", "Country mode" };
        private static readonly Color[] Accents = { Blue, Purple, Teal, Orange };

        private static readonly string[] CardHelp =
        {
            "Default of the Remote Config key ads_config, used when this launch gets no value from Remote Config, or when Config Source is Default. " +
            "One JSON per platform, plus a Country JSON for country mode.",
            "Default ad core config, for the key that ads_config names in selectedAdCoreName. Used like the Ads config default. One JSON per platform, " +
            "plus a Country JSON for country mode.",
            "Remote Config keys of the game itself, read with HDCCustomConfig.Get(key). Each value is used when this launch gets no value for the key " +
            "from Remote Config, or when Config Source is Default. Nothing is kept on the device between launches.",
            "Country mode: a device in the target country gets the Country JSON of Ads config and Ad core, and the Country value of each custom key, " +
            "instead of the Remote Config values. A device counts as in the country when any signal below matches. Devices in Debug devices, or in " +
            "the Remote Config key devices ({\"debugDevices\":[...]}), never get it. The Editor gets it only with Simulate in Editor.",
        };

        private static readonly Page[] Pages =
        {
            new Page("Ads config (Android)", "adsConfigAndroid", HDCConfigJson.Kind.Ads, false,
                "Default of ads_config on Android."),
            new Page("Ads config (iOS)", "adsConfigIos", HDCConfigJson.Kind.Ads, true,
                "Default of ads_config on iOS. Leave it empty to use the Android one."),
            new Page("Ad core (Android)", "coreConfigAndroid", HDCConfigJson.Kind.Core, false,
                "Default ad core config on Android."),
            new Page("Ad core (iOS)", "coreConfigIos", HDCConfigJson.Kind.Core, true,
                "Default ad core config on iOS. Leave it empty to use the Android one."),
            new Page("Country ads (Android)", "countryAdsConfigAndroid", HDCConfigJson.Kind.Ads, true,
                "ads_config that a device in the target country gets on Android while country mode is on. Leave it empty to keep the usual ads_config."),
            new Page("Country ads (iOS)", "countryAdsConfigIos", HDCConfigJson.Kind.Ads, true,
                "ads_config in country mode on iOS. Leave it empty to use the Android one."),
            new Page("Country core (Android)", "countryCoreConfigAndroid", HDCConfigJson.Kind.Core, true,
                "Ad core config that a device in the target country gets on Android while country mode is on. Leave it empty to keep the usual one."),
            new Page("Country core (iOS)", "countryCoreConfigIos", HDCConfigJson.Kind.Core, true,
                "Ad core config in country mode on iOS. Leave it empty to use the Android one."),
        };

        private static readonly (string Property, string Title)[] Sections =
        {
            (SourceAndroidProperty, "config source (Android)"),
            (SourceIosProperty, "config source (iOS)"),
            (KeysProperty, "custom keys"),
            (CountryProperty, "country mode"),
        };

        [SerializeField] private HDCAdsSettings asset;
        [SerializeField] private bool[] open = new bool[CardCount];
        [SerializeField] private int platform;
        [SerializeField] private string platformSource = "";
        [SerializeField] private int[] variant = new int[2];
        [SerializeField] private int selectedKey;
        [SerializeField] private int valueTab;
        [SerializeField] private int focus = -1;
        private readonly string[] drafts = new string[Pages.Length];
        private readonly string[] checkedTexts = new string[Pages.Length];
        private readonly HDCConfigJson.Result[] checks = new HDCConfigJson.Result[Pages.Length];
        private readonly Vector2[] boxScrolls = new Vector2[BoxCount];
        private readonly float[] boxHeights = Enumerable.Repeat(160f, BoxCount).ToArray();
        private readonly float[] tops = new float[CardCount];
        [SerializeField] private HDCAdsSettings working;
        private SerializedObject settings;
        private SerializedObject original;
        private Vector2 scroll;
        private float jsonWidth;
        private float dragFrom;
        private float dragWidth;
        private GUIStyle titleStyle;
        private GUIStyle helpStyle;
        private GUIStyle pillStyle;
        private GUIStyle tabStyle;
        private GUIStyle tabOnStyle;
        private GUIStyle boxStyle;
        private GUIStyle labelStyle;
        private GUIStyle barStyle;
        private GUIStyle barTitleStyle;
        private GUIStyle barPathStyle;
        private GUIStyle barButtonStyle;
        private GUIStyle barButtonOffStyle;

        private static Color Panel => EditorGUIUtility.isProSkin ? new Color(0.20f, 0.20f, 0.20f, 1f) : new Color(0.84f, 0.84f, 0.84f, 1f);

        private static Color Bar => EditorGUIUtility.isProSkin ? new Color(0.14f, 0.14f, 0.14f, 1f) : new Color(0.76f, 0.76f, 0.76f, 1f);

        private static Color TabBack => EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.05f) : new Color(0f, 0f, 0f, 0.06f);

        internal static int ActivePlatform => EditorUserBuildSettings.activeBuildTarget == BuildTarget.iOS ? IosPlatform : AndroidPlatform;

        internal static void Open(int page, HDCAdsSettings target = null, int platformIndex = -1)
        {
            bool json = page >= 0 && page < Pages.Length;
            int chosen = platformIndex >= 0 ? platformIndex : json ? page % 2 : ActivePlatform;
            HDCAdsConfigWindow window = Show(target, chosen, platformIndex < 0 && !json ? "the active build target" : "the menu");
            if (page == CustomKeysPage)
            {
                window.Reveal(KeysCard);
            }
            else if (page == CountryCheckPage)
            {
                window.Reveal(CountryCard);
            }
            else if (page >= 0 && page < Pages.Length)
            {
                int card = page % 4 < 2 ? AdsCard : CoreCard;
                window.variant[card] = page >= CountryAdsAndroidPage ? 1 : 0;
                window.Reveal(card);
            }
        }

        internal static void OpenCard(int card, HDCAdsSettings target = null, int platformIndex = -1, string source = null)
        {
            HDCAdsConfigWindow window = Show(target, platformIndex >= 0 ? platformIndex : ActivePlatform,
                source ?? (platformIndex >= 0 ? "the menu" : "the active build target"));
            if (card >= 0 && card < CardCount)
                window.Reveal(card);
        }

        internal static string CardTitle(int card) => CardTitles[card];

        internal static Color CardAccent(int card) => Accents[card];

        internal static int PageOf(int card, int platformIndex, int variantIndex) => (card == AdsCard ? 0 : 2) + platformIndex + (variantIndex == 1 ? 4 : 0);

        internal static string PlatformName(int platformIndex) => Platforms[platformIndex == IosPlatform ? IosPlatform : AndroidPlatform];

        internal static (string Text, Color Color) SavedStatus(SerializedObject settings, int card, int platformIndex)
        {
            if (card == KeysCard)
                return KeysStatus(settings);
            if (card == CountryCard)
                return CountryStatus(settings);
            return JsonCardStatus(card, platformIndex, page => settings.FindProperty(Pages[page].Property).stringValue, page => false, CheckText);
        }

        internal static (string Text, Color Color) JsonStatus(string text, HDCConfigJson.Result check, bool changed)
        {
            if (changed)
                return ("Unsaved changes", Amber);
            if (!check.IsValid)
                return ("Invalid JSON", Red);
            return string.IsNullOrWhiteSpace(text) ? ("Empty", Gray) : ("Valid", Green);
        }

        internal static (string Text, Color Color) JsonCardStatus(int card, int platformIndex, Func<int, string> text, Func<int, bool> changed,
            Func<int, string, HDCConfigJson.Result> check)
        {
            int[] pages = { PageOf(card, platformIndex, 0), PageOf(card, platformIndex, 1) };
            if (pages.Any(changed))
                return ("Unsaved changes", Amber);
            string[] invalid = pages.Where(page => !check(page, text(page)).IsValid).Select(page => Variants[page >= CountryAdsAndroidPage ? 1 : 0]).ToArray();
            if (invalid.Length > 0)
                return ("Invalid JSON: " + string.Join(", ", invalid), Red);
            string main = text(pages[0]);
            string status = string.IsNullOrWhiteSpace(main) ? platformIndex == IosPlatform ? "Empty · uses Android" : "Empty" : "Valid";
            return (status + (string.IsNullOrWhiteSpace(text(pages[1])) ? "" : " · country set"), string.IsNullOrWhiteSpace(main) ? Gray : Green);
        }

        internal static string[] Split(string text) =>
            (text ?? "").Split(',').Select(part => part.Trim()).Where(part => part.Length > 0).ToArray();

        public override void SaveChanges()
        {
            List<string> kept = SaveAll();
            if (kept.Count == 0)
            {
                base.SaveChanges();
                return;
            }

            EditorUtility.DisplayDialog(WindowTitle, "These JSON have errors, so they were not saved and the window stays open: " + string.Join(", ", kept) +
                                                     ". Fix them, or press Revert, then close the window again.", "OK");
        }

        public override void DiscardChanges()
        {
            for (int i = 0; i < drafts.Length; i++)
                drafts[i] = null;
            Release();
            base.DiscardChanges();
        }

        internal static void ShowOrFocus()
        {
            HDCAdsConfigWindow[] open = Resources.FindObjectsOfTypeAll<HDCAdsConfigWindow>();
            if (open.Length == 0)
            {
                OpenCard(-1);
                return;
            }

            open[0].Focus();
        }

        internal static Rect Centered(Rect screen, float width, float height)
        {
            float w = Mathf.Min(width, screen.width);
            float h = Mathf.Min(height, screen.height);
            return new Rect(Mathf.Round(screen.x + (screen.width - w) / 2f), Mathf.Round(screen.y + (screen.height - h) / 2f), w, h);
        }

        private static HDCAdsConfigWindow Show(HDCAdsSettings target, int platformIndex, string source)
        {
            bool created = !HasOpenInstances<HDCAdsConfigWindow>();
            var window = GetWindow<HDCAdsConfigWindow>(WindowTitle);
            window.minSize = new Vector2(MinWidth, 420f);
            if (created)
            {
                Rect screen = EditorGUIUtility.GetMainWindowPosition();
                window.position = Centered(screen, DefaultWidth, Mathf.Clamp(screen.height * 0.8f, 560f, 900f));
            }

            if (target != null && target != window.asset)
            {
                window.asset = target;
                window.Release();
            }

            window.platform = platformIndex == IosPlatform ? IosPlatform : AndroidPlatform;
            window.platformSource = source ?? "";
            window.titleContent = new GUIContent(WindowTitle + " · " + Platforms[window.platform]);
            window.Focus();
            window.Repaint();
            return window;
        }

        private void Reveal(int card)
        {
            open[card] = true;
            focus = card;
            GUI.FocusControl(null);
            Repaint();
        }

        private void OnEnable()
        {
            titleContent = new GUIContent(WindowTitle + " · " + Platforms[platform]);
            wantsMouseMove = true;
            saveChangesMessage = "HDC Ads Settings has changes that are not saved. Save them before closing?";
            jsonWidth = EditorPrefs.GetFloat(WidthKey, 0f);
            if (open == null || open.Length != CardCount)
                open = new bool[CardCount];
            if (variant == null || variant.Length != 2)
                variant = new int[2];
        }

        private void OnFocus()
        {
            if (working != null && !Sections.Any(section => SectionChanged(section.Property)))
                Release();
        }

        private void OnDestroy() => Release();

        private void Release()
        {
            if (working != null)
                DestroyImmediate(working);
            working = null;
            settings = null;
            original = null;
        }

        private void OnInspectorUpdate()
        {
            if (asset == null)
                Repaint();
        }

        private void NoAsset()
        {
            string path = HDCAdsSettingsAsset.DefaultPath;
            bool there = HDCAssetFiles.Exists(path);
            bool busy = EditorApplication.isCompiling || EditorApplication.isUpdating;
            EditorGUILayout.Space(8f);
            if (there)
                EditorGUILayout.HelpBox(path + " is there but Unity cannot load it right now, for example while it imports. Nothing was changed; the settings show up here once Unity can load it.",
                    MessageType.Warning);
            else if (busy)
                EditorGUILayout.HelpBox("Unity is compiling or importing. The settings show up here once it ends.", MessageType.Info);
            else
                EditorGUILayout.HelpBox("There is no HDCAdsSettings asset yet. Create it to edit the default configs.", MessageType.Info);
            using (new EditorGUI.DisabledScope(there || busy))
            {
                if (GUILayout.Button("Create " + path, GUILayout.Height(28f)))
                    asset = HDCAdsSettingsAsset.CreateAt(path);
            }
        }

        private void OnGUI()
        {
            if (asset == null)
                asset = HDCAdsSettingsAsset.Load();
            if (asset == null)
            {
                NoAsset();
                return;
            }

            if (working == null)
            {
                working = Instantiate(asset);
                working.hideFlags = HideFlags.HideAndDontSave;
                settings = null;
            }

            if (settings == null || settings.targetObject != working)
                settings = new SerializedObject(working);
            if (original == null || original.targetObject != asset)
                original = new SerializedObject(asset);
            settings.Update();
            original.Update();
            Styles();
            if (Event.current.type == EventType.MouseMove)
                Repaint();
            Toolbar();
            SourceRow();
            scroll = EditorGUILayout.BeginScrollView(scroll);
            GUILayout.Space(6f);
            for (int card = 0; card < CardCount; card++)
                Card(card);
            GUILayout.Space(12f);
            EditorGUILayout.EndScrollView();
            hasUnsavedChanges = UnsavedCount() > 0;
            if (focus >= 0 && Event.current.type == EventType.Repaint)
            {
                scroll.y = Mathf.Max(0f, tops[focus] - 6f);
                focus = -1;
                Repaint();
            }
        }

        private void Toolbar()
        {
            Rect bar = EditorGUILayout.BeginHorizontal(barStyle, GUILayout.Height(36f));
            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(bar, Bar);
                float segment = bar.width / Accents.Length;
                for (int i = 0; i < Accents.Length; i++)
                    EditorGUI.DrawRect(new Rect(bar.x + segment * i, bar.yMax - 3f, segment, 3f), Accents[i]);
            }

            GUILayout.Label(WindowTitle, barTitleStyle);
            PlatformBadge();
            GUILayout.Label(new GUIContent(AssetDatabase.GetAssetPath(asset), AssetDatabase.GetAssetPath(asset)), barPathStyle, GUILayout.MinWidth(30f),
                GUILayout.ExpandWidth(true));
            int unsaved = UnsavedCount();
            using (new EditorGUI.DisabledScope(unsaved == 0))
            {
                if (BarButton(unsaved > 0 ? "Save all (" + unsaved + ")" : "Save all", Green, 100f,
                        "Saves every changed JSON that is valid, plus the config source, the custom keys and the country mode."))
                    ShowNotification(new GUIContent(SaveAll().Count == 0 ? "Saved" : "Saved; the JSON with errors stay unsaved"), 1.5d);
            }

            if (BarButton("Expand all", Blue, 84f, "Opens the 4 cards."))
                SetAll(true);
            if (BarButton("Collapse all", Purple, 90f, "Closes the 4 cards."))
                SetAll(false);
            using (new EditorGUI.DisabledScope(jsonWidth <= 0f))
            {
                if (BarButton("Full width", Teal, 80f, "Text boxes take the whole window width again."))
                    SetWidth(0f);
            }

            if (BarButton("Select asset", Orange, 90f, "Selects HDCAdsSettings in the Project window."))
            {
                Selection.activeObject = asset;
                EditorGUIUtility.PingObject(asset);
            }

            GUILayout.Space(4f);
            EditorGUILayout.EndHorizontal();
        }

        private void SourceRow()
        {
            string property = platform == IosPlatform ? SourceIosProperty : SourceAndroidProperty;
            SerializedProperty source = settings.FindProperty(property);
            if (source == null)
                return;
            bool remote = source.intValue != (int)HDCAdsConfigSource.Default;
            GUILayout.Space(6f);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(8f);
                GUILayout.Label(new GUIContent("Config Source (" + Platforms[platform] + ")",
                    "Where the ads_config, the ad core and the custom keys of a build come from. Each platform has its own choice."), labelStyle,
                    GUILayout.Width(EditorGUIUtility.labelWidth - 4f));
                if (Tab("Remote Config", remote, Green, remote ? Green : Gray))
                    SetSource(source, HDCAdsConfigSource.RemoteConfig);
                if (Tab("Default", !remote, Amber, remote ? Gray : Amber))
                    SetSource(source, HDCAdsConfigSource.Default);
                GUILayout.FlexibleSpace();
                if (SectionChanged(property))
                {
                    Rect pill = GUILayoutUtility.GetRect(130f, 22f, GUILayout.Width(130f), GUILayout.Height(22f));
                    if (Event.current.type == EventType.Repaint)
                        Pill(pill, "Unsaved changes", Amber);
                    GUILayout.Space(14f);
                }
            }

            GUILayout.Label(remote
                ? "Every launch fetches Remote Config and waits up to " + HDCAdsSettings.RemoteConfigTimeoutSeconds.ToString("0") +
                  " s of play time. A key that does not come from Remote Config in that launch uses the default below. Nothing is kept from earlier launches."
                : "No Remote Config fetch: every launch uses the defaults below.", helpStyle);
        }

        private void SetSource(SerializedProperty source, HDCAdsConfigSource value)
        {
            if (source.intValue == (int)value)
                return;
            source.intValue = (int)value;
            settings.ApplyModifiedProperties();
            GUI.FocusControl(null);
        }

        private void PlatformBadge()
        {
            string name = Platforms[platform];
            float width = barButtonStyle.CalcSize(new GUIContent(name)).x + 18f;
            Rect rect = GUILayoutUtility.GetRect(width, 20f, GUILayout.Width(width), GUILayout.Height(24f));
            rect.y += 2f;
            rect.height = 20f;
            string tip = "Showing the " + name + " configs, picked from " + (platformSource.Length > 0 ? platformSource : "the active build target") +
                         ". Open HDC Ads Settings from the other tab of the Build Tool, or switch the platform, to edit the other one.";
            if (Event.current.type == EventType.Repaint)
            {
                Color color = platform == IosPlatform ? Sky : Green;
                EditorGUI.DrawRect(rect, new Color(color.r, color.g, color.b, 0.9f));
                GUI.Label(rect, new GUIContent(name, tip), barButtonStyle);
            }
            else
            {
                GUI.Label(rect, new GUIContent("", tip));
            }

            GUILayout.Space(6f);
        }

        private bool BarButton(string text, Color color, float width, string tooltip)
        {
            Rect rect = GUILayoutUtility.GetRect(width, 24f, GUILayout.Width(width), GUILayout.Height(24f));
            Event current = Event.current;
            bool enabled = GUI.enabled;
            bool hover = enabled && rect.Contains(current.mousePosition);
            if (current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(rect, new Color(color.r, color.g, color.b, !enabled ? 0.22f : hover ? 1f : 0.8f));
                GUI.Label(rect, new GUIContent(text, tooltip), enabled ? barButtonStyle : barButtonOffStyle);
            }

            if (!enabled)
                return false;
            EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);
            if (current.type != EventType.MouseDown || current.button != 0 || !rect.Contains(current.mousePosition))
                return false;
            current.Use();
            return true;
        }

        private int UnsavedCount() =>
            Enumerable.Range(0, Pages.Length).Count(IsChanged) + Sections.Count(section => SectionChanged(section.Property));

        private bool SectionChanged(string property) =>
            settings != null && original != null && settings.targetObject != null && original.targetObject != null &&
            !SerializedProperty.DataEquals(settings.FindProperty(property), original.FindProperty(property));

        private List<string> SaveAll()
        {
            GUI.FocusControl(null);
            var saved = new List<string>();
            var kept = new List<string>();
            for (int page = 0; page < Pages.Length; page++)
            {
                if (!IsChanged(page))
                    continue;
                if (!Check(page, drafts[page]).IsValid)
                {
                    kept.Add(Pages[page].Title);
                    continue;
                }

                original.FindProperty(Pages[page].Property).stringValue = drafts[page].Trim();
                settings.FindProperty(Pages[page].Property).stringValue = drafts[page].Trim();
                drafts[page] = null;
                saved.Add(Pages[page].Title);
            }

            foreach ((string property, string title) in Sections)
            {
                if (!SectionChanged(property))
                    continue;
                original.CopyFromSerializedProperty(settings.FindProperty(property));
                saved.Add(title);
            }

            original.ApplyModifiedPropertiesWithoutUndo();
            settings.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssetIfDirty(asset);
            if (saved.Count > 0)
                Debug.Log("[HDCAds] Saved " + string.Join(", ", saved) + " to " + AssetDatabase.GetAssetPath(asset) + ".");
            if (kept.Count > 0)
                Debug.LogWarning("[HDCAds] Not saved, the JSON has errors: " + string.Join(", ", kept) + ".");
            return kept;
        }

        private void Card(int card)
        {
            Color accent = Accents[card];
            (string status, Color color) = Status(card);
            Rect header = GUILayoutUtility.GetRect(1f, 28f, GUILayout.ExpandWidth(true));
            header.xMin += 6f;
            header.xMax -= 6f;
            Event current = Event.current;
            if (current.type == EventType.Repaint)
            {
                tops[card] = header.y;
                EditorGUI.DrawRect(header, Panel);
                EditorGUI.DrawRect(header, new Color(accent.r, accent.g, accent.b, open[card] ? 0.22f : 0.12f));
                EditorGUI.DrawRect(new Rect(header.x, header.y, 4f, header.height), accent);
                GUI.Label(new Rect(header.x + 14f, header.y, 20f, header.height), open[card] ? "▼" : "▶", labelStyle);
                GUI.Label(new Rect(header.x + 32f, header.y, header.width - 220f, header.height), CardTitles[card], titleStyle);
                Pill(new Rect(header.xMax - 196f, header.y + 6f, 188f, 16f), status, color);
            }
            else if (current.type == EventType.MouseDown && current.button == 0 && header.Contains(current.mousePosition))
            {
                open[card] = !open[card];
                GUI.FocusControl(null);
                current.Use();
            }

            if (!open[card])
            {
                GUILayout.Space(6f);
                return;
            }

            Rect body = EditorGUILayout.BeginVertical(boxStyle);
            if (Event.current.type == EventType.Repaint)
            {
                var area = new Rect(body.x + 6f, body.y, body.width - 12f, body.height);
                EditorGUI.DrawRect(area, Panel);
                EditorGUI.DrawRect(new Rect(area.x, area.y, 4f, area.height), new Color(accent.r, accent.g, accent.b, 0.55f));
            }

            GUILayout.Label(CardHelp[card], helpStyle);
            if (card == KeysCard)
                KeysBody();
            else if (card == CountryCard)
                CountryBody();
            else
                JsonBody(card);
            GUILayout.Space(8f);
            EditorGUILayout.EndVertical();
            GUILayout.Space(8f);
        }

        private void JsonBody(int card)
        {
            Color accent = Accents[card];
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(Platforms[platform] + " JSON", labelStyle, GUILayout.Width(EditorGUIUtility.labelWidth - 4f));
                for (int v = 0; v < Variants.Length; v++)
                {
                    int index = v;
                    if (Tab(Variants[v], variant[card] == v, accent, Dot(PageOf(card, platform, v))))
                        Select(() => variant[card] = index);
                }

                GUILayout.FlexibleSpace();
            }

            int page = PageOf(card, platform, variant[card]);
            string help = Pages[page].Help;
            if (variant[card] == 1)
                help += " " + CountryModeLine();
            GUILayout.Space(4f);
            GUILayout.Label(help, helpStyle);
            Json(page);
        }

        private void Json(int page)
        {
            string text = drafts[page] ?? Saved(page);
            string edited = TextBox(page, text);
            if (edited != text)
                drafts[page] = edited;
            HDCConfigJson.Result check = Check(page, edited);
            EditorGUILayout.HelpBox(check.Message, check.IsValid ? MessageType.Info : MessageType.Error);
            bool changed = IsChanged(page);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                using (new EditorGUI.DisabledScope(!check.IsValid || string.IsNullOrWhiteSpace(edited)))
                {
                    if (Colored("Format", Sky, 78f))
                        Replace(page, HDCConfigJson.Format(edited));
                }

                using (new EditorGUI.DisabledScope(!changed))
                {
                    if (Colored("Revert", Amber, 78f))
                        Replace(page, null);
                }

                using (new EditorGUI.DisabledScope(!changed || !check.IsValid))
                {
                    if (Colored("Save", Green, 78f))
                        Save(page, edited);
                }
            }
        }

        private void KeysBody()
        {
            SerializedProperty keys = settings.FindProperty("customKeys");
            float room = position.width - 60f;
            float used = 0f;
            EditorGUILayout.BeginHorizontal();
            for (int i = 0; i < keys.arraySize; i++)
            {
                SerializedProperty entry = keys.GetArrayElementAtIndex(i);
                string name = entry.FindPropertyRelative("key").stringValue;
                string label = string.IsNullOrWhiteSpace(name) ? "(no name)" : name;
                float width = TabWidth(label);
                if (used > 0f && used + width > room)
                {
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    used = 0f;
                }

                used += width + 2f;
                int index = i;
                if (Tab(label, selectedKey == i, Teal, KeyDot(entry)))
                    Select(() => selectedKey = index);
            }

            if (used > 0f && used + 80f > room)
            {
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
            }

            Color oldBack = GUI.backgroundColor;
            GUI.backgroundColor = Teal;
            bool add = GUILayout.Button(new GUIContent("+ Add key", "Adds an empty custom key."), GUILayout.Width(78f), GUILayout.Height(22f));
            GUI.backgroundColor = oldBack;
            if (add)
            {
                keys.arraySize++;
                SerializedProperty added = keys.GetArrayElementAtIndex(keys.arraySize - 1);
                added.FindPropertyRelative("key").stringValue = "new_key_" + keys.arraySize;
                foreach (string field in ValueFields)
                    added.FindPropertyRelative(field).stringValue = "";
                settings.ApplyModifiedProperties();
                int last = keys.arraySize - 1;
                Select(() => selectedKey = last);
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            foreach (string problem in Problems(settings))
                EditorGUILayout.HelpBox(problem, MessageType.Warning);
            if (keys.arraySize == 0)
            {
                GUILayout.Label("No custom keys yet.", helpStyle);
                return;
            }

            selectedKey = Mathf.Clamp(selectedKey, 0, keys.arraySize - 1);
            SerializedProperty item = keys.GetArrayElementAtIndex(selectedKey);
            GUILayout.Space(6f);
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUI.BeginChangeCheck();
                string name = EditorGUILayout.DelayedTextField(new GUIContent("Key", "Remote Config key: letters, digits and underscores."),
                    item.FindPropertyRelative("key").stringValue);
                if (EditorGUI.EndChangeCheck())
                {
                    item.FindPropertyRelative("key").stringValue = name.Trim();
                    settings.ApplyModifiedProperties();
                }

                if (Colored("Remove", Red, 70f) &&
                    EditorUtility.DisplayDialog("Remove custom key", $"Remove the custom key '{item.FindPropertyRelative("key").stringValue}'?", "Remove", "Cancel"))
                {
                    keys.DeleteArrayElementAtIndex(selectedKey);
                    settings.ApplyModifiedProperties();
                    selectedKey = Mathf.Max(0, selectedKey - 1);
                    GUIUtility.ExitGUI();
                }
            }

            GUILayout.Space(4f);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("Value", labelStyle, GUILayout.Width(EditorGUIUtility.labelWidth - 4f));
                valueTab = Mathf.Clamp(valueTab, 0, 1);
                for (int t = 0; t < 2; t++)
                {
                    int tab = t;
                    bool filled = !string.IsNullOrWhiteSpace(item.FindPropertyRelative(ValueField(t)).stringValue);
                    if (Tab(t == 0 ? Platforms[platform] : "Country", valueTab == t, Teal, filled ? Green : Gray))
                        Select(() => valueTab = tab);
                }

                GUILayout.FlexibleSpace();
            }

            GUILayout.Label(ValueHelp[valueTab == 1 ? 2 : platform], helpStyle);
            SerializedProperty value = item.FindPropertyRelative(ValueField(valueTab));
            string edited = TextBox(KeyBox, value.stringValue);
            if (edited != value.stringValue)
            {
                value.stringValue = edited;
                settings.ApplyModifiedProperties();
            }

            SaveRow(KeysProperty);
        }

        private void CountryBody()
        {
            SerializedProperty country = settings.FindProperty("countryCheck");
            EditorGUI.BeginChangeCheck();
            Toggle(country, "enabled", "Enabled");
            Text(country, "targetCountry", "Target country");
            List(country, "systemLanguages", "System languages");
            List(country, "languageCodes", "Language codes");
            Toggle(country, "matchRegion", "Match region");
            List(country, "timezoneNames", "Time zone names");
            Numbers(country, "utcOffsets", "UTC offsets");
            Toggle(country, "matchSimCountry", "Match SIM country");
            Toggle(country, "matchNetworkCountry", "Match network country");
            List(country, "debugDevices", "Debug devices");
            Toggle(country, "simulateInEditor", "Simulate in Editor");
            if (EditorGUI.EndChangeCheck())
                settings.ApplyModifiedProperties();
            GUILayout.Label("Lists are comma separated. The Country JSON of each config is in the Ads config and Ad core cards (JSON > Country).", helpStyle);
            if (country.FindPropertyRelative("enabled").boolValue && string.IsNullOrWhiteSpace(settings.FindProperty("countryAdsConfigAndroid").stringValue))
            {
                EditorGUILayout.HelpBox("Country mode is on but the Android Country JSON of Ads config is empty: devices in the country keep the usual ads_config.",
                    MessageType.Warning);
            }

            SaveRow(CountryProperty);
        }

        private static void Toggle(SerializedProperty parent, string field, string label)
        {
            SerializedProperty property = parent.FindPropertyRelative(field);
            property.boolValue = EditorGUILayout.Toggle(new GUIContent(label, property.tooltip), property.boolValue);
        }

        private static void Text(SerializedProperty parent, string field, string label)
        {
            SerializedProperty property = parent.FindPropertyRelative(field);
            property.stringValue = EditorGUILayout.DelayedTextField(new GUIContent(label, property.tooltip), property.stringValue).Trim();
        }

        private static void List(SerializedProperty parent, string field, string label)
        {
            SerializedProperty property = parent.FindPropertyRelative(field);
            var values = new List<string>();
            for (int i = 0; i < property.arraySize; i++)
                values.Add(property.GetArrayElementAtIndex(i).stringValue);
            string joined = string.Join(", ", values);
            string next = EditorGUILayout.DelayedTextField(new GUIContent(label, property.tooltip), joined);
            if (next == joined)
                return;
            string[] parts = Split(next);
            property.arraySize = parts.Length;
            for (int i = 0; i < parts.Length; i++)
                property.GetArrayElementAtIndex(i).stringValue = parts[i];
        }

        private static void Numbers(SerializedProperty parent, string field, string label)
        {
            SerializedProperty property = parent.FindPropertyRelative(field);
            var values = new List<string>();
            for (int i = 0; i < property.arraySize; i++)
                values.Add(property.GetArrayElementAtIndex(i).floatValue.ToString(CultureInfo.InvariantCulture));
            string joined = string.Join(", ", values);
            string next = EditorGUILayout.DelayedTextField(new GUIContent(label, property.tooltip), joined);
            if (next == joined)
                return;
            float[] parts = Split(next)
                .Select(part => float.TryParse(part, NumberStyles.Float, CultureInfo.InvariantCulture, out float number) ? (float?)number : null)
                .Where(number => number.HasValue).Select(number => number.Value).ToArray();
            property.arraySize = parts.Length;
            for (int i = 0; i < parts.Length; i++)
                property.GetArrayElementAtIndex(i).floatValue = parts[i];
        }

        private string TextBox(int slot, string text)
        {
            Rect row = GUILayoutUtility.GetRect(1f, boxHeights[slot], GUILayout.ExpandWidth(true));
            row.xMin += 4f;
            float full = Mathf.Max(MinJsonWidth, row.width - HandleWidth);
            float width = jsonWidth <= 0f ? full : Mathf.Clamp(jsonWidth, MinJsonWidth, full);
            float textHeight = EditorStyles.textArea.CalcHeight(new GUIContent(text), width - 16f);
            float height = Mathf.Clamp(textHeight + 8f, 96f, MaxBoxHeight);
            if (Event.current.type == EventType.Repaint && !Mathf.Approximately(height, boxHeights[slot]))
            {
                boxHeights[slot] = height;
                Repaint();
            }

            var box = new Rect(row.x, row.y, width, row.height);
            var content = new Rect(0f, 0f, width - 16f, Mathf.Max(row.height - 4f, textHeight + 4f));
            boxScrolls[slot] = GUI.BeginScrollView(box, boxScrolls[slot], content);
            string edited = EditorGUI.TextArea(content, text, EditorStyles.textArea);
            GUI.EndScrollView();
            ResizeHandle(new Rect(box.xMax, row.y, HandleWidth, row.height), width, full);
            return edited;
        }

        private void ResizeHandle(Rect handle, float width, float full)
        {
            int id = GUIUtility.GetControlID(FocusType.Passive);
            Event current = Event.current;
            EditorGUIUtility.AddCursorRect(handle, MouseCursor.ResizeHorizontal);
            switch (current.GetTypeForControl(id))
            {
                case EventType.MouseDown:
                    if (current.button != 0 || !handle.Contains(current.mousePosition))
                        break;
                    if (current.clickCount == 2)
                    {
                        SetWidth(0f);
                    }
                    else
                    {
                        GUIUtility.hotControl = id;
                        dragFrom = current.mousePosition.x;
                        dragWidth = width;
                    }

                    current.Use();
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl != id)
                        break;
                    float wanted = Mathf.Clamp(dragWidth + current.mousePosition.x - dragFrom, MinJsonWidth, full);
                    jsonWidth = wanted >= full - 1f ? 0f : wanted;
                    current.Use();
                    Repaint();
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl != id)
                        break;
                    GUIUtility.hotControl = 0;
                    SetWidth(jsonWidth);
                    current.Use();
                    break;
                case EventType.Repaint:
                    Color grip = GUIUtility.hotControl == id ? Teal : new Color(Gray.r, Gray.g, Gray.b, 0.6f);
                    EditorGUI.DrawRect(new Rect(handle.x + handle.width / 2f - 2f, handle.y + 8f, 1f, handle.height - 16f), grip);
                    EditorGUI.DrawRect(new Rect(handle.x + handle.width / 2f + 1f, handle.y + 8f, 1f, handle.height - 16f), grip);
                    GUI.Label(handle, new GUIContent("", "Drag to make the text boxes wider or narrower. Double-click: full width."));
                    break;
            }
        }

        private bool Tab(string label, bool selected, Color accent, Color dot)
        {
            float width = TabWidth(label);
            Rect rect = GUILayoutUtility.GetRect(width, 22f, GUILayout.Width(width), GUILayout.Height(22f));
            Event current = Event.current;
            if (current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(rect, selected ? new Color(accent.r, accent.g, accent.b, 0.30f) : TabBack);
                if (selected)
                    EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 2f, rect.width, 2f), accent);
                EditorGUI.DrawRect(new Rect(rect.x + 9f, rect.center.y - 3f, 6f, 6f), dot);
                GUI.Label(new Rect(rect.x + 20f, rect.y, rect.width - 22f, rect.height), label, selected ? tabOnStyle : tabStyle);
            }

            EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);
            if (current.type != EventType.MouseDown || current.button != 0 || !rect.Contains(current.mousePosition))
                return false;
            current.Use();
            return true;
        }

        private float TabWidth(string label) => Mathf.Max(72f, tabStyle.CalcSize(new GUIContent(label)).x + 30f);

        private void Pill(Rect rect, string text, Color color)
        {
            Vector2 size = pillStyle.CalcSize(new GUIContent(text));
            var back = new Rect(rect.xMax - size.x - 14f, rect.y, size.x + 14f, rect.height);
            EditorGUI.DrawRect(back, new Color(color.r, color.g, color.b, 0.22f));
            Color old = GUI.color;
            GUI.color = color;
            GUI.Label(back, text, pillStyle);
            GUI.color = old;
        }

        private static bool Colored(string text, Color color, float width)
        {
            Color old = GUI.backgroundColor;
            GUI.backgroundColor = color;
            bool clicked = GUILayout.Button(text, GUILayout.Width(width));
            GUI.backgroundColor = old;
            return clicked;
        }

        private void SaveRow(string property)
        {
            bool changed = SectionChanged(property);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                using (new EditorGUI.DisabledScope(!changed))
                {
                    if (Colored("Revert", Amber, 78f))
                    {
                        GUI.FocusControl(null);
                        settings.CopyFromSerializedProperty(original.FindProperty(property));
                        settings.ApplyModifiedPropertiesWithoutUndo();
                    }

                    if (Colored("Save", Green, 78f))
                    {
                        GUI.FocusControl(null);
                        original.CopyFromSerializedProperty(settings.FindProperty(property));
                        original.ApplyModifiedPropertiesWithoutUndo();
                        EditorUtility.SetDirty(asset);
                        AssetDatabase.SaveAssetIfDirty(asset);
                    }
                }
            }
        }

        private Color Dot(int page)
        {
            string text = drafts[page] ?? Saved(page);
            return JsonStatus(text, Check(page, text), IsChanged(page)).Color;
        }

        private Color KeyDot(SerializedProperty item) =>
            new[] { ValueField(0), ValueField(1) }.Any(field => !string.IsNullOrWhiteSpace(item.FindPropertyRelative(field).stringValue)) ? Green : Gray;

        private string ValueField(int tab) => tab == 1 ? "country" : ValueFields[platform];

        private string CountryModeLine()
        {
            SerializedProperty check = settings.FindProperty("countryCheck");
            return check.FindPropertyRelative("enabled").boolValue
                ? "Country mode is on for " + check.FindPropertyRelative("targetCountry").stringValue + "."
                : "Country mode is off (Country mode card), so this JSON is not used yet.";
        }

        private (string Text, Color Color) Status(int card)
        {
            if (card == KeysCard)
                return SectionChanged(KeysProperty) ? ("Unsaved changes", Amber) : KeysStatus(settings);
            if (card == CountryCard)
                return SectionChanged(CountryProperty) ? ("Unsaved changes", Amber) : CountryStatus(settings);
            return JsonCardStatus(card, platform, page => drafts[page] ?? Saved(page), IsChanged, Check);
        }

        private static (string Text, Color Color) KeysStatus(SerializedObject settings)
        {
            int problems = Problems(settings).Count();
            if (problems > 0)
                return (problems == 1 ? "1 problem" : problems + " problems", Amber);
            int count = settings.FindProperty("customKeys").arraySize;
            return (count == 0 ? "No keys" : count == 1 ? "1 key" : count + " keys", count == 0 ? Gray : Green);
        }

        private static (string Text, Color Color) CountryStatus(SerializedObject settings)
        {
            SerializedProperty country = settings.FindProperty("countryCheck");
            if (!country.FindPropertyRelative("enabled").boolValue)
                return ("Off", Gray);
            string code = country.FindPropertyRelative("targetCountry").stringValue;
            return (code.Length > 0 ? "On · " + code : "On", Green);
        }

        private static IEnumerable<string> Problems(SerializedObject settings)
        {
            SerializedProperty keys = settings.FindProperty("customKeys");
            var names = new List<string>();
            for (int i = 0; i < keys.arraySize; i++)
                names.Add(keys.GetArrayElementAtIndex(i).FindPropertyRelative("key").stringValue);
            return HDCAdsSettings.CustomKeyProblems(names);
        }

        private static HDCConfigJson.Result CheckText(int page, string text) =>
            page >= CountryAdsAndroidPage && string.IsNullOrWhiteSpace(text)
                ? new HDCConfigJson.Result(true, "Empty: devices in the target country keep the usual config.")
                : HDCConfigJson.Check(text, Pages[page].Kind, Pages[page].MayBeEmpty);

        private HDCConfigJson.Result Check(int page, string text)
        {
            if (checkedTexts[page] == text)
                return checks[page];
            checks[page] = CheckText(page, text);
            checkedTexts[page] = text;
            return checks[page];
        }

        private void Select(Action change)
        {
            change();
            GUI.FocusControl(null);
            Repaint();
        }

        private void SetAll(bool value)
        {
            for (int i = 0; i < open.Length; i++)
                open[i] = value;
            GUI.FocusControl(null);
        }

        private void SetWidth(float width)
        {
            jsonWidth = width;
            EditorPrefs.SetFloat(WidthKey, width);
            Repaint();
        }

        private bool IsChanged(int page) => drafts[page] != null && original != null && drafts[page] != Saved(page);

        private string Saved(int page) => original.FindProperty(Pages[page].Property).stringValue;

        private void Replace(int page, string draft)
        {
            GUI.FocusControl(null);
            drafts[page] = draft;
        }

        private void Save(int page, string text)
        {
            original.FindProperty(Pages[page].Property).stringValue = text.Trim();
            original.ApplyModifiedPropertiesWithoutUndo();
            settings.FindProperty(Pages[page].Property).stringValue = text.Trim();
            settings.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssetIfDirty(asset);
            Replace(page, null);
            Debug.Log($"[HDCAds] Saved {Pages[page].Title} to {AssetDatabase.GetAssetPath(asset)}.");
        }

        private void Styles()
        {
            if (titleStyle != null)
                return;
            titleStyle = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleLeft, fontSize = 12 };
            labelStyle = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleLeft };
            helpStyle = new GUIStyle(EditorStyles.wordWrappedMiniLabel) { margin = new RectOffset(4, 4, 2, 6) };
            helpStyle.normal.textColor = Gray;
            pillStyle = new GUIStyle(EditorStyles.miniBoldLabel) { alignment = TextAnchor.MiddleCenter };
            tabStyle = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleLeft };
            tabOnStyle = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleLeft };
            boxStyle = new GUIStyle { padding = new RectOffset(18, 14, 8, 4), margin = new RectOffset(0, 0, 0, 0) };
            barStyle = new GUIStyle { padding = new RectOffset(10, 6, 5, 7), margin = new RectOffset(0, 0, 0, 0) };
            barTitleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 13, alignment = TextAnchor.MiddleLeft, fixedHeight = 24f };
            barPathStyle = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleLeft, fixedHeight = 24f, clipping = TextClipping.Clip };
            barPathStyle.normal.textColor = Gray;
            barButtonStyle = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter, fontSize = 11 };
            barButtonStyle.normal.textColor = Color.white;
            barButtonOffStyle = new GUIStyle(barButtonStyle);
            barButtonOffStyle.normal.textColor = new Color(1f, 1f, 1f, 0.35f);
        }

        private readonly struct Page
        {
            internal Page(string title, string property, HDCConfigJson.Kind kind, bool mayBeEmpty, string help)
            {
                Title = title;
                Property = property;
                Kind = kind;
                MayBeEmpty = mayBeEmpty;
                Help = help;
            }

            internal string Title { get; }
            internal string Property { get; }
            internal HDCConfigJson.Kind Kind { get; }
            internal bool MayBeEmpty { get; }
            internal string Help { get; }
        }
    }
}
