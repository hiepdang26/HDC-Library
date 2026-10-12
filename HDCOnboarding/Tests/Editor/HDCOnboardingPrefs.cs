using System;
using System.Collections.Generic;
using UnityEngine;

namespace HDC.Onboarding.Tests
{
    internal sealed class HDCOnboardingPrefs : IDisposable
    {
        private static readonly string[] IntKeys =
        {
            HDCOnboardingKeys.FirstOpen, "REMOVEADS", "intro_0", "intro_1", "intro_2", "intro_3",
            HDCAdTrackingCounts.SessionKey, HDCAdTrackingCounts.TotalKey, HDCAdTrackingCounts.LegacySessionKey, HDCAdTrackingCounts.LegacyTotalKey,
            HDCAdTrackingCounts.CountPrefix + "native_splash", HDCAdTrackingCounts.CountPrefix + "splash", HDCAdTrackingCounts.CountPrefix + "grp",
            "native_splash", "splash", "grp",
        };

        private static readonly string[] StringKeys =
        {
            "HDCAds.Custom." + HDCOnboardingKeys.OnboardingConfig, "HDCAds.Custom." + HDCOnboardingKeys.FirstNativeBannerConfig, HDCAdTrackingCounts.ValuePrefix + "USD", HDCAdTrackingCounts.ValuePrefix + "EUR",
        };

        private readonly List<(string Key, bool Had, int Value)> ints = new List<(string, bool, int)>();
        private readonly List<(string Key, bool Had, string Value)> strings = new List<(string, bool, string)>();

        internal HDCOnboardingPrefs()
        {
            foreach (string key in IntKeys)
            {
                ints.Add((key, PlayerPrefs.HasKey(key), PlayerPrefs.GetInt(key, 0)));
                PlayerPrefs.DeleteKey(key);
            }

            foreach (string key in StringKeys)
            {
                strings.Add((key, PlayerPrefs.HasKey(key), PlayerPrefs.GetString(key, "")));
                PlayerPrefs.DeleteKey(key);
            }
        }

        public void Dispose()
        {
            foreach ((string key, bool had, int value) in ints)
            {
                if (had)
                    PlayerPrefs.SetInt(key, value);
                else
                    PlayerPrefs.DeleteKey(key);
            }

            foreach ((string key, bool had, string value) in strings)
            {
                if (had)
                    PlayerPrefs.SetString(key, value);
                else
                    PlayerPrefs.DeleteKey(key);
            }

            PlayerPrefs.Save();
        }
    }
}
