using System;
using System.Collections.Generic;
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

#pragma warning disable 0649
        [Serializable]
        private sealed class AdsConfigHeader
        {
            public string selectedAdCoreName;
        }
#pragma warning restore 0649
    }
}
