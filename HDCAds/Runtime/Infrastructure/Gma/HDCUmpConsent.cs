using System;
using System.Collections.Generic;
using GoogleMobileAds.Ump.Api;
using HDC.Ads.Ports;
using UnityEngine;

namespace HDC.Ads.Infrastructure
{
    internal sealed class HDCUmpConsent : IConsentProvider
    {
        private const string PurposeConsentsKey = "IABTCF_PurposeConsents";

        public bool CanRequestAds => ConsentInformation.CanRequestAds();

        public HDCConsentStatus Status
        {
            get
            {
                switch (ConsentInformation.ConsentStatus)
                {
                    case ConsentStatus.NotRequired:
                        return HDCConsentStatus.NotRequired;
                    case ConsentStatus.Required:
                        return HDCConsentStatus.Required;
                    case ConsentStatus.Obtained:
                        return HDCConsentStatus.Obtained;
                    default:
                        return HDCConsentStatus.Unknown;
                }
            }
        }

        public bool PrivacyOptionsRequired =>
            ConsentInformation.PrivacyOptionsRequirementStatus == PrivacyOptionsRequirementStatus.Required;

        public string PurposeConsents
        {
            get
            {
                try
                {
#if UNITY_ANDROID && !UNITY_EDITOR
                    using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                    using (AndroidJavaObject activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                    using (var manager = new AndroidJavaClass("android.preference.PreferenceManager"))
                    using (AndroidJavaObject preferences = manager.CallStatic<AndroidJavaObject>("getDefaultSharedPreferences", activity))
                        return preferences.Call<string>("getString", PurposeConsentsKey, string.Empty) ?? string.Empty;
#else
                    return PlayerPrefs.GetString(PurposeConsentsKey, string.Empty);
#endif
                }
                catch (Exception)
                {
                    return string.Empty;
                }
            }
        }

        public void Update(bool debugInEea, Action<string> onDone)
        {
            HDCMainThread.EnsureCreated();
            var parameters = new ConsentRequestParameters
            {
                TagForUnderAgeOfConsent = false,
                ConsentDebugSettings = new ConsentDebugSettings
                {
                    DebugGeography = debugInEea ? DebugGeography.EEA : DebugGeography.Disabled,
                    TestDeviceHashedIds = new List<string>(HDCGma.TestDeviceIds()),
                },
            };
            ConsentInformation.Update(parameters, error => Answer(onDone, error));
        }

        public void ShowFormIfRequired(Action<string> onDone) =>
            ConsentForm.LoadAndShowConsentFormIfRequired(error => Answer(onDone, error));

        public void ShowPrivacyOptions(Action<string> onDone)
        {
            HDCMainThread.EnsureCreated();
            ConsentForm.ShowPrivacyOptionsForm(error => Answer(onDone, error));
        }

        public void Reset() => ConsentInformation.Reset();

        private static void Answer(Action<string> onDone, FormError error)
        {
            string message = error == null ? null : $"{error.ErrorCode} · {error.Message}";
            HDCMainThread.Post(() => onDone?.Invoke(message));
        }
    }
}
