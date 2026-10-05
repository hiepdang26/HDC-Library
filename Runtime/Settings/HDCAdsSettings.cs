using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace HDC.Ads
{
    public sealed class HDCAdsSettings : ScriptableObject
    {
        public const string ResourceName = "HDCAdsSettings";

        public static readonly string[] CoreConfigKeys = { "adcore_main_android", "adcore_main_ios" };

        [SerializeField, TextArea(5, 30)] private string adsConfigAndroid = "{}";
        [SerializeField, TextArea(5, 30)] private string adsConfigIos = "";
        [SerializeField, TextArea(5, 30)] private string coreConfigAndroid = "{}";
        [SerializeField, TextArea(5, 30)] private string coreConfigIos = "";
        [SerializeField] private CustomKey[] customKeys = new CustomKey[0];

        public string AdsConfig => ForPlatform(adsConfigAndroid, adsConfigIos);

        public string CoreConfig => ForPlatform(coreConfigAndroid, coreConfigIos);

        public static HDCAdsSettings Load()
        {
            var settings = Resources.Load<HDCAdsSettings>(ResourceName);
            if (settings != null)
                return settings;

            Debug.LogWarning("[HDCAds] No HDCAdsSettings in a Resources folder: open HDC > Edit configs to create it.");
            return CreateInstance<HDCAdsSettings>();
        }

        public Dictionary<string, string> CoreConfigsByKey()
        {
            var configs = new Dictionary<string, string>();
            foreach (string key in CoreConfigKeys)
                configs[key] = CoreConfig;

            string selected = SelectedCoreName(AdsConfig);
            if (!string.IsNullOrEmpty(selected))
                configs[selected] = CoreConfig;
            return configs;
        }

        internal static IEnumerable<string> CustomKeyProblems(IEnumerable<string> keys)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            int index = 0;
            foreach (string raw in keys)
            {
                index++;
                string name = raw?.Trim() ?? "";
                if (name.Length == 0)
                {
                    yield return $"Custom key #{index} has no name, so it is skipped.";
                    continue;
                }

                if (!seen.Add(name))
                    yield return $"Custom key '{name}' appears more than once: only the first one counts.";
                else if (Array.IndexOf(CoreConfigKeys, name) >= 0 || name == "ads_config")
                    yield return $"Custom key '{name}' is a key HDC itself reads: pick another name.";
                else if (!Regex.IsMatch(name, "^[A-Za-z_][A-Za-z0-9_]*$"))
                    yield return $"Custom key '{name}' is not a valid Remote Config key: use letters, digits and underscores, starting with a letter or an underscore.";
            }
        }

        internal IEnumerable<string> CustomKeyProblems()
        {
            var names = new List<string>();
            foreach (CustomKey custom in customKeys ?? new CustomKey[0])
                names.Add(custom?.key);
            return CustomKeyProblems(names);
        }

        internal Dictionary<string, string> CustomDefaults()
        {
            var defaults = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (CustomKey custom in customKeys ?? new CustomKey[0])
            {
                string name = custom?.key?.Trim();
                if (!string.IsNullOrEmpty(name) && !defaults.ContainsKey(name))
                    defaults[name] = ForPlatform(custom.android ?? "", custom.ios ?? "");
            }

            return defaults;
        }

        private static string ForPlatform(string android, string ios)
        {
#if UNITY_IOS
            return string.IsNullOrWhiteSpace(ios) ? android : ios;
#else
            return android;
#endif
        }

        private static string SelectedCoreName(string adsConfig)
        {
            try
            {
                return JsonUtility.FromJson<AdsConfigHeader>(adsConfig)?.selectedAdCoreName;
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        [Serializable]
        internal sealed class CustomKey
        {
            [Tooltip("Remote Config key: letters, digits and underscores.")]
            public string key = "";

            [Tooltip("Value used on Android until Remote Config has one.")]
            [TextArea(2, 12)]
            public string android = "";

            [Tooltip("Value used on iOS until Remote Config has one. Leave it empty to use the Android value.")]
            [TextArea(2, 12)]
            public string ios = "";
        }

#pragma warning disable 0649
        [Serializable]
        private sealed class AdsConfigHeader
        {
            public string selectedAdCoreName;
        }
#pragma warning restore 0649
    }
}
