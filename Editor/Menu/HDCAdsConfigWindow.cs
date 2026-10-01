using UnityEditor;
using UnityEngine;

namespace HDC.Ads.Editor
{
    /// <summary>
    /// Edits the default ads configs kept in HDCAdsSettings: ads_config and the ad core config, for Android and
    /// iOS. A page saves only JSON that parses; unsaved pages are marked with an asterisk.
    /// </summary>
    internal sealed class HDCAdsConfigWindow : EditorWindow
    {
        internal const int AdsAndroidPage = 0;
        internal const int CoreAndroidPage = 2;
        internal const int CoreIosPage = 3;

        private static readonly Page[] Pages =
        {
            new Page("Ads (Android)", "adsConfigAndroid", HDCConfigJson.Kind.Ads, false,
                "Default of the Remote Config key ads_config on Android."),
            new Page("Ads (iOS)", "adsConfigIos", HDCConfigJson.Kind.Ads, true,
                "Default of ads_config on iOS. Leave it empty to use the Android one."),
            new Page("Ad core (Android)", "coreConfigAndroid", HDCConfigJson.Kind.Core, false,
                "Default ad core config on Android, for the key ads_config names in selectedAdCoreName."),
            new Page("Ad core (iOS)", "coreConfigIos", HDCConfigJson.Kind.Core, true,
                "Default ad core config on iOS. Leave it empty to use the Android one."),
        };

        [SerializeField] private int page;
        // Edits not saved yet, per page; null when the page shows the saved text.
        private readonly string[] drafts = new string[Pages.Length];
        private Vector2 scroll;
        private SerializedObject settings;

        internal static void Open(int page)
        {
            var window = GetWindow<HDCAdsConfigWindow>("HDC Ads configs");
            window.minSize = new Vector2(560f, 440f);
            window.page = page;
            window.Show();
        }

        private void OnGUI()
        {
            if (settings == null || settings.targetObject == null)
                settings = new SerializedObject(HDCAdsSettingsAsset.LoadOrCreate());
            settings.Update();

            int selected = GUILayout.Toolbar(page, Titles());
            if (selected != page)
            {
                page = selected;
                scroll = Vector2.zero;
                GUI.FocusControl(null);
            }

            Page current = Pages[page];
            EditorGUILayout.HelpBox(current.Help, MessageType.None);

            string saved = Saved(current);
            string text = drafts[page] ?? saved;
            scroll = EditorGUILayout.BeginScrollView(scroll);
            string edited = EditorGUILayout.TextArea(text, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
            if (edited != text)
                drafts[page] = edited;

            HDCConfigJson.Result check = HDCConfigJson.Check(edited, current.Kind, current.MayBeEmpty);
            EditorGUILayout.HelpBox(check.Message, check.IsValid ? MessageType.Info : MessageType.Error);

            bool changed = IsChanged(page);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.enabled = check.IsValid && !string.IsNullOrWhiteSpace(edited);
                if (GUILayout.Button("Format"))
                    Replace(HDCConfigJson.Format(edited));

                GUI.enabled = changed;
                if (GUILayout.Button("Revert"))
                    Replace(null);

                GUI.enabled = changed && check.IsValid;
                if (GUILayout.Button("Save"))
                    Save(current, edited);

                GUI.enabled = true;
            }

            if (GUILayout.Button("Select settings asset", EditorStyles.miniButton))
                EditorGUIUtility.PingObject(settings.targetObject);
        }

        private string[] Titles()
        {
            var titles = new string[Pages.Length];
            for (int i = 0; i < Pages.Length; i++)
                titles[i] = IsChanged(i) ? Pages[i].Title + " *" : Pages[i].Title;
            return titles;
        }

        private bool IsChanged(int index) => drafts[index] != null && drafts[index] != Saved(Pages[index]);

        private string Saved(Page page) => settings.FindProperty(page.Property).stringValue;

        // Text being edited keeps its own copy until it loses focus.
        private void Replace(string draft)
        {
            GUI.FocusControl(null);
            drafts[page] = draft;
        }

        private void Save(Page page, string text)
        {
            settings.FindProperty(page.Property).stringValue = text.Trim();
            settings.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
            Replace(null);
            Debug.Log($"[HDCAds] Saved {page.Title} config to {AssetDatabase.GetAssetPath(settings.targetObject)}.");
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
