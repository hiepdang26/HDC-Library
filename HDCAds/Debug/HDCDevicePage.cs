using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HDC.Ads.Composition;
using HDC.Ads.Diagnostics;
using HDC.Ads.Infrastructure;
using HDC.Ads.Logic;
using HDC.Ads.Ports;
using UnityEngine;
#if HDC_WEB_REQUEST
using UnityEngine.Networking;
#endif
using UnityEngine.UI;

namespace HDC.Ads.DebugUI
{
    public sealed class HDCDevicePage : HDCDebugPage
    {
        private const string IpLookupUrl = "https://ipwho.is/";
        private const float ProbeCooldownSeconds = 60f;
        internal const float ClearDataConfirmSeconds = 4f;

        [SerializeField] private HDCKeyValueList buildList;
        [SerializeField] private HDCKeyValueList libraryList;
        [SerializeField] private Button debugLogButton;
        [SerializeField] private Button testDeviceButton;
        [SerializeField] private Button testAdUnitsButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button clearDataButton;
        [SerializeField] private HDCKeyValueList consentList;
        [SerializeField] private Button privacyOptionsButton;
        [SerializeField] private Button resetConsentButton;
        [SerializeField] private HDCKeyValueList mediationList;
        [SerializeField] private Button metaOnButton;
        [SerializeField] private Button metaOffButton;
        [SerializeField] private Button adInspectorButton;
        [SerializeField] private HDCKeyValueList deviceList;
        [SerializeField] private HDCKeyValueList networkList;
        [SerializeField] private Button probeButton;
        [SerializeField] private HDCKeyValueList adjustList;
        [SerializeField] private Button adjustButton;

        private readonly Dictionary<IMediationPartner, (bool On, string DeviceId)> partnerTestModes =
            new Dictionary<IMediationPartner, (bool, string)>();
        private bool probing;
        private bool probedOnce;
        private float lastProbe = float.MinValue;
        private DateTime? lastProbeClock;
        private string probeError;
        private PublicInfo publicInfo;
        private float clearDataArmedUntil = -1f;
        private Coroutine disarmClearData;
        private string adInspectorNote;
        private string consentNote;

        [Serializable]
        private sealed class PublicInfo
        {
            public bool success;
            public string message = string.Empty;
            public string ip = string.Empty;
            public string type = string.Empty;
            public string city = string.Empty;
            public string region = string.Empty;
            public string country = string.Empty;
            public string country_code = string.Empty;
            public float latitude;
            public float longitude;
            public Connection connection = new Connection();
            public TimeZoneInfo timezone = new TimeZoneInfo();
        }

        [Serializable]
        private sealed class Connection
        {
            public string isp = string.Empty;
            public string org = string.Empty;
        }

        [Serializable]
        private sealed class TimeZoneInfo
        {
            public string id = string.Empty;
            public string utc = string.Empty;
        }

