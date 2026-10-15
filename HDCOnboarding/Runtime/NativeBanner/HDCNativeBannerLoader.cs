using System.Collections;
using System.Collections.Generic;
using HDC.Ads;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HDC.Onboarding
{
    [DisallowMultipleComponent]
    [AddComponentMenu("HDC/Onboarding/HDC Native Banner Loader")]
    public sealed class HDCNativeBannerLoader : MonoBehaviour
    {
        private const string MetaSource = "meta";
        private const float FirstRetrySeconds = 2f;
        private const float MaxRetrySeconds = 64f;

        [Tooltip("Starts by itself once the scene named activeSceneName of first_native_banner_config is open. Off: call Initialize.")]
        [SerializeField] private bool autoInitialize = true;

        [Tooltip("Where the native banners show.")]
        [SerializeField] private RectTransform bannerArea;

        private static HDCNativeBannerLoader current;

        private readonly List<Banner> banners = new List<Banner>();
        private HDCNativeBannerConfig config;
        private float retrySeconds = FirstRetrySeconds;
        private string lastShowing;
        private bool initialized;

        public static bool IsRunning { get; private set; }

        public HDCNativeBannerConfig Config => config;

        public void Initialize()
        {
            if (initialized || config?.bannerLoops == null)
                return;
            initialized = true;
            HDCOnboardingLog.Log("NativeBanner initialize start");
            foreach (HDCNativeBannerLoop loop in config.bannerLoops)
            {
                if (loop != null)
                    banners.Add(new Banner(loop));
            }

            HDCOnboardingLog.Log($"NativeBanner initialized loops={banners.Count}");
            StartCoroutine(Run());
        }

        internal static float HoldSeconds(HDCNativeBannerLoop loop, string adSource) =>
            (adSource ?? "").ToLowerInvariant().Contains(MetaSource) ? loop.metaTimeConfig : loop.defaultTimeConfig;

        private void Awake()
        {
            if (current != null && current != this)
            {
                Destroy(gameObject);
                return;
            }

            current = this;
            if (transform.parent != null)
                transform.SetParent(null, false);
            DontDestroyOnLoad(gameObject);
        }

        private IEnumerator Start()
        {
            if (current != this)
                yield break;
            yield return new WaitUntil(() => HDCOnboardingAds.RemoteConfigDone && HDCOnboardingAds.SdkReady);

            string rawConfig = HDCNativeBannerConfig.Raw();
            config = HDCNativeBannerConfig.Parse(rawConfig);
            if (config?.bannerLoops == null)
            {
                HDCOnboardingLog.Warn($"NativeBanner config invalid or empty, raw={rawConfig}");
                yield break;
            }

            if (autoInitialize && SceneManager.GetActiveScene().name == config.activeSceneName)
                Initialize();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            if (current != this)
                return;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            foreach (Banner banner in banners)
                banner.Ad.Release();
            current = null;
            IsRunning = false;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (autoInitialize && !initialized && scene.name == config.activeSceneName)
                Initialize();
        }

        private IEnumerator Run()
        {
            IsRunning = true;
            int index = 0;
            while (true)
            {
                if (!banners.Exists(banner => banner.Loop.HasShowsLeft))
                {
                    HDCOnboardingLog.Log("NativeBanner reload loop ended (no banner waiting)");
                    break;
                }

                if (HDCAds.IsAdsRemoved)
                {
                    HideLast();
                    HDCOnboardingLog.Log("NativeBanner stopped: ads are removed");
                    break;
                }

                Banner banner = banners[index];
                if (banner.Loop.HasShowsLeft)
                    yield return LoadAndShow(banner);
                index = (index + 1) % banners.Count;
            }

            IsRunning = false;
        }

        private IEnumerator LoadAndShow(Banner banner)
        {
            HDCNativeBannerLoop loop = banner.Loop;
            yield return null;
            HDCOnboardingLog.Log($"NativeBanner load start group={loop.groupName}");
            HDCSplashAdStatus status;
            if (banner.HasAdThatDidNotShow)
            {
                HDCOnboardingLog.Log($"NativeBanner group={loop.groupName} still has an ad that did not show: showing it instead of loading another");
                status = HDCSplashAdStatus.Loaded;
            }
            else
            {
                if (lastShowing == loop.groupName)
                {
                    HideLast();
                    lastShowing = null;
                }

                yield return banner.Load();
                status = banner.Ad.Status;
            }

            if (status == HDCSplashAdStatus.Loaded)
            {
                retrySeconds = FirstRetrySeconds;
                int shownBefore = banner.ShownCount;
                yield return HDCOnboardingAds.ShowPopupUntilShown(loop.groupName, bannerArea, null);
                if (banner.ShownCount == shownBefore)
                {
                    yield return WaitBeforeRetry($"show failed group={loop.groupName}");
                    yield break;
                }

                HideLast();
                lastShowing = loop.groupName;
                float timeShowing = HoldSeconds(loop, banner.Ad.AdSourceAdapterClassName);
                if (loop.adShowCount > 0)
                    loop.adShowCount--;
                HDCOnboardingLog.Log($"NativeBanner loaded group={loop.groupName} source={banner.Ad.AdSourceAdapterClassName} showTime={timeShowing} remain={loop.adShowCount}");
                yield return new WaitForSeconds(timeShowing);
            }
            else if (status == HDCSplashAdStatus.LoadFailed)
            {
                yield return WaitBeforeRetry($"load failed group={loop.groupName}");
            }
        }

        private IEnumerator WaitBeforeRetry(string reason)
        {
            float seconds = retrySeconds;
            HDCOnboardingLog.Warn($"NativeBanner {reason} retryIn={seconds}s");
            retrySeconds = Mathf.Min(retrySeconds * 2f, MaxRetrySeconds);
            yield return new WaitForSeconds(seconds);
        }

        private void HideLast()
        {
            if (string.IsNullOrEmpty(lastShowing))
                return;
            HDCOnboardingLog.Log($"NativeBanner hide previous group={lastShowing}");
            HDCOnboardingAds.HidePopup(lastShowing);
        }

#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForPlayMode()
        {
            current = null;
            IsRunning = false;
        }
#endif

        private sealed class Banner
        {
            private readonly HDCPopupEvents.Counts counts;
            private int shownAtLoad = -1;

            internal Banner(HDCNativeBannerLoop loop)
            {
                Loop = loop;
                Ad = new HDCSplashPopupAd(loop.groupName, loop.groupName, loop.timeOut, 0f, false);
                counts = HDCPopupEvents.Of(HDCOnboardingAds.PopupInstance(loop.groupName));
            }

            internal HDCNativeBannerLoop Loop { get; }

            internal HDCSplashPopupAd Ad { get; }

            internal int ShownCount => counts.Shown;

            internal bool HasAdThatDidNotShow =>
                shownAtLoad >= 0 && counts.Shown == shownAtLoad && HDCAds.Popup.IsGroupReady(Loop.groupName);

            internal IEnumerator Load()
            {
                shownAtLoad = counts.Shown;
                yield return Ad.Load();
            }
        }
    }
}
