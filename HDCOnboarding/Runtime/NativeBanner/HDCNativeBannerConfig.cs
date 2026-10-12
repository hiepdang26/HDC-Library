using System;
using HDC.Ads;
using UnityEngine;

namespace HDC.Onboarding
{
    [Serializable]
    public sealed class HDCNativeBannerConfig
    {
        public string activeSceneName;
        public HDCNativeBannerLoop[] bannerLoops;

        public static HDCNativeBannerConfig Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;
            try
            {
                return JsonUtility.FromJson<HDCNativeBannerConfig>(json);
            }
            catch (Exception exception)
            {
                HDCOnboardingLog.Error(HDCOnboardingKeys.FirstNativeBannerConfig + " is not valid JSON: " + exception.Message);
                return null;
            }
        }

        internal static string Raw() => HDCCustomConfig.Get(HDCOnboardingKeys.FirstNativeBannerConfig);
    }

    [Serializable]
    public sealed class HDCNativeBannerLoop
    {
        public bool isEnabled;
        public string groupName;
        public float timeOut;
        public int adShowCount;
        public float defaultTimeConfig;
        public float metaTimeConfig;

        internal bool HasShowsLeft => isEnabled && (adShowCount > 0 || adShowCount == -1);
    }
}