        private void Awake()
        {
            debugLogButton.onClick.AddListener(() =>
            {
                HDCAdsSdk.DebugLog = !HDCAdsSdk.DebugLog;
                Refresh();
            });
            testDeviceButton.onClick.AddListener(() =>
            {
                HDCAdsSdk.EnableTestDevice();
                Refresh();
            });
            testAdUnitsButton.onClick.AddListener(() =>
            {
                HDCTestAdUnitsSwitch.Toggle();
                Refresh();
            });
            restartButton.onClick.AddListener(HDCAppRestart.Restart);
            clearDataButton.onClick.AddListener(() =>
            {
                if (ClearDataArmed)
                {
                    clearDataArmedUntil = -1f;
                    HDCAppRestart.ClearDataAndRestart();
                    return;
                }

                clearDataArmedUntil = Time.realtimeSinceStartup + ClearDataConfirmSeconds;
                if (disarmClearData != null)
                    StopCoroutine(disarmClearData);
                disarmClearData = StartCoroutine(DisarmClearDataLater());
                Refresh();
            });
            metaOnButton.onClick.AddListener(() =>
            {
                HDCAdsSdk.EnableTestDevice();
                HDCAds.Testing.EnableMetaTestMode();
                ReadPartners();
                Refresh();
            });
            metaOffButton.onClick.AddListener(() =>
            {
                HDCAds.Testing.DisableMetaTestMode();
                ReadPartners();
                Refresh();
            });
            adInspectorButton.onClick.AddListener(OpenAdInspector);
            privacyOptionsButton.onClick.AddListener(() => HDCAds.Consent.ShowPrivacyOptions(error =>
            {
                consentNote = error == null
                    ? HDCDebugStyle.Colored(HDCDebugStyle.MutedHex, "Privacy options đã đóng.")
                    : HDCDebugStyle.Colored(HDCDebugStyle.BadHex, "Không mở được privacy options: " + error);
                MarkDirty();
            }));
            resetConsentButton.onClick.AddListener(() =>
            {
                HDCAdsRuntime.Current.Context.Consent.Reset();
                consentNote = HDCDebugStyle.Colored(HDCDebugStyle.WarnHex, HDCAppRestart.Relaunches
                    ? "Đã xoá consent đã lưu. Bấm Restart App ở thẻ HDC Ads để app hỏi lại từ đầu."
                    : "Đã xoá consent đã lưu. iOS không cho app tự mở lại: thoát app rồi mở lại để app hỏi lại từ đầu.");
                Refresh();
            });
            probeButton.onClick.AddListener(() =>
            {
                OpenCard(probeButton);
                Probe(true);
                Refresh();
            });
            adjustButton.onClick.AddListener(() =>
            {
                OpenCard(adjustButton);
                HDCAdjustProbe.Ask();
                Refresh();
            });
        }

        private static void OpenCard(Component inCard)
        {
            HDCDebugFoldout foldout = inCard.GetComponentInParent<HDCDebugFoldout>();
            if (foldout != null)
                foldout.Apply(true);
        }

        private void OpenAdInspector()
        {
#if UNITY_EDITOR
            adInspectorNote = HDCDebugStyle.Colored(HDCDebugStyle.WarnHex, "Ad Inspector chỉ mở được trên máy thật (Android, iOS).");
#else
            bool registered = !HDCAdsSdk.IsTestDevice;
            if (registered)
                HDCAdsSdk.EnableTestDevice();
            adInspectorNote = HDCDebugStyle.Colored(HDCDebugStyle.MutedHex,
                registered ? "Đã đăng ký máy làm thiết bị test của Google, đang mở Ad Inspector..." : "Đang mở Ad Inspector...");
            HDCAdsSdk.OpenAdInspector(error =>
            {
                adInspectorNote = error == null
                    ? HDCDebugStyle.Colored(HDCDebugStyle.MutedHex, "Ad Inspector đã đóng.")
                    : HDCDebugStyle.Colored(HDCDebugStyle.BadHex, "Không mở được Ad Inspector: " + error);
                MarkDirty();
            });
#endif
            Refresh();
        }

        internal override void Shown()
        {
            ReadPartners();
            if (!probedOnce)
            {
                probedOnce = true;
                Probe(false);
                HDCAdjustProbe.Ask();
            }
        }

        protected override void Redraw()
        {
            if (Shown(buildList))
                RedrawBuild();
            if (Shown(libraryList))
                RedrawLibrary();
            if (Shown(consentList))
                RedrawConsent();
            if (Shown(mediationList))
                RedrawMediation();
            if (Shown(deviceList))
                RedrawDevice();
            if (Shown(networkList))
                RedrawNetwork();
            if (Shown(adjustList))
                RedrawAdjust();
            probeButton.interactable = !probing;
            adjustButton.interactable = HDCAdjustProbe.Found || HDCAdjustProbe.State == "Not checked";
        }

        private static bool Shown(Component part) => part.gameObject.activeInHierarchy;

