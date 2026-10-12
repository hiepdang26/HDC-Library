using System.Collections;
using System.Globalization;
using HDC.Ads;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HDC.Onboarding
{
    [DisallowMultipleComponent]
    [AddComponentMenu("HDC/Onboarding/HDC Intro")]
    public sealed class HDCIntro : MonoBehaviour
    {
        [Tooltip("Views for the AR, AL and PT pages. A page uses the last view that shows its type.")]
        [SerializeField] private HDCIntroView[] views = new HDCIntroView[0];

        private HDCIntroConfig config;
        private HDCWaitToken showAdToken;

        public static bool IsFinished { get; private set; }

        public HDCIntroConfig Config => config;

        public static bool IsCompleteIntro(string introName) => PlayerPrefs.GetInt(HDCOnboardingKeys.IntroPrefix + introName, 0) == 1;

        public static void CompleteIntro(string introName)
        {
            PlayerPrefs.SetInt(HDCOnboardingKeys.IntroPrefix + introName, 1);
            PlayerPrefs.Save();
        }

        private static bool IsCompleteIntro(int index) => IsCompleteIntro(index.ToString(CultureInfo.InvariantCulture));

        private static void CompleteIntro(int index) => CompleteIntro(index.ToString(CultureInfo.InvariantCulture));

        private void Awake() => HDCOnboardingInput.EnsureEventSystem();

        private IEnumerator Start()
        {
            HDCOnboardingTracking.Track("7_start_intro");
            HDCOnboardingLog.Log("Intro start");
            string rawConfig = HDCOnboardingConfig.Raw();
            HDCOnboardingLog.Log($"Intro remote config loaded raw={rawConfig}");
            config = HDCOnboardingConfig.Parse(rawConfig).intro_config;
            HDCOnboardingLog.Log($"Intro config parsed count={config.Configs.Length}");

            int index = 0;
            foreach (HDCIntroPage intro in config.Configs)
            {
                HDCOnboardingTracking.Track($"7_start_intro_{index}");
                HDCOnboardingLog.Log($"Intro [{index}] start type={intro.IntroType} interval={intro.IntroInterval}");

                HDCIntroView viewSelect = views.Length > 0 ? views[0] : null;
                bool hasView = false;
                bool isCompleteIntro = IsCompleteIntro(index);
                if (intro.EnableCompleteIntro && isCompleteIntro)
                    continue;
                foreach (HDCIntroView view in views)
                {
                    if (view == null)
                        continue;
                    bool canView = view.CanRender(intro);
                    if (canView)
                    {
                        viewSelect = view;
                        hasView = true;
                    }

                    view.gameObject.SetActive(canView);
                }

                showAdToken = new HDCWaitToken();
                if (index + 1 < config.Configs.Length)
                    StartCoroutine(DelayAndPreloadAd(config.Configs[index + 1], intro.TimeDelayPreload));

                if (intro.IntroType != HDCIntroType.FORCE_AD && !hasView)
                {
                    HDCOnboardingLog.Error($"Intro [{index}] has page type {(int)intro.IntroType}, which no view shows: skipped");
                    CompleteIntro(index);
                    index++;
                    continue;
                }

                if (intro.IntroType != HDCIntroType.FORCE_AD)
                {
                    ShowAd(viewSelect, intro, showAdToken);
                    HDCOnboardingLog.Log($"Intro [{index}] view selected={viewSelect.name}");
                    viewSelect.Render(intro);
                    yield return new WaitUntil(() => viewSelect.IsComplete);
                    HDCOnboardingLog.Log($"Intro [{index}] view completed={viewSelect.name}");
                    CancelPendingShowAd();
                }
                else if (intro.AdConfig.PreloadType == HDCIntroPreload.ForceAd)
                {
                    yield return ShowForceAdAndClose(intro);
                }
                else
                {
                    ShowAd(viewSelect, intro, showAdToken);
                }

                HideAd(intro);
                if (viewSelect != null)
                    viewSelect.gameObject.SetActive(false);
                CompleteIntro(index);
                index++;
            }

            IsFinished = true;
            LoadNextScene();
            HDCOnboardingTracking.Track("7_end_intro");
            HDCOnboardingLog.Log("Intro done");
        }

        private void OnDestroy() => CancelPendingShowAd();

        private void LoadNextScene()
        {
            if (string.IsNullOrEmpty(config.nextSceneName))
            {
                HDCOnboardingLog.Error("onboarding_config.intro_config names no nextSceneName, so the intro scene stays open.");
                return;
            }

            if (!Application.CanStreamedLevelBeLoaded(config.nextSceneName))
            {
                HDCOnboardingLog.Error($"Scene '{config.nextSceneName}' is not in Build Settings.");
                return;
            }

            SceneManager.LoadScene(config.nextSceneName);
        }

        private static IEnumerator ShowForceAdAndClose(HDCIntroPage intro)
        {
            string groupName = intro.AdConfig.GroupName;
            bool ready = HDCOnboardingAds.CanShowForceAd(groupName);
            HDCOnboardingLog.Log($"Intro show ad {groupName} ready={ready}");
            if (!ready)
                yield break;
            bool closed = false;
            HDCAds.ForceAd.Show(groupName, () => closed = true);
            yield return new WaitUntil(() => closed);
        }

        private IEnumerator DelayAndPreloadAd(HDCIntroPage intro, float delay)
        {
            yield return new WaitForSeconds(delay);
            PreloadAd(intro);
        }

        private void CancelPendingShowAd()
        {
            if (showAdToken == null)
                return;
            showAdToken.Cancel();
            showAdToken = null;
        }

        private static void HideAd(HDCIntroPage intro)
        {
            if (intro.AdConfig.PreloadType != HDCIntroPreload.Popup)
                return;
            HDCOnboardingLog.Log($"Popup {intro.AdConfig.GroupName} hide");
            HDCOnboardingAds.HidePopup(intro.AdConfig.GroupName);
        }

        private void ShowAd(HDCIntroView view, HDCIntroPage intro, HDCWaitToken token)
        {
            HDCIntroAd showConfig = intro.AdConfig;
            if (showConfig.PreloadType != HDCIntroPreload.Popup && showConfig.PreloadType != HDCIntroPreload.ForceAd)
                return;

            if (IsAdReady(showConfig))
            {
                HDCOnboardingLog.Log($"{showConfig.PreloadType} {showConfig.GroupName} ready, showing");
                StartCoroutine(ShowAdNow(view, showConfig, token));
            }
            else
            {
                StartCoroutine(WaitAndShowAd(view, showConfig, token));
            }
        }

        private static IEnumerator WaitAndShowAd(HDCIntroView view, HDCIntroAd showConfig, HDCWaitToken token)
        {
            HDCOnboardingLog.Log($"{showConfig.PreloadType} {showConfig.GroupName} not ready, waiting...");
            while (!IsAdReady(showConfig))
            {
                yield return null;
                if (!token.Cancelled)
                    continue;
                HDCOnboardingLog.Log($"{showConfig.PreloadType} {showConfig.GroupName} wait canceled, skip showing");
                yield break;
            }

            HDCOnboardingLog.Log($"{showConfig.PreloadType} {showConfig.GroupName} ready after waiting, showing");
            yield return ShowAdNow(view, showConfig, token);
        }

        private static bool IsAdReady(HDCIntroAd showConfig) =>
            showConfig.PreloadType == HDCIntroPreload.Popup
                ? HDCOnboardingAds.IsPopupReady(showConfig.GroupName)
                : HDCOnboardingAds.CanShowForceAd(showConfig.GroupName);

        private static IEnumerator ShowAdNow(HDCIntroView view, HDCIntroAd showConfig, HDCWaitToken token)
        {
            if (showConfig.PreloadType == HDCIntroPreload.Popup)
                yield return HDCOnboardingAds.ShowPopupUntilShown(showConfig.GroupName, view != null ? view.AdArea : null, token);
            else if (!string.IsNullOrEmpty(showConfig.GroupName))
                HDCAds.ForceAd.Show(showConfig.GroupName);
        }

        private static void PreloadAd(HDCIntroPage intro)
        {
            HDCIntroAd preloadConfig = intro.AdConfig;
            if (preloadConfig.PreloadType == HDCIntroPreload.Popup)
            {
                HDCOnboardingLog.Log($"Popup {preloadConfig.GroupName} preload");
                HDCOnboardingAds.InitializePopup(preloadConfig.GroupName);
            }
            else if (preloadConfig.PreloadType == HDCIntroPreload.ForceAd)
            {
                HDCOnboardingLog.Log($"ForceAd {preloadConfig.GroupName} preload");
                HDCOnboardingAds.InitializeForceAd(preloadConfig.GroupName);
            }
        }

#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForPlayMode() => IsFinished = false;
#endif
    }
}
