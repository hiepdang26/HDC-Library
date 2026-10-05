using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HDC.Ads.Editor
{
    internal sealed class HDCAdsConfigWindow : EditorWindow
    {
        internal const int AdsAndroidPage = 0;
        internal const int CoreAndroidPage = 2;
        internal const int CoreIosPage = 3;
        internal const int CustomKeysPage = 8;
        internal const int CountryCheckPage = 9;
        internal const int CountryAdsAndroidPage = 4;

        private const string CustomKeysTitle = "Custom keys";
        private const string CountryCheckTitle = "Country check";
        private const int TabsPerRow = 5;

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
            new Page("Country ads (Android)", "countryAdsConfigAndroid", HDCConfigJson.Kind.Ads, true,
                "ads_config a device in the target country gets in country mode, on Android, in place of Remote Config. Leave it empty to keep the usual ads_config."),
            new Page("Country ads (iOS)", "countryAdsConfigIos", HDCConfigJson.Kind.Ads, true,
                "ads_config in country mode on iOS. Leave it empty to use the Android one."),
            new Page("Country core (Android)", "countryCoreConfigAndroid", HDCConfigJson.Kind.Core, true,
                "Ad core config in country mode on Android, in place of Remote Config. Leave it empty to keep the usual ad core config."),
            new Page("Country core (iOS)", "countryCoreConfigIos", HDCConfigJson.Kind.Core, true,
                "Ad core config in country mode on iOS. Leave it empty to use the Android one."),
        };

        [SerializeField] private int page;
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

            int selected = GUILayout.SelectionGrid(page, Titles(), TabsPerRow);
            if (selected != page)
            {
                page = selected;
                scroll = Vector2.zero;
                GUI.FocusControl(null);
            }

            if (page == CustomKeysPage)
            {
                DrawCustomKeys();
                return;
            }

            if (page == CountryCheckPage)
            {
                DrawCountryCheck();
                return;
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

        private void OnLostFocus() => SaveCustomKeys();

        private void OnDisable() => SaveCustomKeys();

        private void DrawCustomKeys()
        {
            EditorGUILayout.HelpBox("Remote Config keys of the game itself, read with HDCCustomConfig.Get(key). Each value is used until " +
                                    "Remote Config has one for the key, then the last fetched value is kept on the device. iOS uses the " +
                                    "Android value when its own is empty.", MessageType.None);
            SerializedProperty keys = settings.FindProperty("customKeys");
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(keys, new GUIContent(CustomKeysTitle), true);
            if (EditorGUI.EndChangeCheck())
                settings.ApplyModifiedProperties();
            EditorGUILayout.EndScrollView();

            var names = new List<string>();
            for (int i = 0; i < keys.arraySize; i++)
                names.Add(keys.GetArrayElementAtIndex(i).FindPropertyRelative("key").stringValue);
            bool anyProblem = false;
            foreach (string problem in HDCAdsSettings.CustomKeyProblems(names))
            {
                anyProblem = true;
                EditorGUILayout.HelpBox(problem, MessageType.Warning);
            }

            if (!anyProblem)
                EditorGUILayout.HelpBox(keys.arraySize == 0 ? "No custom keys." : keys.arraySize + " custom keys.", MessageType.Info);

            GUI.enabled = EditorUtility.IsDirty(settings.targetObject);
            if (GUILayout.Button("Save"))
                SaveCustomKeys();
            GUI.enabled = true;
            if (GUILayout.Button("Select settings asset", EditorStyles.miniButton))
                EditorGUIUtility.PingObject(settings.targetObject);
        }

        private void DrawCountryCheck()
        {
            EditorGUILayout.HelpBox("Country mode: a device in the target country gets the Country ads and Country core configs of this " +
                                    "asset, and the country value of each custom key, instead of the Remote Config values. A device counts " +
                                    "as in the country when any of the signals below matches. Devices in debugDevices here or in the Remote " +
                                    "Config key devices ({\"debugDevices\":[...]}) never get it. The Editor gets it only with Simulate In Editor.",
                MessageType.None);
            SerializedProperty country = settings.FindProperty("countryCheck");
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUI.BeginChangeCheck();
            SerializedProperty field = country.Copy();
            SerializedProperty end = country.GetEndProperty();
            if (field.NextVisible(true))
            {
                do
                {
                    if (SerializedProperty.EqualContents(field, end))
                        break;
                    EditorGUILayout.PropertyField(field, true);
                }
                while (field.NextVisible(false));
            }

            if (EditorGUI.EndChangeCheck())
                settings.ApplyModifiedProperties();
            EditorGUILayout.EndScrollView();

            if (country.FindPropertyRelative("enabled").boolValue && string.IsNullOrWhiteSpace(settings.FindProperty("countryAdsConfigAndroid").stringValue))
                EditorGUILayout.HelpBox("Country mode is on but Country ads (Android) is empty: devices in the country keep the usual ads_config.", MessageType.Warning);

            GUI.enabled = EditorUtility.IsDirty(settings.targetObject);
            if (GUILayout.Button("Save"))
                SaveCustomKeys();
            GUI.enabled = true;
        }

        private void SaveCustomKeys()
        {
            if (settings != null && settings.targetObject != null && EditorUtility.IsDirty(settings.targetObject))
                AssetDatabase.SaveAssetIfDirty(settings.targetObject);
        }

        private string[] Titles()
        {
            var titles = new string[Pages.Length + 2];
            for (int i = 0; i < Pages.Length; i++)
                titles[i] = IsChanged(i) ? Pages[i].Title + " *" : Pages[i].Title;
            titles[CustomKeysPage] = CustomKeysTitle;
            titles[CountryCheckPage] = CountryCheckTitle;
            return titles;
        }

        private bool IsChanged(int index) => drafts[index] != null && drafts[index] != Saved(Pages[index]);

        private string Saved(Page page) => settings.FindProperty(page.Property).stringValue;

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
