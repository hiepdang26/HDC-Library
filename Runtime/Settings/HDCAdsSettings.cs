using System;
using System.Collections.Generic;
using UnityEngine;

namespace HDC.Ads
{
    /// <summary>
    /// The project's default ads configs: the JSON values used until Firebase Remote Config has its own.
    /// One asset in a Resources folder, edited from HDC > Edit configs. iOS uses the Android values while its
    /// own are empty. This assembly always compiles, so the configs stay editable while HDC ads are off.
    /// </summary>
    public sealed class HDCAdsSettings : ScriptableObject
    {
        public const string ResourceName = "HDCAdsSettings";

        /// <summary>Remote Config keys of the ad core config; ads_config picks one through selectedAdCoreName.</summary>
        public static readonly string[] CoreConfigKeys = { "adcore_main_android", "adcore_main_ios" };

        [SerializeField, TextArea(5, 30)] private string adsConfigAndroid = "{}";
        [SerializeField, TextArea(5, 30)] private string adsConfigIos = "";
        [SerializeField, TextArea(5, 30)] private string coreConfigAndroid = "{}";
        [SerializeField, TextArea(5, 30)] private string coreConfigIos = "";

        /// <summary>This platform's default ads_config.</summary>
        public string AdsConfig => ForPlatform(adsConfigAndroid, adsConfigIos);

        /// <summary>This platform's default ad core config.</summary>
        public string CoreConfig => ForPlatform(coreConfigAndroid, coreConfigIos);

        /// <summary>The project's settings, or empty ones, with a warning, when the project has none.</summary>
        public static HDCAdsSettings Load()
        {
            var settings = Resources.Load<HDCAdsSettings>(ResourceName);
            if (settings != null)
                return settings;

            Debug.LogWarning("[HDCAds] No HDCAdsSettings in a Resources folder: open HDC > Edit configs to create it.");
            return CreateInstance<HDCAdsSettings>();
        }

        /// <summary>
        /// The default ad core config under every key the ads config may pick: the standard keys, and the one the
        /// default ads config names.
        /// </summary>
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

#pragma warning disable 0649 // Assigned by JsonUtility.
        [Serializable]
        private sealed class AdsConfigHeader
        {
            public string selectedAdCoreName;
        }
#pragma warning restore 0649
    }
}
