using System;
using UnityEngine;

namespace HDC.Ads.Domain
{
    [Serializable]
    internal sealed class HDCCountryRules
    {
        public bool enabled;
        public string targetCountry = "vn";
        public string[] systemLanguages = new string[0];
        public string[] languageCodes = new string[0];
        public bool matchRegion = true;
        public string[] timezoneNames = new string[0];
        public float[] utcOffsets = new float[0];
        public bool matchSimCountry = true;
        public bool matchNetworkCountry = true;
        public string[] debugDevices = new string[0];
        public bool simulateInEditor;

        internal static HDCCountryRules Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new HDCCountryRules();
            try
            {
                return JsonUtility.FromJson<HDCCountryRules>(json) ?? new HDCCountryRules();
            }
            catch (ArgumentException)
            {
                return new HDCCountryRules();
            }
        }
    }
}