        private void RedrawBuild()
        {
            buildList.Begin();
            buildList.Row("App Version", Application.version);
            buildList.Row("Bundle ID", Application.identifier);
            buildList.Row("Unity Version", Application.unityVersion);
            buildList.Row("Platform", Application.platform.ToString());
            buildList.Row("Build", Debug.isDebugBuild ? "Development" : "Release", Debug.isDebugBuild ? HDCDebugStyle.WarnColor : HDCDebugStyle.TextColor);
            buildList.Row("Scripting Backend", ScriptingBackend);
            buildList.Row("Install Mode", Application.installMode.ToString());
            buildList.Row("Language", Application.systemLanguage.ToString());
            buildList.End();
        }

        private void RedrawLibrary()
        {
            libraryList.Begin();
            libraryList.Row("Native Library", NativeLibrary);
            libraryList.Row("Remote Config (HDC_FIREBASE)", FirebaseBuilt ? "On" : "Off: configs from the defaults", FirebaseBuilt ? HDCDebugStyle.GoodColor : HDCDebugStyle.WarnColor);
            libraryList.Row("SDK Initialized", HDCAdsSdk.IsInitialized ? "Yes" : "No", HDCAdsSdk.IsInitialized ? HDCDebugStyle.GoodColor : HDCDebugStyle.WarnColor);
            libraryList.Row("HDCAds Initialized", HDCAds.IsInitialized ? "Yes" : "No", HDCAds.IsInitialized ? HDCDebugStyle.GoodColor : HDCDebugStyle.WarnColor);
            libraryList.Row("Ads Removed", HDCAds.IsAdsRemoved ? "Yes: only rewarded ads show" : "No", HDCAds.IsAdsRemoved ? HDCDebugStyle.WarnColor : HDCDebugStyle.TextColor);
            libraryList.Row("Debug Log", HDCAdsSdk.DebugLog ? "On" : "Off", HDCAdsSdk.DebugLog ? HDCDebugStyle.GoodColor : HDCDebugStyle.MutedColor);
            libraryList.Row("Google Test Device", HDCAdsSdk.IsTestDevice
                    ? "On: requests keep the real ad units, and Google answers with test ads"
                    : "Off", HDCAdsSdk.IsTestDevice ? HDCDebugStyle.GoodColor : HDCDebugStyle.MutedColor);
            libraryList.Row("Google Test Ad Units", TestAdUnitsState, HDCAdsSdk.UseTestAdUnits ? HDCDebugStyle.GoodColor : HDCDebugStyle.MutedColor);
            if (HDCTestAdUnitsSwitch.ChangedThisSession)
                libraryList.Note(HDCDebugStyle.Colored(HDCDebugStyle.WarnHex, HDCAppRestart.Relaunches
                    ? "Quảng cáo đã load vẫn giữ ad unit cũ. Bấm Restart App để mở lại app với lựa chọn mới ngay từ đầu."
                    : "Quảng cáo đã load vẫn giữ ad unit cũ. iOS không cho app tự mở lại: bấm Quit App rồi mở lại app."));
            if (ClearDataArmed)
                libraryList.Note(HDCDebugStyle.Colored(HDCDebugStyle.WarnHex,
                    $"Bấm lần nữa trong {ClearDataConfirmSeconds:0} giây để xoá toàn bộ dữ liệu của app: PlayerPrefs, file, cache và Remote Config đã lưu. Công tắc Test Ad Units được giữ lại. " +
                    (HDCAppRestart.Relaunches ? "App sẽ tự mở lại." : "iOS không cho app tự mở lại: app sẽ thoát, mở lại app bằng tay. Lựa chọn ATT và quyền thông báo do iOS giữ, app không xoá được: muốn được hỏi lại thì xoá app rồi cài lại.")));
            libraryList.End();

            HDCDebugStyle.SetLabel(debugLogButton, HDCAdsSdk.DebugLog ? "Debug Log: On" : "Debug Log: Off");
            HDCDebugStyle.Highlight(debugLogButton, HDCAdsSdk.DebugLog);
            HDCDebugStyle.Highlight(testDeviceButton, HDCAdsSdk.IsTestDevice);
            HDCDebugStyle.SetLabel(testAdUnitsButton, HDCAdsSdk.UseTestAdUnits ? "Test Ad Units: On" : "Test Ad Units: Off");
            HDCDebugStyle.Highlight(testAdUnitsButton, HDCAdsSdk.UseTestAdUnits);
            HDCDebugStyle.SetVisible(restartButton, HDCTestAdUnitsSwitch.ChangedThisSession);
            HDCDebugStyle.SetLabel(restartButton, HDCAppRestart.Relaunches ? "Restart App" : "Quit App");
            HDCDebugStyle.SetLabel(clearDataButton, ClearDataArmed
                ? "Tap Again To Clear"
                : HDCAppRestart.Relaunches ? "Clear Data & Restart" : "Clear Data & Quit");
        }

