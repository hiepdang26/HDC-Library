using System;
using System.Collections.Generic;
using Firebase;
using Firebase.Extensions;
using Firebase.RemoteConfig;
using HDC.Ads.Internal;
using UnityEngine;

namespace HDC.Ads
{
    /// <summary>
    /// Reads the ads configs from Firebase Remote Config: <see cref="AdsConfigKey"/>, then the ad core config
    /// under the key its <see cref="HDCAdsConfig.selectedAdCoreName"/> names. A value Remote Config does not
    /// have comes from the one saved on the device by the last run, then from the defaults. Every value used
    /// is saved on the device for the next run. In the Editor the defaults are used as they are, unless
    /// <see cref="FetchInEditor"/> is on.
    /// </summary>
    public static class HDCRemoteConfig
    {
        public const string AdsConfigKey = "ads_config";

        /// <summary>
        /// Off by default: in the Editor the default configs apply as they are, without Firebase or the values
        /// saved by earlier runs, so edits to the defaults show at once. Turn it on to fetch in the Editor too.
        /// </summary>
        public static bool FetchInEditor { get; set; }

        /// <summary>
        /// Fetches the configs and hands them to <paramref name="onLoaded"/> as (ads config, ad core config).
        /// When Firebase is unavailable, or the fetch takes longer than <paramref name="timeoutSeconds"/>, the
        /// saved or default values are used.
        /// </summary>
        public static void Fetch(
            string defaultAdsConfig,
            IDictionary<string, string> defaultCoreConfigs,
            Action<string, string> onLoaded,
            float timeoutSeconds = 10f)
        {
            HDCMainThread.EnsureCreated();
            bool defaultsOnly = Application.isEditor && !FetchInEditor;
            var fetch = new Request(defaultAdsConfig, defaultCoreConfigs, onLoaded, defaultsOnly);
            if (defaultsOnly)
            {
                HDCMainThread.Post(() => fetch.Finish("defaults in the Editor"));
                return;
            }

            HDCMainThread.PostDelayed(timeoutSeconds, () => fetch.Finish("timeout"));
            fetch.Start();
        }

        /// <summary>Fetches the configs, then initializes <see cref="HDCAds"/> with them.</summary>
        public static void FetchAndInitialize(
            string defaultAdsConfig,
            IDictionary<string, string> defaultCoreConfigs,
            Action onInitialized = null,
            float timeoutSeconds = 10f)
        {
            Fetch(defaultAdsConfig, defaultCoreConfigs, (ads, core) => HDCAds.Initialize(ads, core, onInitialized), timeoutSeconds);
        }

        private sealed class Request
        {
            private readonly string defaultAdsConfig;
            private readonly IDictionary<string, string> defaultCoreConfigs;
            private readonly Action<string, string> onLoaded;
            private readonly bool defaultsOnly;
            private FirebaseRemoteConfig remoteConfig;
            private bool finished;

            internal Request(string defaultAdsConfig, IDictionary<string, string> defaultCoreConfigs, Action<string, string> onLoaded, bool defaultsOnly)
            {
                this.defaultAdsConfig = defaultAdsConfig ?? "";
                this.defaultCoreConfigs = defaultCoreConfigs ?? new Dictionary<string, string>();
                this.onLoaded = onLoaded;
                this.defaultsOnly = defaultsOnly;
            }

            internal void Start()
            {
                FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(dependencies =>
                {
                    if (finished)
                        return;
                    if (dependencies.IsFaulted || dependencies.IsCanceled || dependencies.Result != DependencyStatus.Available)
                    {
                        Finish("Firebase unavailable");
                        return;
                    }

                    remoteConfig = FirebaseRemoteConfig.DefaultInstance;
                    // Always fetch fresh values; a failed fetch keeps the values activated by an earlier run.
                    remoteConfig.FetchAsync(TimeSpan.Zero).ContinueWithOnMainThread(fetch =>
                    {
                        if (finished)
                            return;
                        bool fetched = !fetch.IsFaulted && !fetch.IsCanceled && remoteConfig.Info.LastFetchStatus == LastFetchStatus.Success;
                        if (!fetched)
                        {
                            Finish("fetch failed");
                            return;
                        }

                        remoteConfig.ActivateAsync().ContinueWithOnMainThread(_ => Finish("fetched"));
                    });
                });
            }

            internal void Finish(string reason)
            {
                if (finished)
                    return;
                finished = true;

                string ads = Read(AdsConfigKey, defaultAdsConfig);
                string coreKey = HDCAdsConfig.Parse(ads).selectedAdCoreName;
                string core = string.IsNullOrEmpty(coreKey)
                    ? ""
                    : Read(coreKey, defaultCoreConfigs.TryGetValue(coreKey, out string fallback) ? fallback : "");
                if (!defaultsOnly)
                {
                    Save(AdsConfigKey, ads);
                    if (!string.IsNullOrEmpty(coreKey))
                        Save(coreKey, core);
                }

                if (HDCAdsSdk.DebugLog)
                    Debug.Log($"[HDCAds] remote config ready ({reason}): core config '{coreKey}'");
                try
                {
                    onLoaded?.Invoke(ads, core);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }

            private string Read(string key, string fallback)
            {
                if (defaultsOnly)
                    return fallback;

                string value = "";
                try
                {
                    if (remoteConfig != null)
                        value = remoteConfig.GetValue(key).StringValue;
                }
                catch (Exception exception)
                {
                    Debug.LogWarning($"[HDCAds] cannot read remote config '{key}': {exception.Message}");
                }

                if (string.IsNullOrEmpty(value))
                    value = Saved(key);
                return string.IsNullOrEmpty(value) ? fallback : value;
            }

            private static string Saved(string key)
            {
                try
                {
                    return PlayerPrefs.GetString(key, "");
                }
                catch (Exception)
                {
                    return "";
                }
            }

            private static void Save(string key, string value)
            {
                try
                {
                    PlayerPrefs.SetString(key, value ?? "");
                    PlayerPrefs.Save();
                }
                catch (Exception exception)
                {
                    Debug.LogWarning($"[HDCAds] cannot save '{key}': {exception.Message}");
                }
            }
        }
    }
}
