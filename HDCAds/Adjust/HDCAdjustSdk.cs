using System.Collections;
using UnityEngine;
#if HDC_ADJUST
using AdjustSdk;
#endif

namespace HDC.Ads
{
    internal static class HDCAdjustSdk
    {
        private const float EnabledPollSeconds = 0.5f;
        private const float SlowEnabledPollSeconds = 5f;
        private const float SlowPollAfterSeconds = 30f;

        internal static bool UsedSandbox { get; private set; }

        internal static bool AdjustEnabled { get; private set; }

        internal static bool UsesSandbox(HDCAdjustEnvironment environment, bool developmentBuild) =>
            environment == HDCAdjustEnvironment.Sandbox || (environment == HDCAdjustEnvironment.Auto && developmentBuild);

        internal static void Reset()
        {
            UsedSandbox = false;
            AdjustEnabled = false;
        }

#if HDC_ADJUST
        internal static void Start(HDCAdjustSdkSettings settings)
        {
            bool sandbox = UsesSandbox(settings.Environment, Debug.isDebugBuild);
            if (sandbox && !Debug.isDebugBuild)
                Debug.LogWarning(HDCAdjust.Tag + "Adjust runs in Sandbox in a release build, so its installs and revenue stay out of the live data. " +
                                 "Set Environment to Auto or Production for release.");

            var config = new AdjustConfig(settings.AppToken, sandbox ? AdjustEnvironment.Sandbox : AdjustEnvironment.Production,
                settings.LogLevel == HDCAdjustLogLevel.Suppress)
            {
                LogLevel = (AdjustLogLevel)settings.LogLevel,
                IsSendingInBackgroundEnabled = settings.SendInBackground,
                IsDeferredDeeplinkOpeningEnabled = settings.LaunchDeferredDeeplink,
                DefaultTracker = NullIfEmpty(settings.DefaultTracker),
                IsCoppaComplianceEnabled = settings.CoppaCompliance,
                IsCostDataInAttributionEnabled = settings.CostDataInAttribution,
                IsPreinstallTrackingEnabled = settings.PreinstallTracking,
                PreinstallFilePath = NullIfEmpty(settings.PreinstallFilePath),
                IsAdServicesEnabled = settings.AdServices,
                IsIdfaReadingEnabled = settings.IdfaReading,
                IsLinkMeEnabled = settings.LinkMe,
                IsSkanAttributionEnabled = settings.SkanAttribution,
                AttributionChangedDelegate = OnAttribution,
            };
            Adjust.InitSdk(config);
            UsedSandbox = sandbox;
        }

        internal static IEnumerator ReadAttribution(float timeoutSeconds)
        {
            float startedAt = Time.realtimeSinceStartup;
            while (!AdjustEnabled)
            {
                Adjust.IsEnabled(on => AdjustEnabled |= on);
                bool slow = Time.realtimeSinceStartup - startedAt > SlowPollAfterSeconds;
                yield return new WaitForSecondsRealtime(slow ? SlowEnabledPollSeconds : EnabledPollSeconds);
            }

            Adjust.GetAttribution(OnAttribution);
            float deadline = Time.realtimeSinceStartup + Mathf.Max(0f, timeoutSeconds);
            while (HDCAdjustAttributions.Current == null && Time.realtimeSinceStartup < deadline)
                yield return null;
            HDCAdjustAttributions.MarkTimedOut();
        }

        internal static bool SdkPrefabInScene(out bool sdkPrefabStarts)
        {
#if UNITY_2023_1_OR_NEWER
            Adjust sdkPrefab = UnityEngine.Object.FindAnyObjectByType<Adjust>();
#else
            Adjust sdkPrefab = UnityEngine.Object.FindObjectOfType<Adjust>();
#endif
            sdkPrefabStarts = sdkPrefab != null && !sdkPrefab.startManually;
            return sdkPrefab != null;
        }

        internal static void ListenForDeeplinks()
        {
#if UNITY_ANDROID
            Application.deepLinkActivated += OnDeeplink;
            if (!string.IsNullOrEmpty(Application.absoluteURL))
                OnDeeplink(Application.absoluteURL);
#endif
        }

        internal static void StopListeningForDeeplinks()
        {
#if UNITY_ANDROID
            Application.deepLinkActivated -= OnDeeplink;
#endif
        }

        private static void OnAttribution(AdjustAttribution attribution)
        {
            if (attribution == null)
                return;
            HDCAdjustAttributions.Receive(new HDCAdjustAttribution(attribution.TrackerToken, attribution.TrackerName, attribution.Network,
                attribution.Campaign, attribution.Adgroup, attribution.Creative, attribution.ClickLabel, attribution.CostType,
                attribution.CostAmount, attribution.CostCurrency));
        }

        private static string NullIfEmpty(string text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();

#if UNITY_ANDROID
        private static void OnDeeplink(string url) => Adjust.ProcessDeeplink(new AdjustDeeplink(url));
#endif
#endif
    }
}
