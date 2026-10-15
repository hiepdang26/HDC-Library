using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase;
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

        public const float TimeoutSeconds = HDCAdsSettings.RemoteConfigTimeoutSeconds;

        private static Request current;

        public static bool FetchInEditor { get; set; }

        public static void Fetch(
            string defaultAdsConfig,
            IDictionary<string, string> defaultCoreConfigs,
            Action<string, string> onLoaded,
            float timeoutSeconds = TimeoutSeconds)
        {
            HDCMainThread.EnsureCreated();
            if (current != null)
            {
                current.Join(onLoaded);
                return;
            }

            current = new Request(defaultAdsConfig, defaultCoreConfigs, onLoaded, ModeFor(HDCAdsSettings.Load()), timeoutSeconds);
            current.Start();
        }

        public static void FetchAndInitialize(
            string defaultAdsConfig,
            IDictionary<string, string> defaultCoreConfigs,
            Action onInitialized = null,
            float timeoutSeconds = TimeoutSeconds)
        {
            Fetch(defaultAdsConfig, defaultCoreConfigs, (ads, core) => HDCAds.Initialize(ads, core, onInitialized), timeoutSeconds);
        }

        private static HDCConfigMode ModeFor(HDCAdsSettings settings)
        {
            if (Application.isEditor && !FetchInEditor)
                return HDCConfigMode.EditorDefaults;
            return settings.ConfigSource == HDCAdsConfigSource.Default ? HDCConfigMode.Defaults : HDCConfigMode.RemoteConfig;
        }

        private static HDCConfigPlan PlanFor(HDCConfigMode mode) =>
            mode == HDCConfigMode.RemoteConfig ? HDCConfigPlan.RemoteConfig
            : mode == HDCConfigMode.Defaults ? HDCConfigPlan.Default
            : HDCConfigPlan.EditorDefaults;

#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForPlayMode()
        {
            current?.Abandon();
            current = null;
        }
