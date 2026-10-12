using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
#if HDC_ADS
using HDC.Ads.Logic;
#endif
#if HDC_ADS && !HDC_FIREBASE
using HDC.Ads.Composition;
#endif

namespace HDC.Ads
{
    [DisallowMultipleComponent]
    [AddComponentMenu("HDC/HDC Ads Setup")]
    public sealed class HDCAdsSetup : MonoBehaviour
    {
        [Tooltip("Scene opened once the ads are started. It must be in Build Settings. Leave it empty to stay in this scene.")]
        [SerializeField] private string nextScene = "";

#pragma warning disable CS0169, CS0414
        [Tooltip("Opens the next scene after this many seconds even if the ads are not done.")]
        [SerializeField] private float maxWaitSeconds = 30f;

        [Tooltip("Also starts the channels whose autoInit is off in the ads config, so the next scene finds their ads loaded.")]
        [SerializeField] private bool startAllChannels = true;

        [Tooltip("Shows the app launch ad and waits for it before the next scene. Turn it off when HDCSplash shows the launch ads instead.")]
        [SerializeField] private bool runAppLaunch = true;

        [SerializeField, HideInInspector] private bool debugLog;

        [SerializeField, HideInInspector] private bool googleTestAds;

        [SerializeField, HideInInspector] private bool googleTestAdUnits;
#pragma warning restore CS0169, CS0414

        [Tooltip("Called once HDCAds is initialized.")]
        [SerializeField] private UnityEvent onAdsReady = new UnityEvent();

        [Tooltip("Called when the setup is done, right before the next scene opens.")]
        [SerializeField] private UnityEvent onFinished = new UnityEvent();

#if HDC_ADS
        public static void InitializeAds(Action onReady = null)
        {
            HDCAdsSettings defaults = HDCAdsSettings.Load();
#if HDC_FIREBASE
            HDCRemoteConfig.FetchAndInitialize(defaults.AdsConfig, defaults.CoreConfigsByKey(), onReady);
#else
            HDCConfigChoice choice = HDCAdsRuntime.ConfigSelection().Select(new HDCConfigInputs
            {
                Mode = HDCConfigMode.NoRemoteConfig,
                DefaultAds = defaults.AdsConfig,
                DefaultCore = defaults.CoreConfig,
                CustomDefaults = HDCCustomConfig.Defaults,
                SavedCustom = HDCCustomConfig.SavedValue,
                CountryRules = defaults.CountryRulesJson(),
                CountryAds = defaults.CountryAdsConfig,
                CountryCore = defaults.CountryCoreConfig,
                CustomCountry = defaults.CustomCountryValues(),
            });
            HDCCustomConfig.Apply(choice.CustomValues(), choice.SaveCustom);
            HDCAds.Initialize(choice.Ads, choice.Core, onReady);
#endif
        }
#endif

        private IEnumerator Start()
        {
            AsyncOperation loading = LoadNextScene();
#if HDC_ADS
            if (debugLog)
                HDCAds.Testing.DebugLog = true;
            if (googleTestAds)
            {
                HDCAds.Testing.EnableTestDevice();
                Debug.LogWarning("[HDCAds] Google test ads are on (HDC Build Tool preset > Google test ads): turn them off before release.");
            }

            if (googleTestAdUnits)
            {
                HDCAds.Testing.UseTestAdUnits = true;
                Debug.LogWarning("[HDCAds] Google test ad units replace the configured ones (HDC Build Tool preset > Test ad units): turn them off before release.");
            }

            if (HDCAds.IsInitialized)
                onAdsReady.Invoke();
            else
                InitializeAds(OnAdsReady);

            float deadline = Time.realtimeSinceStartup + maxWaitSeconds;
            while (runAppLaunch && !HDCAds.AppLaunch.IsCompleted && Time.realtimeSinceStartup < deadline)
                yield return null;
#else
            Debug.LogWarning("[HDCAds] HDC ads are off (HDC > Ads > Enable), so the setup only opens the next scene.");
            yield return null;
#endif
            onFinished.Invoke();
            if (loading != null)
                loading.allowSceneActivation = true;
        }

        private AsyncOperation LoadNextScene()
        {
            if (string.IsNullOrEmpty(nextScene))
                return null;
            if (!Application.CanStreamedLevelBeLoaded(nextScene))
            {
                Debug.LogError($"[HDCAds] Scene '{nextScene}' is not in Build Settings.");
                return null;
            }

            AsyncOperation loading = SceneManager.LoadSceneAsync(nextScene);
            loading.allowSceneActivation = false;
            return loading;
        }

#if HDC_ADS
        private void OnAdsReady()
        {
            if (runAppLaunch)
                HDCAds.AppLaunch.Initialize();
            if (startAllChannels)
                StartChannels();
            onAdsReady.Invoke();
        }

        private static void StartChannels()
        {
            foreach (IAdChannel channel in HDCAds.Channels.All)
                channel.InitializeAll();
        }
#endif
    }
}
