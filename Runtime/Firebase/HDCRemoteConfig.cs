using System;
using System.Collections.Generic;
using Firebase;
using Firebase.Extensions;
using Firebase.RemoteConfig;
using HDC.Ads.Composition;
using HDC.Ads.Diagnostics;
using HDC.Ads.Infrastructure;
using HDC.Ads.Logic;
using HDC.Ads.Ports;
using UnityEngine;

namespace HDC.Ads
{
    public static class HDCRemoteConfig
    {
        public const string AdsConfigKey = HDCConfigSelection.AdsConfigKey;

        public static bool FetchInEditor { get; set; }

        public static void Fetch(
            string defaultAdsConfig,
            IDictionary<string, string> defaultCoreConfigs,
            Action<string, string> onLoaded,
            float timeoutSeconds = 10f)
        {
            HDCMainThread.EnsureCreated();
            bool defaultsOnly = Application.isEditor && !FetchInEditor;
            var fetch = new Request(defaultAdsConfig, defaultCoreConfigs, onLoaded, defaultsOnly);
            HDCConfigReport.LoadStarted(defaultsOnly);
            if (defaultsOnly)
            {
                HDCMainThread.Post(() => fetch.Finish("defaults in the Editor"));
                return;
            }

            HDCMainThread.PostDelayed(timeoutSeconds, () => fetch.Finish("timeout"));
            fetch.Start();
        }

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
                try
                {
                    FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(dependencies =>
                    {
                        if (finished)
                            return;
                        if (dependencies.IsFaulted || dependencies.IsCanceled || dependencies.Result != DependencyStatus.Available)
                        {
                            HDCConfigReport.FirebaseChecked(dependencies.IsFaulted || dependencies.IsCanceled
                                ? "Check failed: " + Problem(dependencies.Exception)
                                : dependencies.Result.ToString());
                            Finish("Firebase unavailable");
                            return;
                        }

                        HDCConfigReport.FirebaseChecked("Available");
                        StartFetch();
                    });
                }
                catch (Exception exception)
                {
                    Debug.LogWarning("[HDCAds] Firebase cannot start: " + Problem(exception));
                    HDCConfigReport.FirebaseChecked("Cannot start: " + Problem(exception));
                    Finish("Firebase unavailable");
                }
            }

            private void StartFetch()
            {
                try
                {
                    remoteConfig = FirebaseRemoteConfig.DefaultInstance;
                    remoteConfig.FetchAsync(TimeSpan.Zero).ContinueWithOnMainThread(fetch =>
                    {
                        if (finished)
                            return;
                        ReportFetch(fetch.IsFaulted || fetch.IsCanceled ? Problem(fetch.Exception) : null);
                        bool fetched = !fetch.IsFaulted && !fetch.IsCanceled && remoteConfig.Info.LastFetchStatus == LastFetchStatus.Success;
                        if (!fetched)
                        {
                            Finish("fetch failed");
                            return;
                        }

                        remoteConfig.ActivateAsync().ContinueWithOnMainThread(activation =>
                        {
                            HDCConfigReport.ActivationDone(activation.IsFaulted || activation.IsCanceled
                                ? "Failed: " + Problem(activation.Exception)
                                : activation.Result ? "New values activated" : "Values were already active");
                            Finish("fetched");
                        });
                    });
                }
                catch (Exception exception)
                {
                    Debug.LogWarning("[HDCAds] cannot fetch the remote config: " + Problem(exception));
                    HDCConfigReport.FetchDone("Cannot fetch: " + Problem(exception), null, null);
                    Finish("fetch failed");
                }
            }

            internal void Finish(string reason)
            {
                if (finished)
                    return;
                finished = true;

                HDCAdsSettings settings = HDCAdsSettings.Load();
                HDCConfigChoice choice = HDCAdsRuntime.ConfigSelection().Select(new HDCConfigInputs
                {
                    Mode = defaultsOnly ? HDCConfigMode.EditorDefaults : HDCConfigMode.RemoteConfig,
                    Remote = defaultsOnly || remoteConfig == null ? null : new FirebaseValues(remoteConfig),
                    DefaultAds = defaultAdsConfig,
                    DefaultCores = defaultCoreConfigs,
                    Saved = Saved,
                    CustomDefaults = HDCCustomConfig.Defaults,
                    SavedCustom = HDCCustomConfig.SavedValue,
                    CountryRules = settings.CountryRulesJson(),
                    CountryAds = settings.CountryAdsConfig,
                    CountryCore = settings.CountryCoreConfig,
                    CustomCountry = settings.CustomCountryValues(),
                });
                ReportRemoteValues();
                foreach (KeyValuePair<string, string> save in choice.Saves)
                    Save(save.Key, save.Value);
                HDCCustomConfig.Apply(choice.CustomValues(), choice.SaveCustom);

                HDCConfigReport.LoadFinished(reason, choice.CoreKey);
                if (HDCAdsSdk.DebugLog)
                    Debug.Log($"[HDCAds] remote config ready ({reason}): core config '{choice.CoreKey}'");
                try
                {
                    onLoaded?.Invoke(choice.Ads, choice.Core);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }

            private void ReportFetch(string problem)
            {
                try
                {
                    ConfigInfo info = remoteConfig.Info;
                    string status = info.LastFetchStatus.ToString();
                    if (info.LastFetchStatus == LastFetchStatus.Failure)
                        status += ": " + info.LastFetchFailureReason;
                    if (!string.IsNullOrEmpty(problem))
                        status += " (" + problem + ")";
                    HDCConfigReport.FetchDone(status, FirebaseTime(info.FetchTime), FirebaseTime(info.ThrottledEndTime));
                }
                catch (Exception exception)
                {
                    HDCConfigReport.FetchDone("Unknown: " + exception.Message, null, null);
                }
            }

            private void ReportRemoteValues()
            {
                if (defaultsOnly || remoteConfig == null)
                    return;
                try
                {
                    foreach (KeyValuePair<string, ConfigValue> pair in remoteConfig.AllValues)
                        HDCConfigReport.AddRemoteValue(pair.Key, pair.Value.StringValue, pair.Value.Source.ToString());
                }
                catch (Exception exception)
                {
                    Debug.LogWarning("[HDCAds] cannot list the remote config values: " + exception.Message);
                }
            }

            private static DateTime? FirebaseTime(DateTime value) =>
                value.Year <= 1970 ? (DateTime?)null : value.Kind == DateTimeKind.Utc ? value.ToLocalTime() : value;

            private static string Problem(Exception exception)
            {
                Exception root = exception?.GetBaseException();
                if (root == null)
                    return "canceled";
                return root is DllNotFoundException ? "native library " + root.Message.Split(' ')[0] + " not found" : root.Message;
            }

            private sealed class FirebaseValues : IRemoteConfigValues
            {
                private readonly FirebaseRemoteConfig remoteConfig;

                internal FirebaseValues(FirebaseRemoteConfig remoteConfig)
                {
                    this.remoteConfig = remoteConfig;
                }

                public string Read(string key, out string origin)
                {
                    try
                    {
                        ConfigValue value = remoteConfig.GetValue(key);
                        string text = value.StringValue ?? "";
                        origin = value.Source.ToString();
                        return text;
                    }
                    catch (Exception exception)
                    {
                        origin = "Read failed: " + exception.Message;
                        Debug.LogWarning($"[HDCAds] cannot read remote config '{key}': {exception.Message}");
                        return "";
                    }
                }
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