        private bool ClearDataArmed => Time.realtimeSinceStartup < clearDataArmedUntil;

        private IEnumerator DisarmClearDataLater()
        {
            yield return new WaitForSecondsRealtime(ClearDataConfirmSeconds);
            disarmClearData = null;
            clearDataArmedUntil = -1f;
            Refresh();
        }

        private static string TestAdUnitsState
        {
            get
            {
                if (!HDCAdsSdk.UseTestAdUnits)
                    return "Off: turn on with the Test Ad Units button, or HDCAdsSetup > Google Test Ad Units";
                return HDCTestAdUnitsSwitch.IsSaved
                    ? "On: every position loads Google's sample ad unit, on every launch until turned off"
                    : "On: every position loads Google's sample ad unit, turned on by HDCAdsSetup or code";
            }
        }

        private void RedrawMediation()
        {
            IReadOnlyList<IMediationPartner> partners = HDCAdsRuntime.Current.Partners;
            IReadOnlyList<HDCAdapterStatus> adapters = HDCMediationReport.Adapters;
            mediationList.Begin();
            if (HDCMediationReport.Received)
            {
                int ready = adapters.Count(adapter => adapter.Ready);
                mediationList.Row("Adapters Started", $"{ready} of {adapters.Count} ready",
                    ready == adapters.Count ? HDCDebugStyle.GoodColor : HDCDebugStyle.WarnColor);
            }
            else
            {
                mediationList.Row("Adapters Started", "Waiting for Google Mobile Ads to start", HDCDebugStyle.MutedColor);
            }

            foreach (IMediationPartner partner in partners)
            {
                mediationList.Header(partner.Name);
                HDCAdapterStatus status = HDCMediationReport.Find(partner.AdapterClass);
                if (status != null)
                    AdapterRow("Adapter", status);
                else
                    mediationList.Row("Adapter", HDCMediationReport.Received ? "Not reported: not in this build, or no mediation group uses it" : "-",
                        HDCDebugStyle.MutedColor);
                if (partner.HasTestMode && partnerTestModes.TryGetValue(partner, out (bool On, string DeviceId) test))
                {
                    mediationList.Row("Test Mode", test.On ? "On" : "Off", test.On ? HDCDebugStyle.GoodColor : HDCDebugStyle.MutedColor);
                    if (!string.IsNullOrEmpty(test.DeviceId))
                        mediationList.Row("Test Device ID", test.DeviceId);
                }
            }

            List<HDCAdapterStatus> others = adapters.Where(adapter => partners.All(partner => partner.AdapterClass != adapter.AdapterClass)).ToList();
            if (others.Count > 0)
            {
                mediationList.Header("Other Adapters");
                foreach (HDCAdapterStatus adapter in others)
                    AdapterRow(ShortName(adapter.AdapterClass), adapter);
            }

            bool metaTest = partnerTestModes.Any(pair => pair.Key.HasTestMode && pair.Value.On);
            if (HDCAdsSdk.UseTestAdUnits)
                mediationList.Note(HDCDebugStyle.Colored(HDCDebugStyle.WarnHex,
                    "Test Ad Units đang bật: mọi vị trí load ad unit mẫu của Google, không đi qua mediation, nên Meta không bao giờ được hỏi. Tắt Test Ad Units ở thẻ HDC Ads rồi mở lại app."));
            if (metaTest)
                mediationList.Note("Meta Test Mode chỉ làm SDK Meta trả quảng cáo test khi Meta được hỏi. Nguồn nào thắng do mediation group của ad unit trên AdMob quyết định, "
                    + "nên phần lớn quảng cáo vẫn là của AdMob. Muốn chỉ nhận Meta: bấm Open Ad Inspector, chọn ad unit, Ad source testing, Meta Audience Network, rồi load lại quảng cáo.");
            if (!HDCAdsSdk.IsTestDevice)
                mediationList.Note(HDCDebugStyle.Colored(HDCDebugStyle.MutedHex,
                    "Ad Inspector chỉ mở trên thiết bị test của Google. Bấm Open Ad Inspector hoặc Meta On: máy được tự đăng ký làm thiết bị test trước."));
            if (!string.IsNullOrEmpty(adInspectorNote))
                mediationList.Note(adInspectorNote);
            mediationList.End();

            HDCDebugStyle.Highlight(metaOnButton, metaTest);
            HDCDebugStyle.Highlight(metaOffButton, !metaTest);
        }

