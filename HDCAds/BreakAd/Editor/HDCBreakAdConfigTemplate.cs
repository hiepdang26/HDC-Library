using System;
using UnityEditor;
using UnityEngine;

namespace HDC.Ads.Editor
{
    internal static class HDCBreakAdConfigTemplate
    {
        internal const string Key = "break_ads_config";
        internal const string AndroidTestUnit = "ca-app-pub-3940256099942544/2247696110";
        internal const string IosTestUnit = "ca-app-pub-3940256099942544/3986624511";

        internal static void AddToDefaults()
        {
            HDCAdsSettings settings = FindSettings();
            if (settings == null)
            {
                Debug.LogWarning("[HDCAds] No HDCAdsSettings asset yet: open HDC > Settings asset once, then add the break ad config again.");
                return;
            }

            var serialized = new SerializedObject(settings);
            SerializedProperty android = serialized.FindProperty("coreConfigAndroid");
            SerializedProperty ios = serialized.FindProperty("coreConfigIos");
            string added = "";
            added += Add(android, AndroidTestUnit, "Android");
            if (!string.IsNullOrWhiteSpace(ios.stringValue))
                added += Add(ios, IosTestUnit, "iOS");
            serialized.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
            Selection.activeObject = settings;
            Debug.Log(added.Length == 0
                ? "[HDCAds] The default ad core configs already have break_ads_config, or are not valid JSON: edit them in HDC > Edit configs."
                : "[HDCAds] Added break_ads_config to the default ad core config of" + added + ", with Google's test native unit, a break every 60 s " +
                  "and a 5 s countdown. Put the same block in adcore_main_ios and adcore_main_android on Remote Config, with the real ad unit.");
        }

        internal static string WithBreakAd(string coreJson, string adUnitId)
        {
            string json = (coreJson ?? "").Trim();
            if (json.IndexOf("\"" + Key + "\"", StringComparison.Ordinal) >= 0)
                return null;
            if (json.Length == 0)
                json = "{}";
            int end = json.LastIndexOf('}');
            if (!json.StartsWith("{", StringComparison.Ordinal) || end < 0)
                return null;

            string head = json.Substring(0, end).TrimEnd();
            string separator = head.EndsWith("{", StringComparison.Ordinal) ? "\n  " : ",\n  ";
            return head + separator + Block(adUnitId, FirstLayoutGroup(json)) + "\n}";
        }

        internal static string Block(string adUnitId, string layoutGroup) =>
            "\"" + Key + "\": {\n" +
            "    \"adUnitId\": \"" + adUnitId + "\",\n" +
            "    \"layoutGroup\": \"" + layoutGroup + "\",\n" +
            "    \"showBreakAdTime\": 60,\n" +
            "    \"notiBreakAdTime\": 5\n" +
            "  }";

        private static string Add(SerializedProperty core, string adUnitId, string platform)
        {
            string updated = WithBreakAd(core.stringValue, adUnitId);
            if (updated == null)
                return "";
            core.stringValue = updated;
            return " " + platform;
        }

        private static string FirstLayoutGroup(string json)
        {
            try
            {
                LayoutGroups[] groups = JsonUtility.FromJson<Core>(json)?.forceAdLayoutConfig?.layoutGroups;
                return groups != null && groups.Length > 0 ? groups[0]?.groupName ?? "" : "";
            }
            catch (ArgumentException)
            {
                return "";
            }
        }

        private static HDCAdsSettings FindSettings()
        {
            HDCAdsSettings found = null;
            foreach (string guid in AssetDatabase.FindAssets("t:" + nameof(HDCAdsSettings)))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var settings = AssetDatabase.LoadAssetAtPath<HDCAdsSettings>(path);
                if (settings == null)
                    continue;
                if (path.EndsWith("/Resources/" + HDCAdsSettings.ResourceName + ".asset", StringComparison.Ordinal))
                    return settings;
                if (found == null)
                    found = settings;
            }

            return found;
        }

#pragma warning disable 0649
        [Serializable]
        private sealed class Core
        {
            public Layouts forceAdLayoutConfig;
        }

        [Serializable]
        private sealed class Layouts
        {
            public LayoutGroups[] layoutGroups;
        }

        [Serializable]
        private sealed class LayoutGroups
        {
            public string groupName;
        }
#pragma warning restore 0649
    }
}
