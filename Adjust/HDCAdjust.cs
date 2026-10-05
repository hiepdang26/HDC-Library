using System;
using System.Collections;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;
#if HDC_ADJUST
using AdjustSdk;
#endif

namespace HDC.Ads
{
    [DisallowMultipleComponent]
    [AddComponentMenu("HDC/HDC Adjust")]
    public sealed class HDCAdjust : MonoBehaviour
    {
        public const string AdMobRevenueSource = "admob_sdk";
        public const string TimedOutNetwork = "time_out";
        public const string NetworkKey = "user_network";
        public const string CampaignKey = "user_campaign";
        public const string CreativeKey = "user_creative";
        public const string CostKey = "user_cost";

        private const string Tag = "[HDCAdjust] ";
        private const float EnabledPollSeconds = 0.5f;
        private const float SlowEnabledPollSeconds = 5f;
        private const float SlowPollAfterSeconds = 30f;
        private const int NetworkMaxLength = 30;

#if UNITY_ANDROID
        private const string PlatformName = "Android";
#elif UNITY_IOS
        private const string PlatformName = "iOS";
#else
        private const string PlatformName = "";
#endif

#pragma warning disable CS0169, CS0414
        [Header("SDK")]
        [Tooltip("Turn it on when the game starts Adjust itself with Adjust.InitSdk. HDCAdjust then skips the SDK settings below " +
                 "and only reads the attribution, sends the ad revenue and the purchases.")]
        [SerializeField] private bool startManually;

        [Tooltip("App token of the Android app in the Adjust dashboard.")]
        [SerializeField] private string androidAppToken = "";

        [Tooltip("App token of the iOS app in the Adjust dashboard.")]
        [SerializeField] private string iosAppToken = "";

        [Tooltip("Auto: Sandbox in the Editor and in Development builds, Production in every other build.")]
        [SerializeField] private HDCAdjustEnvironment environment = HDCAdjustEnvironment.Auto;

        [Tooltip("Adjust SDK log level. Suppress turns the Adjust logs off.")]
        [SerializeField] private HDCAdjustLogLevel logLevel = HDCAdjustLogLevel.Info;

        [Tooltip("Marks the app as directed at children (COPPA).")]
        [SerializeField] private bool coppaCompliance;

        [Tooltip("Lets Adjust send its requests while the app is in the background.")]
        [SerializeField] private bool sendInBackground;

        [Tooltip("Opens the deferred deep link of the install campaign.")]
        [SerializeField] private bool launchDeferredDeeplink = true;

        [Tooltip("Adds the cost type, amount and currency to the attribution. They stay empty while it is off.")]
        [SerializeField] private bool costDataInAttribution;

        [Tooltip("Turns on LinkMe: iOS deferred deep links read from the pasteboard.")]
        [SerializeField] private bool linkMe;

        [Tooltip("Tracker token for the installs that no campaign claims, such as a preinstalled build. Leave it empty otherwise.")]
        [SerializeField] private string defaultTracker = "";

        [Header("Android")]
        [Tooltip("Reads the preinstall campaign of a device maker build.")]
        [SerializeField] private bool preinstallTracking;

        [Tooltip("Path of the system file that holds the preinstall campaign. Leave it empty for the default.")]
        [SerializeField] private string preinstallFilePath = "";

        [Header("iOS")]
        [Tooltip("Uses Apple Search Ads attribution (AdServices).")]
        [SerializeField] private bool adServices = true;

        [Tooltip("Reads the IDFA once the player allows tracking.")]
        [SerializeField] private bool idfaReading = true;

        [Tooltip("Lets Adjust register the app for SKAdNetwork attribution.")]
        [SerializeField] private bool skanAttribution = true;

        [Header("HDC")]
        [Tooltip("Sends the revenue of every HDC ad to Adjust: source admob_sdk, network = ad source, unit = ad unit id, " +
                 "placement = position.")]
        [SerializeField] private bool sendAdRevenue = true;

        [Tooltip("Seconds to wait for the attribution once Adjust is running. HDCAdjust.Network reads time_out when it does not come in time.")]
        [SerializeField] private float attributionTimeoutSeconds = 7f;