#endif

        private sealed class Request
        {
            private readonly string defaultAdsConfig;
            private readonly IDictionary<string, string> defaultCoreConfigs;
            private readonly List<Action<string, string>> listeners = new List<Action<string, string>>();
            private readonly HDCConfigMode mode;
            private readonly float timeoutSeconds;
            private readonly HDCLoadClock clock;
            private FirebaseRemoteConfig remoteConfig;
            private Task<DependencyStatus> checking;
            private Task fetching;
            private Task<bool> activating;
            private bool finished;
            private string ads = "";
            private string core = "";

            internal Request(
                string defaultAdsConfig,
                IDictionary<string, string> defaultCoreConfigs,
                Action<string, string> onLoaded,
                HDCConfigMode mode,
                float timeoutSeconds)
            {
                this.defaultAdsConfig = defaultAdsConfig ?? "";
                this.defaultCoreConfigs = defaultCoreConfigs ?? new Dictionary<string, string>();
                this.mode = mode;
                this.timeoutSeconds = Mathf.Max(0f, timeoutSeconds);
                clock = new HDCLoadClock(this.timeoutSeconds);
                if (onLoaded != null)
                    listeners.Add(onLoaded);
            }

            internal void Start()
            {
                HDCConfigReport.LoadStarted(PlanFor(mode), timeoutSeconds);
                if (mode != HDCConfigMode.RemoteConfig)
                {
                    string reason = mode == HDCConfigMode.Defaults ? "chosen in HDC Ads Settings" : "defaults in the Editor";
                    HDCMainThread.Post(() => Finish(reason, false));
                    return;
                }

                try
                {
                    checking = FirebaseApp.CheckAndFixDependenciesAsync();
                }
                catch (Exception exception)
                {
                    Debug.LogWarning("[HDCAds] Firebase cannot start: " + Problem(exception));
                    HDCConfigReport.FirebaseChecked("Cannot start: " + Problem(exception));
                    HDCMainThread.Post(() => Finish("Firebase unavailable", false));
                    return;
                }

                HDCMainThread.Ticked += Tick;
            }

            internal void Join(Action<string, string> onLoaded)
            {
                if (onLoaded == null)
                    return;
                HDCConfigReport.Joined();
                if (!finished)
                {
                    listeners.Add(onLoaded);
                    return;
                }

                HDCMainThread.Post(() => Deliver(onLoaded));
            }

            internal void Abandon()
            {
                finished = true;
                listeners.Clear();
                HDCMainThread.Ticked -= Tick;
            }

            private void Tick()
            {
                if (finished)
                {
                    HDCMainThread.Ticked -= Tick;
                    return;
                }

                if (checking != null && checking.IsCompleted)
                    Checked(Take(ref checking));
                if (!finished && fetching != null && fetching.IsCompleted)
                    Fetched(Take(ref fetching));
                if (!finished && activating != null && activating.IsCompleted)
                    Activated(Take(ref activating));
                if (finished)
                    return;

                if (clock.Tick(Time.unscaledDeltaTime, activating != null))
                    Finish("timeout", false);
            }

            private static T Take<T>(ref T task) where T : class
            {
                T taken = task;
                task = null;
                return taken;
            }

            private void Checked(Task<DependencyStatus> task)
            {
                HDCConfigReport.StepDone("Firebase");
                if (task.IsFaulted || task.IsCanceled || task.Result != DependencyStatus.Available)
                {
                    HDCConfigReport.FirebaseChecked(task.IsFaulted || task.IsCanceled
                        ? "Check failed: " + Problem(task.Exception)
                        : task.Result.ToString());
                    Finish("Firebase unavailable", false);
                    return;
                }

                HDCConfigReport.FirebaseChecked("Available");
                try
                {
                    remoteConfig = FirebaseRemoteConfig.DefaultInstance;
                    fetching = remoteConfig.FetchAsync(TimeSpan.Zero);
                }
                catch (Exception exception)
                {
                    HDCConfigReport.FetchDone("Cannot fetch: " + Problem(exception), null, null);
                    Finish("fetch failed", false);
                }
            }

            private void Fetched(Task task)
            {
                HDCConfigReport.StepDone("Fetch");
                ReportFetch(task.IsFaulted || task.IsCanceled ? Problem(task.Exception) : null);
                if (task.IsFaulted || task.IsCanceled || !LastFetchSucceeded())
                {
                    Finish("fetch failed", false);
                    return;
                }

                try
                {
                    activating = remoteConfig.ActivateAsync();
                }
                catch (Exception exception)
                {
                    HDCConfigReport.ActivationDone("Failed: " + Problem(exception));
                    Finish("activation failed", false);
                }
            }

            private void Activated(Task<bool> task)
            {
                HDCConfigReport.StepDone("Activation");
                bool failed = task.IsFaulted || task.IsCanceled;
                HDCConfigReport.ActivationDone(failed
                    ? "Failed: " + Problem(task.Exception)
                    : task.Result ? "New values activated" : "Values were already active");
                Finish(failed ? "activation failed" : "fetched", !failed);
            }

            private bool LastFetchSucceeded()
            {
                try
                {
                    return remoteConfig.Info.LastFetchStatus == LastFetchStatus.Success;
                }
                catch (Exception)
                {
                    return false;
                }
            }

            private void Finish(string reason, bool fresh)
            {
                if (finished)
                    return;
                finished = true;
                HDCMainThread.Ticked -= Tick;

                HDCAdsSettings settings = HDCAdsSettings.Load();
                HDCConfigChoice choice = HDCAdsRuntime.ConfigSelection().Select(new HDCConfigInputs
                {
                    Mode = mode,
                    Remote = fresh ? new FirebaseValues(remoteConfig) : null,
                    RemoteProblem = reason,
                    DefaultAds = defaultAdsConfig,
                    DefaultCores = defaultCoreConfigs,
                    CustomDefaults = HDCCustomConfig.Defaults,
                    CountryRules = settings.CountryRulesJson(),
                    CountryAds = settings.CountryAdsConfig,
                    CountryCore = settings.CountryCoreConfig,
                    CustomCountry = settings.CustomCountryValues(),
                });
                if (fresh)
                    ReportRemoteValues();
                HDCCustomConfig.Apply(choice.CustomValues());
                ads = choice.Ads;
                core = choice.Core;

                HDCConfigReport.LoadFinished(reason, choice.CoreKey, clock.Running);
                if (mode == HDCConfigMode.RemoteConfig && !fresh)
                    Debug.LogWarning($"[HDCAds] Remote Config not used ({reason} after {HDCConfigReport.LoadSeconds:0.0} s): the configs come from HDC Ads Settings.");
                else if (HDCAdsSdk.DebugLog)
                    Debug.Log($"[HDCAds] configs ready ({reason} after {HDCConfigReport.LoadSeconds:0.0} s): core config '{choice.CoreKey}'");

                Action<string, string>[] waiting = listeners.ToArray();
                listeners.Clear();
                foreach (Action<string, string> listener in waiting)
                    Deliver(listener);
            }

            private void Deliver(Action<string, string> listener)
            {
                try
                {
                    listener(ads, core);
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
        }
    }
}