        private void RedrawConsent()
        {
            HDCConsentGate consent = HDCAdsRuntime.Current.Context.Consent;
            consentList.Begin();
            consentList.Row("Step", ConsentStep(consent.Current), consent.IsDone ? HDCDebugStyle.GoodColor : HDCDebugStyle.WarnColor);
            if (consent.TogglesKnown)
            {
                consentList.Row("Ask Consent", consent.Toggles.Consent ? "Yes" : "No: ads_config.toggleConsent=false, counted as accepted",
                    consent.Toggles.Consent ? HDCDebugStyle.TextColor : HDCDebugStyle.WarnColor);
                consentList.Row("Ask ATT", consent.Toggles.Att ? "Yes" : "No: ads_config.toggleAtt=false, never asked",
                    consent.Toggles.Att ? HDCDebugStyle.TextColor : HDCDebugStyle.WarnColor);
                consentList.Row("Toggles From", consent.Toggles.Source);
            }

            consentList.Row("Consent Status", consent.ConsentSkipped ? consent.Status + " (not asked)" : consent.Status.ToString());
            if (consent.IsDone)
                consentList.Row("Can Request Ads", consent.CanRequestAds ? "Yes" : "No: no ad loads this session",
                    consent.CanRequestAds ? HDCDebugStyle.GoodColor : HDCDebugStyle.BadColor);
            consentList.Row("Privacy Options", consent.PrivacyOptionsRequired ? "Required: the game needs a button for it" : "Not required");
            consentList.Row("Tracking (ATT)", consent.TogglesKnown && !consent.Toggles.Att ? consent.TrackingStatus + " (not asked, counted as accepted)" : consent.TrackingStatus.ToString(),
                consent.TrackingStatus == HDCTrackingStatus.Authorized ? HDCDebugStyle.GoodColor : HDCDebugStyle.TextColor);
            consentList.Row("Asked As EEA", consent.DebugInEea ? "Yes: a Google test device sees the EEA form" : "No");
            if (!string.IsNullOrEmpty(consent.LastError))
                consentList.Row("Last Error", consent.LastError, HDCDebugStyle.BadColor);
#if UNITY_EDITOR
            consentList.Note(HDCDebugStyle.Colored(HDCDebugStyle.MutedHex, "Trong Editor không hỏi consent và ATT: coi như được phép."));
#endif
            if (!HDCAdsSdk.IsTestDevice)
                consentList.Note(HDCDebugStyle.Colored(HDCDebugStyle.MutedHex,
                    "Muốn thấy form consent như ở châu Âu: bấm Make Test Device ở thẻ HDC Ads, bấm Reset Consent, rồi mở lại app."));
            if (!string.IsNullOrEmpty(consentNote))
                consentList.Note(consentNote);
            consentList.End();
        }