        [Tooltip("Adjust event token that HDCAdjust.TrackPurchaseRevenue sends on Android.")]
        [SerializeField] private string androidPurchaseEventToken = "";

        [Tooltip("Adjust event token that HDCAdjust.TrackPurchaseRevenue sends on iOS.")]
        [SerializeField] private string iosPurchaseEventToken = "";
#pragma warning restore CS0169, CS0414

        private static HDCAdjust instance;

        public static HDCAdjustAttribution Attribution { get; private set; }

        public static bool IsAttributionReady => Attribution != null;

        public static string Network { get; private set; } = "";

        public static event Action<HDCAdjustAttribution> AttributionChanged;

        internal static bool InScene => instance != null;

        internal static HDCAdjustStart StartedBy { get; private set; }

        internal static bool UsedSandbox { get; private set; }

        internal static bool AdjustEnabled { get; private set; }

        internal static bool TimedOut { get; private set; }

        internal static int RevenueCount { get; private set; }

        internal static HDCAdjustAdRevenue? LastRevenue { get; private set; }

        internal static bool SendsAdRevenue => instance != null && instance.sendAdRevenue;

        internal static bool HasPurchaseEventToken => instance != null && instance.PurchaseEventToken.Length > 0;

        private static bool IsPhone =>
            Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer;

        private string AppToken => ForPlatform(androidAppToken, iosAppToken);

        private string PurchaseEventToken => ForPlatform(androidPurchaseEventToken, iosPurchaseEventToken);

        public static bool TrackPurchaseRevenue(double amount, string currency, string transactionId = null)
        {
#if HDC_ADJUST
            string token = instance != null ? instance.PurchaseEventToken : "";
            if (token.Length == 0)
            {
                Debug.LogWarning(Tag + (instance == null ? "HDCAdjust is not in the scene" : "HDCAdjust has no " + PlatformName + " purchase event token")
                    + ", so the purchase is not sent to Adjust.");
                return false;
            }

            if (!IsPhone)
            {
                Debug.Log(Tag + "Purchase not sent outside Android and iOS: " + amount.ToString(CultureInfo.InvariantCulture) + " " + currency);
                return false;
            }

            var purchase = new AdjustEvent(token);
            purchase.SetRevenue(amount, currency);
            if (!string.IsNullOrEmpty(transactionId))
                purchase.TransactionId = transactionId;
            Adjust.TrackEvent(purchase);
            return true;
#else
            Debug.LogWarning(Tag + "The Adjust SDK is not in the project, so the purchase is not sent.");
            return false;
#endif
        }

        internal static bool UsesSandbox(HDCAdjustEnvironment environment, bool developmentBuild) =>
            environment == HDCAdjustEnvironment.Sandbox || (environment == HDCAdjustEnvironment.Auto && developmentBuild);

        internal static string NormalizeNetwork(string network)
        {
            if (string.IsNullOrEmpty(network))
                return "";
            if (char.IsDigit(network[0]))
                network = network.Substring(1);
            string normalized = Regex.Replace(network.Replace(" ", "_"), "[^a-zA-Z0-9_]", "").ToLowerInvariant();
            return normalized.Length > NetworkMaxLength ? normalized.Substring(0, NetworkMaxLength) : normalized;
        }

        internal static void Receive(HDCAdjustAttribution attribution)
        {
            if (attribution == null || attribution.IsEmpty || attribution.SameAs(Attribution))
                return;

            Attribution = attribution;
            Network = NormalizeNetwork(attribution.Network);
            Store(attribution);
            Action<HDCAdjustAttribution> handlers = AttributionChanged;
            if (handlers == null)
                return;
            foreach (Action<HDCAdjustAttribution> handler in handlers.GetInvocationList())
            {
                try
                {
                    handler(attribution);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }
        }

        internal static void MarkTimedOut()
        {
            if (Attribution != null)
                return;
            TimedOut = true;
            Network = TimedOutNetwork;
        }

        internal static void ResetState()
        {
            instance = null;
            Attribution = null;
            Network = "";
            AttributionChanged = null;
            StartedBy = HDCAdjustStart.NotInScene;
            UsedSandbox = false;
            AdjustEnabled = false;
            TimedOut = false;
            RevenueCount = 0;
            LastRevenue = null;
        }

#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForPlayMode() => ResetState();
#endif

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                if (GetComponents<Component>().Length > 2)
                    Destroy(this);
                else
                    Destroy(gameObject);
                return;
            }

