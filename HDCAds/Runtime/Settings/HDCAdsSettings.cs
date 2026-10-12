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
        [SerializeField, TextArea(5, 30)] private string countryAdsConfigAndroid = "";
        [SerializeField, TextArea(5, 30)] private string countryAdsConfigIos = "";
        [SerializeField, TextArea(5, 30)] private string countryCoreConfigAndroid = "";
        [SerializeField, TextArea(5, 30)] private string countryCoreConfigIos = "";
        [SerializeField] private CountryCheck countryCheck = new CountryCheck();

        public string AdsConfig => ForPlatform(adsConfigAndroid, adsConfigIos);

        public string CoreConfig => ForPlatform(coreConfigAndroid, coreConfigIos);

        internal string CountryAdsConfig => ForPlatform(countryAdsConfigAndroid, countryAdsConfigIos);

        internal string CountryCoreConfig => ForPlatform(countryCoreConfigAndroid, countryCoreConfigIos);

        internal string CountryRulesJson() => JsonUtility.ToJson(countryCheck ?? new CountryCheck());

        internal static HDCAdsSettings Override { get; set; }

        public static HDCAdsSettings Load()
        {
            if (Override != null)
                return Override;
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

        internal Dictionary<string, string> CustomDefaults() => CustomValues(false);

        internal Dictionary<string, string> CustomCountryValues() => CustomValues(true);

        private Dictionary<string, string> CustomValues(bool country)
        {
            var values = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (CustomKey custom in customKeys ?? new CustomKey[0])
            {
                string name = custom?.key?.Trim();
                if (string.IsNullOrEmpty(name) || values.ContainsKey(name))
                    continue;
                string value = ForPlatform(custom.android ?? "", custom.ios ?? "");
                values[name] = country && !string.IsNullOrWhiteSpace(custom.country) ? custom.country : value;
            }

            return values;
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

            [Tooltip("Value used in country mode, in place of Remote Config. Leave it empty to use the value of the platform.")]
            [TextArea(2, 12)]
            public string country = "";
        }

        [Serializable]
        internal sealed class CountryCheck
        {
            [Tooltip("Country mode: a device in the target country gets the country configs of this asset instead of the Remote Config values.")]
            public bool enabled;

            [Tooltip("ISO 3166 code of the target country, such as vn.")]
            public string targetCountry = "vn";

            [Tooltip("Application.systemLanguage values that count as the target country.")]
            public string[] systemLanguages = { "Vietnamese" };

            [Tooltip("Language codes of the device locale that count as the target country.")]
            public string[] languageCodes = { "vi" };

            [Tooltip("Counts a device whose locale region is the target country.")]
            public bool matchRegion = true;

            [Tooltip("Parts of the time zone name that count as the target country, such as Ho_Chi_Minh.")]
            public string[] timezoneNames = { "Ho_Chi_Minh", "Saigon" };

            [Tooltip("UTC offsets in hours that count as the target country. Several countries share an offset (UTC+7 is also Thailand and western Indonesia), so it is empty by default.")]
            public float[] utcOffsets = new float[0];

            [Tooltip("Android: counts a device whose SIM card is from the target country.")]
            public bool matchSimCountry = true;

            [Tooltip("Android: counts a device on a mobile network of the target country.")]
            public bool matchNetworkCountry = true;

            [Tooltip("Device IDs that never get country mode, on top of the Remote Config key devices.debugDevices. The Device page of the debug panel shows the ID.")]
            public string[] debugDevices = new string[0];

            [Tooltip("Treats the Editor as a device in the target country, to try the country configs. The Editor never gets country mode otherwise.")]
            public bool simulateInEditor;
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