        private static string ConsentStep(HDCConsentGate.Step step)
        {
            switch (step)
            {
                case HDCConsentGate.Step.NotStarted:
                    return "Not started";
                case HDCConsentGate.Step.WaitingForConfig:
                    return "Waiting for ads_config (first launch)";
                case HDCConsentGate.Step.Updating:
                    return "Asking the consent service";
                case HDCConsentGate.Step.ShowingForm:
                    return "Consent form on screen";
                case HDCConsentGate.Step.AskingTracking:
                    return "ATT question on screen";
                default:
                    return "Done";
            }
        }

        private void AdapterRow(string label, HDCAdapterStatus status) =>
            mediationList.Row(label, status.Ready
                    ? $"Ready · {status.LatencyMillis} ms"
                    : "Not ready" + (string.IsNullOrEmpty(status.Description) ? "" : " · " + status.Description),
                status.Ready ? HDCDebugStyle.GoodColor : HDCDebugStyle.BadColor);

        private static string ShortName(string adapterClass)
        {
            int dot = adapterClass.LastIndexOf('.');
            return dot >= 0 && dot < adapterClass.Length - 1 ? adapterClass.Substring(dot + 1) : adapterClass;
        }

        private void RedrawDevice()
        {
            Rect safe = Screen.safeArea;
            deviceList.Begin();
            deviceList.Row("Device Name", SystemInfo.deviceName);
            deviceList.Row("Device Model", SystemInfo.deviceModel);
            deviceList.Row("Device ID", HDCAdsRuntime.DeviceRegion.DeviceId);
            deviceList.Row("Device Type", SystemInfo.deviceType.ToString());
            deviceList.Row("Operating System", SystemInfo.operatingSystem);
            deviceList.Row("CPU", $"{SystemInfo.processorType} · {SystemInfo.processorCount} cores");
            deviceList.Row("Memory", SystemInfo.systemMemorySize.ToString("#,0", CultureInfo.InvariantCulture) + " MB");
            deviceList.Row("GPU", $"{SystemInfo.graphicsDeviceName} · {SystemInfo.graphicsMemorySize:#,0} MB");
            deviceList.Row("Screen", $"{Screen.width} x {Screen.height} · {Screen.dpi:0.#} dpi · {Screen.orientation}");
            deviceList.Row("Safe Area", $"x {safe.x:0}, y {safe.y:0}, {safe.width:0} x {safe.height:0}");
            deviceList.Row("Battery", SystemInfo.batteryLevel < 0f ? SystemInfo.batteryStatus.ToString() : $"{SystemInfo.batteryStatus} · {SystemInfo.batteryLevel * 100f:0}%");
            deviceList.End();
        }

        private void RedrawNetwork()
        {
            networkList.Begin();
            NetworkReachability reachability = Application.internetReachability;
            networkList.Row("Connection", Reachability(reachability), reachability == NetworkReachability.NotReachable ? HDCDebugStyle.BadColor : HDCDebugStyle.GoodColor);
            if (probing)
            {
                networkList.Row("Public Network", "Checking...", HDCDebugStyle.MutedColor);
            }
            else if (publicInfo != null)
            {
                networkList.Row("Public IP", Dash(publicInfo.ip));
                networkList.Row("Country", $"{Dash(publicInfo.country)} ({Dash(publicInfo.country_code)})");
                networkList.Row("Region / City", $"{Dash(publicInfo.region)} / {Dash(publicInfo.city)}");
                networkList.Row("Provider", Dash(!string.IsNullOrEmpty(publicInfo.connection?.isp) ? publicInfo.connection.isp : publicInfo.connection?.org));
                networkList.Row("IP Type", Dash(publicInfo.type));
                networkList.Row("Time Zone", $"{Dash(publicInfo.timezone?.id)} (UTC {Dash(publicInfo.timezone?.utc)})");
                if (Mathf.Abs(publicInfo.latitude) > 0f || Mathf.Abs(publicInfo.longitude) > 0f)
                    networkList.Row("Lat / Lon", $"{publicInfo.latitude:0.####}, {publicInfo.longitude:0.####}");
            }
            else
            {
                networkList.Row("Public Network", string.IsNullOrEmpty(probeError) ? "Not checked" : "Unavailable: " + probeError,
                    string.IsNullOrEmpty(probeError) ? HDCDebugStyle.MutedColor : HDCDebugStyle.WarnColor);
            }

            if (lastProbeClock.HasValue)
                networkList.Row("Checked At", lastProbeClock.Value.ToString("HH:mm:ss", CultureInfo.InvariantCulture));
            networkList.End();
        }

