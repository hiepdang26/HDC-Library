using System;
using UnityEngine;

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

        internal const string Tag = "[HDCAdjust] ";

#if UNITY_ANDROID
        internal const string PlatformName = "Android";
#elif UNITY_IOS
        internal const string PlatformName = "iOS";
#else
        internal const string PlatformName = "";
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

        public static HDCAdjustAttribution Attribution => HDCAdjustAttributions.Current;

        public static bool IsAttributionReady => Attribution != null;

        public static string Network => HDCAdjustAttributions.Network;

        public static event Action<HDCAdjustAttribution> AttributionChanged
        {
            add => HDCAdjustAttributions.Changed += value;
            remove => HDCAdjustAttributions.Changed -= value;
        }

        internal static bool InScene => instance != null;

        internal static HDCAdjustStart StartedBy { get; private set; }

        internal static bool SendsAdRevenue => instance != null && instance.sendAdRevenue;

        internal static bool HasPurchaseEventToken => instance != null && instance.PurchaseEventToken.Length > 0;

        internal static bool IsPhone =>
            Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer;

        private string AppToken => ForPlatform(androidAppToken, iosAppToken);

        private string PurchaseEventToken => ForPlatform(androidPurchaseEventToken, iosPurchaseEventToken);

        public static bool TrackPurchaseRevenue(double amount, string currency, string transactionId = null) =>
            HDCAdjustPurchases.Track(instance != null, instance != null ? instance.PurchaseEventToken : "", amount, currency, transactionId);

        internal static void ResetState()
        {
            instance = null;
            StartedBy = HDCAdjustStart.NotInScene;
            HDCAdjustAttributions.Reset();
            HDCAdjustSdk.Reset();
            HDCAdjustRevenue.Reset();
        }

#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForPlayMode() => ResetState();
#endif

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                if (!IsConfigured || instance.IsConfigured)
                {
                    Remove(this);
                    return;
                }

                HDCAdjust replaced = instance;
                replaced.Leave();
                Remove(replaced);
            }
            else if (!IsConfigured && ConfiguredOneIsLoaded())
            {
                Remove(this);
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
            Leave();
        }

        private void Leave()
        {
            StopAllCoroutines();
#if HDC_ADS && HDC_ADJUST
            HDCAds.Revenue -= OnAdRevenue;
#endif
#if HDC_ADJUST
            HDCAdjustSdk.StopListeningForDeeplinks();
#endif
        }

        private bool IsConfigured => startManually || AppToken.Length > 0;

        private bool ConfiguredOneIsLoaded()
        {
#if UNITY_2023_1_OR_NEWER
            HDCAdjust[] loaded = FindObjectsByType<HDCAdjust>(FindObjectsSortMode.None);
#else
            HDCAdjust[] loaded = FindObjectsOfType<HDCAdjust>();
#endif
            foreach (HDCAdjust other in loaded)
            {
                if (other != this && other.IsConfigured)
                    return true;
            }

            return false;
        }

        private static void Remove(HDCAdjust adjust)
        {
            if (adjust.GetComponents<Component>().Length > 2)
                Destroy(adjust);
            else
                Destroy(adjust.gameObject);
        }

        private HDCAdjustStart Begin()
        {
#if HDC_ADJUST
            bool sdkPrefabInScene = HDCAdjustSdk.SdkPrefabInScene(out bool sdkPrefabStarts);
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

            if (!sdkPrefabInScene)
                HDCAdjustSdk.ListenForDeeplinks();

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
                HDCAdjustSdk.Start(SdkSettings());
                start = HDCAdjustStart.ByHdc;
            }

            StartCoroutine(HDCAdjustSdk.ReadAttribution(attributionTimeoutSeconds));
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

        private HDCAdjustSdkSettings SdkSettings() =>
            new HDCAdjustSdkSettings
            {
                AppToken = AppToken,
                Environment = environment,
                LogLevel = logLevel,
                CoppaCompliance = coppaCompliance,
                SendInBackground = sendInBackground,
                LaunchDeferredDeeplink = launchDeferredDeeplink,
                CostDataInAttribution = costDataInAttribution,
                LinkMe = linkMe,
                DefaultTracker = defaultTracker,
                PreinstallTracking = preinstallTracking,
                PreinstallFilePath = preinstallFilePath,
                AdServices = adServices,
                IdfaReading = idfaReading,
                SkanAttribution = skanAttribution,
            };

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

#if HDC_ADS && HDC_ADJUST
        private void OnAdRevenue(HDCAdRevenue revenue)
        {
            if (!sendAdRevenue || StartedBy == HDCAdjustStart.NoAppToken)
                return;
            HDCAdjustRevenue.Send(revenue);
        }
#endif
    }
}