            instance = this;
            if (transform.parent != null)
                transform.SetParent(null, false);
            DontDestroyOnLoad(gameObject);
            StartedBy = Begin();
#if HDC_ADS && HDC_ADJUST
            HDCAds.Revenue += OnAdRevenue;
#endif
        }

        private void OnDestroy()
        {
            if (instance != this)
                return;
            instance = null;
#if HDC_ADS && HDC_ADJUST
            HDCAds.Revenue -= OnAdRevenue;
#endif
#if HDC_ADJUST && UNITY_ANDROID
            Application.deepLinkActivated -= OnDeeplink;
#endif
        }

        private HDCAdjustStart Begin()
        {
#if HDC_ADJUST
            Adjust sdkPrefab = FindSdkPrefab();
            bool sdkPrefabStarts = sdkPrefab != null && !sdkPrefab.startManually;
            if (sdkPrefabStarts)
                Debug.LogWarning(Tag + "The Adjust SDK prefab in the scene starts Adjust with its own settings, so HDCAdjust does not. " +
                                 "Remove that prefab to start Adjust with the HDCAdjust settings.");

            if (Application.isEditor)
            {
                if (!sdkPrefabStarts)
                    WarnIfNoAppToken();
                return HDCAdjustStart.Editor;
            }

            if (!IsPhone)
                return HDCAdjustStart.Unsupported;

            if (sdkPrefab == null)
                ListenForDeeplinks();

            HDCAdjustStart start;
            if (sdkPrefabStarts)
            {
                start = HDCAdjustStart.BySdkPrefab;
            }
            else if (startManually)
            {
                start = HDCAdjustStart.ByGame;
            }
            else if (WarnIfNoAppToken())
            {
                return HDCAdjustStart.NoAppToken;
            }
            else
            {
                StartAdjust();
                start = HDCAdjustStart.ByHdc;
            }

            StartCoroutine(ReadAttribution());
            return start;
#else
            Debug.LogWarning(Tag + "The Adjust SDK is not in the project, so HDCAdjust does nothing. Import the Adjust Unity SDK 5.");
            return HDCAdjustStart.NoSdk;
#endif
        }

        private bool WarnIfNoAppToken()
        {
            if (startManually || PlatformName.Length == 0 || AppToken.Length > 0)
                return false;
            string message = Tag + "The " + PlatformName + " app token is empty, so Adjust does not start on " + PlatformName + ". Set it on HDCAdjust.";
            if (Application.isEditor)
                Debug.LogWarning(message);
            else
                Debug.LogError(message);
            return true;
        }

        private static string ForPlatform(string android, string ios)
        {
#if UNITY_ANDROID
            return (android ?? "").Trim();
#elif UNITY_IOS
            return (ios ?? "").Trim();
#else
            return "";
#endif
        }

