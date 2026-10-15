using System;
using System.Collections.Generic;
using System.Linq;
using HDC.Ads.Domain;
using UnityEngine;

namespace HDC.Ads.Diagnostics
{
    internal static class HDCConfigReport
    {
        internal const string AdsKey = "ads_config";

        private static readonly List<HDCConfigEntry> entries = new List<HDCConfigEntry>();
        private static readonly SortedDictionary<string, string> remoteValues = new SortedDictionary<string, string>(StringComparer.Ordinal);
        private static readonly Dictionary<string, string> remoteOrigins = new Dictionary<string, string>(StringComparer.Ordinal);
        private static readonly List<(string Name, float Seconds)> steps = new List<(string Name, float Seconds)>();
        private static float startTime;

        internal static bool Started { get; private set; }

        internal static bool Finished { get; private set; }

        internal static HDCConfigPlan Plan { get; private set; }

        internal static bool DefaultsOnly => Started && Plan != HDCConfigPlan.RemoteConfig;

        internal static float TimeoutSeconds { get; private set; }

        internal static DateTime StartClock { get; private set; }

        internal static float LoadSeconds { get; private set; } = -1f;

        internal static float RunningSeconds { get; private set; } = -1f;

        internal static float ElapsedSeconds => Started ? Time.realtimeSinceStartup - startTime : 0f;

        internal static int JoinedLoads { get; private set; }

        internal static IReadOnlyList<(string Name, float Seconds)> Steps => steps;

        internal static string Outcome { get; private set; } = "";

        internal const string FirebaseChecking = "Checking";

        internal static string Firebase { get; private set; } = "";

        internal static bool FirebaseKnown => !string.IsNullOrEmpty(Firebase) && Firebase != FirebaseChecking;

        internal static string FetchStatus { get; private set; } = "";

        internal static DateTime? FetchTime { get; private set; }
        internal static DateTime? ThrottledUntil { get; private set; }

        internal static string Activation { get; private set; } = "";

        internal static string CoreKey { get; private set; } = "";

        internal static IReadOnlyList<HDCConfigEntry> Entries => entries;

        internal static IEnumerable<KeyValuePair<string, string>> RemoteValues => remoteValues;

        internal static int RemoteValueCount => remoteValues.Count;

        internal static string AppliedAds { get; private set; }

        internal static string AppliedCore { get; private set; }

        internal static DateTime AppliedClock { get; private set; }

        internal static string AppliedCoreKey { get; private set; } = "";

        internal static HDCConfigSource? AppliedAdsSource { get; private set; }

        internal static HDCConfigSource? AppliedCoreSource { get; private set; }

        internal static HDCConfigEntry Find(string key) => entries.Find(entry => entry.Key == key);

        internal static string RemoteValue(string key) => remoteValues.TryGetValue(key ?? "", out string value) ? value : null;

        internal static string RemoteOrigin(string key) => remoteOrigins.TryGetValue(key ?? "", out string origin) ? origin : "";

        internal static void LoadStarted(HDCConfigPlan plan, float timeoutSeconds)
        {
            Reset(false);
            Started = true;
            Plan = plan;
            TimeoutSeconds = plan == HDCConfigPlan.RemoteConfig ? timeoutSeconds : 0f;
            StartClock = DateTime.Now;
            startTime = Time.realtimeSinceStartup;
            Firebase = plan == HDCConfigPlan.EditorDefaults ? "Not used in the Editor"
                : plan == HDCConfigPlan.Default ? "Not used: HDC Ads Settings uses Default"
                : plan == HDCConfigPlan.NoFirebase ? "Not in this build"
                : FirebaseChecking;
        }

        internal static HDCCountryResult Country { get; private set; }

        internal static void CountryChecked(HDCCountryResult result) => Country = result;

        internal static void FirebaseChecked(string status) => Firebase = status ?? "";

        internal static void FetchDone(string status, DateTime? fetchTime, DateTime? throttledUntil)
        {
            FetchStatus = status ?? "";
            FetchTime = fetchTime;
            ThrottledUntil = throttledUntil;
        }

        internal static void ActivationDone(string activation) => Activation = activation ?? "";

        internal static void StepDone(string name)
        {
            if (Started && !Finished)
                steps.Add((name ?? "", ElapsedSeconds));
        }

        internal static void Joined() => JoinedLoads++;

        internal static HDCConfigEntry Entry(string key)
        {
            HDCConfigEntry entry = Find(key);
            if (entry == null)
                entries.Add(entry = new HDCConfigEntry(key));
            return entry;
        }

        internal static void AddRemoteValue(string key, string value, string origin)
        {
            if (string.IsNullOrEmpty(key))
                return;
            remoteValues[key] = value ?? "";
            remoteOrigins[key] = origin ?? "";
        }

        internal static void LoadFinished(string outcome, string coreKey, float runningSeconds)
        {
            Outcome = outcome ?? "";
            CoreKey = coreKey ?? "";
            LoadSeconds = ElapsedSeconds;
            RunningSeconds = runningSeconds;
            steps.Add((Outcome, LoadSeconds));
            Finished = true;
        }

        internal static void Applied(string adsConfig, string coreConfig)
        {
            AppliedAds = adsConfig ?? "";
            AppliedCore = coreConfig ?? "";
            AppliedClock = DateTime.Now;
            AppliedCoreKey = HDCAdsConfig.Parse(AppliedAds).selectedAdCoreName ?? "";
            AppliedAdsSource = ChosenSource(AdsKey, AppliedAds);
            AppliedCoreSource = AppliedCoreKey.Length == 0 ? (HDCConfigSource?)null : ChosenSource(AppliedCoreKey, AppliedCore);
        }

        internal static string Compact(string json) => new string((json ?? string.Empty).Where(c => !char.IsWhiteSpace(c)).ToArray());

        private static HDCConfigSource? ChosenSource(string key, string applied)
        {
            HDCConfigEntry entry = Finished ? Find(key) : null;
            return entry != null && Compact(entry.Used) == Compact(applied) ? entry.Source : (HDCConfigSource?)null;
        }

        internal static void Reset(bool applied = true)
        {
            entries.Clear();
            remoteValues.Clear();
            remoteOrigins.Clear();
            steps.Clear();
            Started = Finished = false;
            Plan = HDCConfigPlan.RemoteConfig;
            TimeoutSeconds = 0f;
            LoadSeconds = RunningSeconds = -1f;
            JoinedLoads = 0;
            Outcome = Firebase = FetchStatus = Activation = CoreKey = "";
            FetchTime = ThrottledUntil = null;
            Country = null;
            if (!applied)
                return;
            AppliedAds = AppliedCore = null;
            AppliedCoreKey = "";
            AppliedAdsSource = AppliedCoreSource = null;
        }
    }
}
