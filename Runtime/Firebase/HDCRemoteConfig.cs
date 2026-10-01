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
            HDCConfigReport.LoadStarted(defaultsOnly);
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
                    // A Firebase that cannot load its native library throws here: go on with the saved values.
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
                    // Always fetch fresh values; a failed fetch keeps the values activated by an earlier run.
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

                string ads = Read(AdsConfigKey, defaultAdsConfig);
                string coreKey = HDCAdsConfig.Parse(ads).selectedAdCoreName;
                string core = string.IsNullOrEmpty(coreKey)
                    ? ""
                    : Read(coreKey, defaultCoreConfigs.TryGetValue(coreKey, out string fallback) ? fallback : "");
                ReportRemoteValues();
                if (!defaultsOnly)
                {
                    Save(AdsConfigKey, ads);
                    if (!string.IsNullOrEmpty(coreKey))
                        Save(coreKey, core);
                }

                HDCConfigReport.LoadFinished(reason, coreKey);
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

            // Remote Config's value, else the one saved by the last run, else the default. The report keeps all three.
            private string Read(string key, string fallback)
            {
                HDCConfigEntry entry = HDCConfigReport.Entry(key);
                entry.Default = fallback ?? "";
                entry.Saved = Saved(key);
                if (defaultsOnly)
                {
                    entry.RemoteOrigin = "Not fetched in the Editor";
                }
                else if (remoteConfig == null)
                {
                    entry.RemoteOrigin = "Firebase not ready";
                }
                else
                {
                    try
                    {
                        ConfigValue value = remoteConfig.GetValue(key);
                        entry.Remote = value.StringValue ?? "";
                        entry.RemoteOrigin = value.Source.ToString();
                    }
                    catch (Exception exception)
                    {
                        entry.RemoteOrigin = "Read failed: " + exception.Message;
                        Debug.LogWarning($"[HDCAds] cannot read remote config '{key}': {exception.Message}");
                    }
                }

                if (!defaultsOnly && !string.IsNullOrEmpty(entry.Remote))
                    Use(entry, entry.Remote, HDCConfigSource.Remote);
                else if (!defaultsOnly && !string.IsNullOrEmpty(entry.Saved))
                    Use(entry, entry.Saved, HDCConfigSource.Saved);
                else
                    Use(entry, entry.Default, HDCConfigSource.Default);
                return entry.Used;
            }

            private static void Use(HDCConfigEntry entry, string value, HDCConfigSource source)
            {
                entry.Used = value;
                entry.Source = source;
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

            // Every value Remote Config holds, for the debug panel's list of keys.
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

            // Firebase reports the epoch for a time it does not have.
            private static DateTime? FirebaseTime(DateTime value) =>
                value.Year <= 1970 ? (DateTime?)null : value.Kind == DateTimeKind.Utc ? value.ToLocalTime() : value;

            private static string Problem(Exception exception)
            {
                Exception root = exception?.GetBaseException();
                if (root == null)
                    return "canceled";
                return root is DllNotFoundException ? "native library " + root.Message.Split(' ')[0] + " not found" : root.Message;
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
