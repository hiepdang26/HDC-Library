using System;
using HDC.Ads;
using UnityEngine;

namespace HDC.Onboarding
{
    [Serializable]
    public sealed class HDCOnboardingConfig
    {
        public HDCSplashConfig splash_config = new HDCSplashConfig();
        public HDCIntroConfig intro_config = new HDCIntroConfig();

        public static HDCOnboardingConfig Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new HDCOnboardingConfig();
            try
            {
                HDCOnboardingConfig config = JsonUtility.FromJson<HDCOnboardingConfig>(HDCIntroConfig.EnumsAsNumbers(json)) ?? new HDCOnboardingConfig();
                config.splash_config = config.splash_config ?? new HDCSplashConfig();
                config.intro_config = (config.intro_config ?? new HDCIntroConfig()).Normalized();
                return config;
            }
            catch (Exception exception)
            {
                HDCOnboardingLog.Error(HDCOnboardingKeys.OnboardingConfig + " is not valid JSON: " + exception.Message);
                return new HDCOnboardingConfig();
            }
        }

        internal static string Raw() => HDCCustomConfig.Get(HDCOnboardingKeys.OnboardingConfig);
    }
}
