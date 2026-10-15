using System.Collections;
using System.Linq;
using HDC.Ads;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace HDC.Onboarding
{
    [DisallowMultipleComponent]
    [AddComponentMenu("HDC/Onboarding/HDC Splash")]
    public sealed class HDCSplash : MonoBehaviour
    {
        private const string AdMobAdapter = "admob";
        private const string NativeSplashPosition = "na";
        private const string InterstitialPosition = "it";
        private const string NativeAfterPosition = "naf";
        private const string NativePreloadPosition = "nap";
        private const float FirstLoadingSeconds = 30f;
        private const float NativePreloadTimeOut = 100f;

        [Tooltip("Waits until the device is online before the remote config and the ads.")]
        [SerializeField] private bool checkInternet = true;

        [Tooltip("Opens firstOpenNextScene of onboarding_config's splash_config on the first open, its nextScene after that.")]
        [SerializeField] private bool loadToNextScene = true;

        [Tooltip("Shows the no-internet popup and pauses the game while the device is offline.")]
        [SerializeField] private HDCNoInternet noInternet;

        [Tooltip("Where the native splash ad shows: the area of the HDCNativeSplashArea prefab. Empty: the HDCSplashAdArea of kind NativeSplash in the open scenes.")]
        [SerializeField] private RectTransform nativeSplashArea;

        [Tooltip("Where the native splash ad shows when AdMob filled it and enableAdmobLayout of onboarding_config's splash_config is on: the area of the HDCNativeSplashAdMobArea prefab. Empty: the HDCSplashAdArea of kind NativeSplashAdMob in the open scenes.")]
        [SerializeField] private RectTransform nativeSplashAdMobArea;

        [SerializeField] private UnityEvent onFirebaseInitialized = new UnityEvent();
        [SerializeField] private UnityEvent<float> onProgressChanged = new UnityEvent<float>();
        [SerializeField] private UnityEvent onSplashFinished = new UnityEvent();

        private HDCSplashConfig config;
        private HDCSplashPopupAd nativeSplash;
        private HDCSplashPopupAd nativePreload;
        private HDCSplashLaunchAd interstitial;
        private HDCSplashNativeAd nativeAfter;
        private bool interstitialShowing;
        private bool nativeSplashLoadEnd;
        private bool nativeAfterLoadEnd;
        private bool nativeAfterShowCalled;
        private bool nativeAfterShowEnd;
        private float progress;
        private float progressFrom;
        private float progressTo;
        private float progressSeconds;
        private float progressElapsed;
        private int progressRun;
        private bool progressRunning;

        public static bool IsFinished { get; private set; }

        public UnityEvent FirebaseInitialized => onFirebaseInitialized;

        public UnityEvent<float> ProgressChanged => onProgressChanged;

        public UnityEvent SplashFinished => onSplashFinished;

        public HDCSplashConfig Config => config;

        private bool InterstitialOnScreen => interstitialShowing && interstitial != null && interstitial.IsOnScreen;

        private bool NativeAfterShowsUnderRightAway
        {
            get
            {
#if UNITY_ANDROID
                return false;
#else
                return interstitial != null && interstitial.IsInterstitial;
#endif
            }
        }

        private void Awake()
        {
            HDCFirstSession.CheckAndCacheFirstOpenData();
            HDCFirstSession.EndFirstOpenSession();
            HDCAds.AppLaunch.IgnoreAds = true;
            if (transform.parent != null)
                transform.SetParent(null, false);
            DontDestroyOnLoad(gameObject);
        }

        private IEnumerator Start()
        {
            StartLoading(0.9f, FirstLoadingSeconds);
            HDCOnboardingLog.Log("Splash start");
            HDCOnboardingTracking.Track("4_admob_start");
            HDCConsentTracking.Begin();
            HDCOnboardingLog.Log("Waiting consent, tracking and admob...");
            yield return new WaitUntil(() => HDCOnboardingAds.AdsGateDone);
            HDCOnboardingTracking.Track("4_admob_end");
            HDCOnboardingLog.Log("Admob privacy gate completed");

            yield return new WaitUntil(() => HDCOnboardingAds.FirebaseChecked);
            HDCOnboardingLog.Log("Firebase initialized");
            HDCOnboardingTracking.Track("1_firebase_end");
            if (checkInternet)
            {
                HDCOnboardingTracking.Track("2_internet_start");
                HDCOnboardingLog.Log("Waiting for internet...");
                yield return new WaitUntil(IsInternetAvailable);
                HDCOnboardingTracking.Track("2_internet_end");
            }

            HDCOnboardingLog.Log("Internet available");
            HDCOnboardingTracking.Track("3_admob_start");
            HDCOnboardingLog.Log("Waiting Firebase remote & admob...");
            yield return new WaitUntil(() => HDCOnboardingAds.RemoteConfigDone && HDCOnboardingAds.AdsGateDone);
            onFirebaseInitialized.Invoke();
            HDCOnboardingTracking.Track("3_admob_end");
            HDCOnboardingLog.Log("Remote & Admob available");
            HDCOnboardingTracking.Track("4_remote_start");
            HDCOnboardingLog.Log("Load remote config");
            LoadRemoteConfiguration();
            HDCOnboardingLog.Log("Load remote end");
            HDCOnboardingTracking.Track("4_remote_end");

            StartLoading(0.9f, config.loadingTime > 0f ? config.loadingTime : FirstLoadingSeconds);
            if (!HDCAds.Consent.CanRequestAds)
            {
                HDCOnboardingLog.Warn("Ad request disabled by consent. Skip splash ads.");
                HDCOnboardingTracking.Track("5_load_ad_skip_consent");
                yield return FinishSplashWithoutAds();
                yield break;
            }

            if (HDCAds.IsAdsRemoved)
            {
                HDCOnboardingLog.Log("Ads are removed. Skip splash ads.");
                yield return FinishSplashWithoutAds();
                yield break;
            }

            HDCOnboardingTracking.Track("5_load_ad_start");
            HDCOnboardingLog.Log("Start load ads");
            Coroutine nativeSplashFlow = StartCoroutine(WaitNativeSplashAndShowing());
            Coroutine interstitialFlow = StartCoroutine(WaitInterstitialSplash());
            yield return nativeSplashFlow;
            yield return interstitialFlow;
            HDCOnboardingLog.Log("Start load end");
            HDCOnboardingTracking.Track("5_load_ad_end");
            LoadNativeAfterInterstitial();
            HDCOnboardingLog.Log("Start load next scene");
            yield return Loading(1f, 0.25f);

            if (loadToNextScene)
                yield return LoadNextScene();

            if (nativeSplash != null)
                HDCOnboardingAds.HidePopup(nativeSplash.GroupName);

            yield return new WaitForSeconds(config.delayShowInterstitials);
            yield return ShowInterstitialAndNativeAfter();
            HDCOnboardingLog.Log("Splash end show ad");
            Finish();
            interstitial?.Destroy();
        }

        private void Update()
        {
            if (!progressRunning || !SplashScreen.isFinished)
                return;
            progressElapsed += Time.deltaTime;
            float t = progressSeconds > 0f ? Mathf.Clamp01(progressElapsed / progressSeconds) : 1f;
            SetProgress(Mathf.LerpUnclamped(progressFrom, progressTo, 1f - (1f - t) * (1f - t)));
            if (t >= 1f)
                progressRunning = false;
        }

        private void OnDestroy()
        {
            nativeSplash?.Release();
            nativePreload?.Release();
            if (nativeAfter != null)
                nativeAfter.StatusChanged -= OnNativeAfterInterstitialChangeStatus;
        }

        private IEnumerator FinishSplashWithoutAds()
        {
            yield return Loading(1f, 0.25f);
            if (loadToNextScene)
                yield return LoadNextScene();
            HDCOnboardingLog.Log("Splash end without ads");
            Finish();
        }

        private void Finish()
        {
            IsFinished = true;
            HDCAds.Channels.AppLaunch.CompleteWithoutShowing("the splash showed the launch ads");
            onSplashFinished.Invoke();
            HDCOnboardingTracking.Track("6_end_splash");
            HDCOnboardingLog.Log("Splash done");
        }

        private void LoadRemoteConfiguration()
        {
            string rawConfig = HDCOnboardingConfig.Raw();
            config = HDCOnboardingConfig.Parse(rawConfig).splash_config;
            HDCOnboardingLog.Log($"Remote config loaded raw={rawConfig}");
            if (string.IsNullOrWhiteSpace(rawConfig))
                HDCOnboardingLog.Error("onboarding_config is empty, so the splash has no ads and no next scene: " +
                                       "add it with HDC > Onboarding > Add config keys and to Remote Config.");
        }

        private IEnumerator LoadNextScene()
        {
            string nextScene = HDCFirstSession.IsFirstOpen ? config.firstOpenNextScene : config.nextScene;
            if (string.IsNullOrEmpty(nextScene))
            {
                HDCOnboardingLog.Error("onboarding_config.splash_config names no next scene, so the splash scene stays open.");
                yield break;
            }

            if (!Application.CanStreamedLevelBeLoaded(nextScene))
            {
                HDCOnboardingLog.Error($"Scene '{nextScene}' is not in Build Settings.");
                yield break;
            }

            yield return SceneManager.LoadSceneAsync(nextScene);
        }

        private bool IsInternetAvailable() =>
            noInternet != null ? noInternet.IsInternetAvailable : Application.internetReachability != NetworkReachability.NotReachable;

        private IEnumerator WaitInterstitialSplash()
        {
            interstitial = new HDCSplashLaunchAd(InterstitialPosition, config.launchChannel, config.interLoadAdTimeout);
            if (string.IsNullOrEmpty(interstitial.AdUnitId))
                yield break;

            if (config.waitNativeSplashLoad)
                yield return new WaitUntil(() => nativeSplashLoadEnd);
            HDCOnboardingTracking.Track("5_it_l_s");
            HDCOnboardingLog.Log($"Interstitial splash load start group={interstitial.AdUnitId} type={interstitial.Type}");
            yield return interstitial.Load();
            HDCOnboardingLog.Log($"Interstitial splash load status={interstitial.IsReady}");
            HDCOnboardingTracking.Track(interstitial.IsReady ? "5_it_l_e" : "5_it_l_m");
        }

        private void OnNativeAfterInterstitialChangeStatus(HDCSplashAdStatus status)
        {
            if (status == HDCSplashAdStatus.Loaded)
            {
                HDCOnboardingTracking.Track("5_naf_l_e");
                LoadNativePreload();
            }

            if (status == HDCSplashAdStatus.Loaded && interstitialShowing && this != null)
                StartCoroutine(ShowNativeAfterInterstitial());

            if (status == HDCSplashAdStatus.LoadFailed)
            {
                LoadNativePreload();
                HDCOnboardingTracking.Track("5_naf_l_m");
            }

            HDCOnboardingLog.Log($"Native after interstitials splash status: {status}");
        }

        private void LoadNativeAfterInterstitial()
        {
            if (string.IsNullOrEmpty(config.nativeAfterInterId))
            {
                nativeAfterLoadEnd = true;
                return;
            }

            nativeAfter = new HDCSplashNativeAd(NativeAfterPosition, config.nativeAfterInterId, config.nativeAfterLayoutGroup);
            nativeAfter.StatusChanged += OnNativeAfterInterstitialChangeStatus;
            HDCOnboardingTracking.Track("5_naf_l_s");
            HDCOnboardingLog.Log($"Native after interstitials load start group={config.nativeAfterInterId}");
            StartCoroutine(LoadNativeAfter());
        }

        private IEnumerator LoadNativeAfter()
        {
            yield return nativeAfter.Load();
            HDCOnboardingLog.Log($"Native after interstitials load status={nativeAfter.Status}");
            nativeAfterLoadEnd = true;
        }

        private IEnumerator ShowInterstitialAndNativeAfter()
        {
            if (!interstitial.IsReady)
            {
                HDCOnboardingLog.Log($"Interstitial {interstitial.Position} load failed skipped");
                if (!nativeAfterLoadEnd)
                    yield return new WaitUntil(() => nativeAfterLoadEnd);
                yield return ShowNativeAfterInterstitial();
                yield break;
            }

            HDCOnboardingLog.Log($"Interstitial {interstitial.Position} splash showing isNativeAfterInterstitialEnd={nativeAfterLoadEnd}");
            HDCOnboardingTracking.Track($"5_{interstitial.Position}_s_s");
            interstitialShowing = true;
#if UNITY_ANDROID
            if (interstitial.IsInterstitial)
                nativeAfter?.ArmUnder(interstitial.Ad.Id);
#endif
            Coroutine interstitialShow = StartCoroutine(interstitial.Show());
            Coroutine nativeAfterShow = nativeAfterLoadEnd ? StartCoroutine(ShowNativeAfterInterstitial()) : null;
            yield return interstitialShow;
            if (nativeAfterShow != null)
                yield return nativeAfterShow;
            HDCOnboardingLog.Log($"Interstitial {interstitial.Position} splash showing completed");

            if (nativeAfterShowCalled)
            {
                HDCOnboardingLog.Log("Wait show native after interstitials splash showing");
                yield return new WaitUntil(() => nativeAfterShowEnd);
                HDCOnboardingLog.Log("Native after interstitials splash showing completed");
            }

            HDCOnboardingTracking.Track($"5_{interstitial.Position}_s_e");
        }

        private IEnumerator ShowNativeAfterInterstitial()
        {
            if (nativeAfterShowCalled)
            {
                HDCOnboardingLog.Log("Native After Inter skipped by is caller");
                yield break;
            }

            if (nativeAfter == null)
            {
                HDCOnboardingLog.Log("Native After Inter skipped by not initialize");
                yield break;
            }

            if (!nativeAfter.IsReady)
            {
                HDCOnboardingLog.Log($"Native After Inter {nativeAfter.Position} load failed skipped status={nativeAfter.Status}");
                yield break;
            }

            nativeAfterShowCalled = true;
            HDCOnboardingLog.Log($"Native after interstitials {nativeAfter.Position} splash showing");
            HDCOnboardingTracking.Track($"5_{nativeAfter.Position}_s_s");
            yield return nativeAfter.Show(NativeAfterShowsUnderRightAway ? null : (System.Func<bool>)(() => InterstitialOnScreen));
            HDCOnboardingLog.Log($"Native after interstitials {nativeAfter.Position} splash showing completed");
            HDCOnboardingTracking.Track($"5_{nativeAfter.Position}_s_e");
            nativeAfterShowEnd = true;
        }

        private IEnumerator WaitNativeSplashAndShowing()
        {
            if (string.IsNullOrEmpty(config.nativeSplashGroup))
            {
                nativeSplashLoadEnd = true;
                yield break;
            }

            nativeSplash = new HDCSplashPopupAd(config.nativeSplashGroup, NativeSplashPosition, config.nativeSplashLoadTimeOut,
                config.nativeSplashShowTime);
            HDCOnboardingTracking.Track("5_na_l_s");
            HDCOnboardingLog.Log($"Native splash load start group={config.nativeSplashGroup}");
            yield return nativeSplash.Load();
            nativeSplashLoadEnd = true;
            if (nativeSplash.Status == HDCSplashAdStatus.Loaded)
            {
                HDCOnboardingLog.Log("Native splash showing");
                HDCOnboardingTracking.Track("5_na_l_e");
                HDCOnboardingTracking.Track("5_na_s_s");
                bool adMob = nativeSplash.AdSourceAdapterClassName.ToLowerInvariant().Contains(AdMobAdapter) && config.enableAdmobLayout;
                yield return nativeSplash.Show(adMob
                    ? AreaFor(nativeSplashAdMobArea, HDCSplashAdAreaKind.NativeSplashAdMob)
                    : AreaFor(nativeSplashArea, HDCSplashAdAreaKind.NativeSplash));
                HDCOnboardingTracking.Track("5_na_s_e");
                HDCOnboardingLog.Log("Native splash shown & completed");
            }
            else
            {
                HDCOnboardingTracking.Track("5_na_l_m");
            }
        }

        internal static RectTransform AreaFor(RectTransform assigned, HDCSplashAdAreaKind kind)
        {
            if (assigned != null)
                return assigned;
            HDCSplashAdArea found = FindObjectsOfType<HDCSplashAdArea>().FirstOrDefault(area => area.Kind == kind);
            return found != null ? found.Area : null;
        }

        private void LoadNativePreload()
        {
            if (!HDCFirstSession.IsFirstOpen || this == null)
                return;

            string rawConfig = HDCOnboardingConfig.Raw();
            if (string.IsNullOrEmpty(rawConfig))
                return;

            HDCIntroConfig introConfig = HDCOnboardingConfig.Parse(rawConfig).intro_config;
            string preloadGroup = introConfig.Configs.Length > 0 ? introConfig.Configs[0]?.AdConfig?.GroupName : null;
            if (string.IsNullOrEmpty(preloadGroup))
                return;

            nativePreload = new HDCSplashPopupAd(preloadGroup, NativePreloadPosition, NativePreloadTimeOut, 0f);
            HDCOnboardingTracking.Track("5_nap_l_s");
            HDCOnboardingLog.Log($"Native preload load start group={preloadGroup}");
            StartCoroutine(LoadNativePreload(nativePreload));
        }

        private IEnumerator LoadNativePreload(HDCSplashPopupAd preload)
        {
            yield return preload.Load();
            HDCOnboardingLog.Log($"Native preload load status={preload.Status}");
            HDCOnboardingTracking.Track(preload.Status == HDCSplashAdStatus.Loaded ? "5_nap_l_e" : "5_nap_l_m");
        }

        private void StartLoading(float value, float seconds)
        {
            progressRun++;
            progressFrom = progress;
            progressTo = value;
            progressSeconds = seconds;
            progressElapsed = 0f;
            progressRunning = true;
        }

        private IEnumerator Loading(float value, float seconds)
        {
            StartLoading(value, seconds);
            int run = progressRun;
            while (progressRunning && run == progressRun)
                yield return null;
        }

        private void SetProgress(float value)
        {
            progress = value;
            onProgressChanged.Invoke(value);
        }

#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForPlayMode() => IsFinished = false;
#endif
    }
}