        private static void Store(HDCAdjustAttribution attribution)
        {
            PlayerPrefs.SetString(NetworkKey, attribution.Network);
            PlayerPrefs.SetString(CampaignKey, attribution.Campaign);
            PlayerPrefs.SetString(CreativeKey, attribution.Creative);
            if (attribution.CostAmount.HasValue)
                PlayerPrefs.SetString(CostKey, attribution.CostAmount.Value.ToString(CultureInfo.InvariantCulture));
            PlayerPrefs.Save();
        }

#if HDC_ADJUST
        private void StartAdjust()
        {
            bool sandbox = UsesSandbox(environment, Debug.isDebugBuild);
            if (sandbox && !Debug.isDebugBuild)
                Debug.LogWarning(Tag + "Adjust runs in Sandbox in a release build, so its installs and revenue stay out of the live data. " +
                                 "Set Environment to Auto or Production for release.");

            var config = new AdjustConfig(AppToken, sandbox ? AdjustEnvironment.Sandbox : AdjustEnvironment.Production,
                logLevel == HDCAdjustLogLevel.Suppress)
            {
                LogLevel = (AdjustLogLevel)logLevel,
                IsSendingInBackgroundEnabled = sendInBackground,
                IsDeferredDeeplinkOpeningEnabled = launchDeferredDeeplink,
                DefaultTracker = NullIfEmpty(defaultTracker),
                IsCoppaComplianceEnabled = coppaCompliance,
                IsCostDataInAttributionEnabled = costDataInAttribution,
                IsPreinstallTrackingEnabled = preinstallTracking,
                PreinstallFilePath = NullIfEmpty(preinstallFilePath),
                IsAdServicesEnabled = adServices,
                IsIdfaReadingEnabled = idfaReading,
                IsLinkMeEnabled = linkMe,
                IsSkanAttributionEnabled = skanAttribution,
                AttributionChangedDelegate = OnAdjustAttribution,
            };
            Adjust.InitSdk(config);
            UsedSandbox = sandbox;
        }

        private IEnumerator ReadAttribution()
        {
            float startedAt = Time.realtimeSinceStartup;
            while (!AdjustEnabled)
            {
                Adjust.IsEnabled(on => AdjustEnabled |= on);
                bool slow = Time.realtimeSinceStartup - startedAt > SlowPollAfterSeconds;
                yield return new WaitForSecondsRealtime(slow ? SlowEnabledPollSeconds : EnabledPollSeconds);
            }

            Adjust.GetAttribution(OnAdjustAttribution);
            float deadline = Time.realtimeSinceStartup + Mathf.Max(0f, attributionTimeoutSeconds);
            while (Attribution == null && Time.realtimeSinceStartup < deadline)
                yield return null;
            MarkTimedOut();
        }

        private static void OnAdjustAttribution(AdjustAttribution attribution)
        {
            if (attribution == null)
                return;
            Receive(new HDCAdjustAttribution(attribution.TrackerToken, attribution.TrackerName, attribution.Network, attribution.Campaign,
                attribution.Adgroup, attribution.Creative, attribution.ClickLabel, attribution.CostType, attribution.CostAmount,
                attribution.CostCurrency));
        }

        private static Adjust FindSdkPrefab()
        {
#if UNITY_2023_1_OR_NEWER
            return FindAnyObjectByType<Adjust>();
#else
            return FindObjectOfType<Adjust>();
#endif
        }

        private static string NullIfEmpty(string text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();

        private void ListenForDeeplinks()
        {
#if UNITY_ANDROID
            Application.deepLinkActivated += OnDeeplink;
            if (!string.IsNullOrEmpty(Application.absoluteURL))
                OnDeeplink(Application.absoluteURL);
#endif
        }

#if UNITY_ANDROID
        private static void OnDeeplink(string url) => Adjust.ProcessDeeplink(new AdjustDeeplink(url));
#endif
#endif

#if HDC_ADS && HDC_ADJUST
        private void OnAdRevenue(HDCAdRevenue revenue)
        {
            if (!sendAdRevenue || StartedBy == HDCAdjustStart.NoAppToken)
                return;

            HDCAdjustAdRevenue adRevenue = HDCAdjustAdRevenue.From(revenue);
            RevenueCount++;
            LastRevenue = adRevenue;
            if (!IsPhone)
            {
                if (HDCAds.Testing.DebugLog)
                    Debug.Log(Tag + "Ad revenue not sent outside Android and iOS: " + adRevenue);
                return;
            }

            var tracked = new AdjustAdRevenue(adRevenue.Source);
            tracked.SetRevenue(adRevenue.Revenue, adRevenue.Currency);
            tracked.AdRevenueNetwork = adRevenue.Network;
            tracked.AdRevenueUnit = adRevenue.Unit;
            tracked.AdRevenuePlacement = adRevenue.Placement;
            Adjust.TrackAdRevenue(tracked);
            if (HDCAds.Testing.DebugLog)
                Debug.Log(Tag + "Ad revenue sent: " + adRevenue);
        }
#endif
    }
}