        private void RedrawAdjust()
        {
            adjustList.Begin();
            foreach ((string label, string value, Color color) in HDCAdjustStatus.Rows())
                adjustList.Row(label, value, color);
            string state = HDCAdjustProbe.State;
            adjustList.Row("State", state, state == "OK" ? HDCDebugStyle.GoodColor : state.StartsWith("Không có phản hồi") ? HDCDebugStyle.WarnColor : HDCDebugStyle.MutedColor);
            foreach (KeyValuePair<string, string> pair in HDCAdjustProbe.Ordered())
                adjustList.Row(pair.Key, pair.Value);
            adjustList.End();
        }

        private void ReadPartners()
        {
            partnerTestModes.Clear();
            foreach (IMediationPartner partner in HDCAdsRuntime.Current.Partners)
            {
                if (partner.HasTestMode)
                    partnerTestModes[partner] = (Try(() => partner.IsTestMode), Try(() => partner.TestDeviceId));
            }
        }

        private void Probe(bool force)
        {
            if (probing || (!force && Time.realtimeSinceStartup - lastProbe < ProbeCooldownSeconds))
                return;
#if HDC_WEB_REQUEST
            StartCoroutine(ProbePublicNetwork());
#else
            probeError = "the UnityWebRequest module is off";
#endif
        }

#if HDC_WEB_REQUEST
        private IEnumerator ProbePublicNetwork()
        {
            probing = true;
            MarkDirty();
            using (UnityWebRequest request = UnityWebRequest.Get(IpLookupUrl))
            {
                request.timeout = 6;
                yield return request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success)
                {
                    publicInfo = null;
                    probeError = string.IsNullOrEmpty(request.error) ? "lookup failed" : request.error;
                }
                else
                {
                    Apply(request.downloadHandler.text);
                }
            }

            lastProbe = Time.realtimeSinceStartup;
            lastProbeClock = DateTime.Now;
            probing = false;
            MarkDirty();
        }
#endif

        private void Apply(string json)
        {
            try
            {
                PublicInfo info = JsonUtility.FromJson<PublicInfo>(json);
                if (info != null && info.success)
                {
                    publicInfo = info;
                    probeError = null;
                    return;
                }

                publicInfo = null;
                probeError = string.IsNullOrEmpty(info?.message) ? "lookup failed" : info.message;
            }
            catch (Exception exception)
            {
                publicInfo = null;
                probeError = exception.Message;
            }
        }

        private static string NativeLibrary
        {
            get
            {
#if UNITY_EDITOR
                return "Editor simulation (no real ads)";
#elif UNITY_ANDROID
                return "Android (hdc-ads-android)";
#elif UNITY_IOS
                return "iOS (HDCAds.xcframework)";
#else
                return "Not supported on " + Application.platform;
#endif
            }
        }

        private static bool FirebaseBuilt
        {
            get
            {
#if HDC_FIREBASE
                return true;
#else
                return false;
#endif
            }
        }

        private static string ScriptingBackend
        {
            get
            {
#if ENABLE_IL2CPP
                return "IL2CPP";
#else
                return "Mono";
#endif
            }
        }

        private static string Reachability(NetworkReachability reachability)
        {
            switch (reachability)
            {
                case NetworkReachability.ReachableViaCarrierDataNetwork: return "Mobile data";
                case NetworkReachability.ReachableViaLocalAreaNetwork: return "Wi-Fi or LAN";
                default: return "Offline";
            }
        }

        private static T Try<T>(Func<T> read)
        {
            try
            {
                return read();
            }
            catch (Exception)
            {
                return default;
            }
        }

        private static string Dash(string value) => string.IsNullOrEmpty(value) ? "-" : value;
    }
}
