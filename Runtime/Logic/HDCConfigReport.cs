using System;
using System.Collections.Generic;
using UnityEngine;

namespace HDC.Ads
{
    /// <summary>Where a config value the ads used came from.</summary>
    internal enum HDCConfigSource
    {
        None,
        Remote,
        Saved,
        Default,
    }

    /// <summary>One Remote Config key the ads read: the value each source had, and the one used.</summary>
    internal sealed class HDCConfigEntry
    {
        internal HDCConfigEntry(string key) => Key = key;

        internal string Key { get; }

        /// <summary>Remote Config's active value; empty when it has none.</summary>
        internal string Remote { get; set; } = "";

        /// <summary>Where Remote Config's value came from, as Firebase reports it, or why there is none.</summary>
        internal string RemoteOrigin { get; set; } = "";

        /// <summary>The value the last run saved on the device, read before this run saved its own.</summary>
        internal string Saved { get; set; } = "";

        /// <summary>The project's default, from HDC > Edit configs.</summary>
        internal string Default { get; set; } = "";

        internal string Used { get; set; } = "";
        internal HDCConfigSource Source { get; set; }
    }

    /// <summary>
    /// How the ads configs were loaded, for the debug panel: Firebase, the fetch, what each source had for every
    /// key the ads read, every value Remote Config holds, and the configs HDCAds started with. HDCRemoteConfig
    /// reports the load and HDCAds.Initialize the configs it applied.
    /// </summary>
    internal static class HDCConfigReport
    {
        private static readonly List<HDCConfigEntry> entries = new List<HDCConfigEntry>();
        private static readonly SortedDictionary<string, string> remoteValues = new SortedDictionary<string, string>(StringComparer.Ordinal);
        private static readonly Dictionary<string, string> remoteOrigins = new Dictionary<string, string>(StringComparer.Ordinal);
        private static float startTime;

        /// <summary>True once HDCRemoteConfig started a load.</summary>
        internal static bool Started { get; private set; }

        internal static bool Finished { get; private set; }

        /// <summary>The Editor used the defaults without Firebase.</summary>
        internal static bool DefaultsOnly { get; private set; }

        internal static DateTime StartClock { get; private set; }

        /// <summary>Seconds the load took; below zero until it finished.</summary>
        internal static float LoadSeconds { get; private set; } = -1f;

        /// <summary>How the load ended: fetched, fetch failed, Firebase unavailable, timeout or defaults in the Editor.</summary>
        internal static string Outcome { get; private set; } = "";

        /// <summary>Firebase's dependency check: Available, or why Firebase cannot run.</summary>
        internal static string Firebase { get; private set; } = "";

        /// <summary>The last fetch's status, with the failure reason.</summary>
        internal static string FetchStatus { get; private set; } = "";

        internal static DateTime? FetchTime { get; private set; }
        internal static DateTime? ThrottledUntil { get; private set; }

        /// <summary>Whether the fetched values replaced the active ones.</summary>
        internal static string Activation { get; private set; } = "";

        /// <summary>The ad core config key the ads config picked.</summary>
        internal static string CoreKey { get; private set; } = "";

        internal static IReadOnlyList<HDCConfigEntry> Entries => entries;

        /// <summary>Every key Remote Config holds after the load, with its value.</summary>
        internal static IEnumerable<KeyValuePair<string, string>> RemoteValues => remoteValues;

        internal static int RemoteValueCount => remoteValues.Count;

        /// <summary>The ads config HDCAds started with; null before HDCAds.Initialize.</summary>
        internal static string AppliedAds { get; private set; }

        /// <summary>The ad core config HDCAds started with; null before HDCAds.Initialize.</summary>
        internal static string AppliedCore { get; private set; }

        internal static DateTime AppliedClock { get; private set; }

        internal static HDCConfigEntry Find(string key) => entries.Find(entry => entry.Key == key);

        internal static string RemoteValue(string key) => remoteValues.TryGetValue(key ?? "", out string value) ? value : null;

        internal static string RemoteOrigin(string key) => remoteOrigins.TryGetValue(key ?? "", out string origin) ? origin : "";

        internal static void LoadStarted(bool defaultsOnly)
        {
            Reset(false);
            Started = true;
            DefaultsOnly = defaultsOnly;
            StartClock = DateTime.Now;
            startTime = Time.realtimeSinceStartup;
            Firebase = defaultsOnly ? "Not used in the Editor" : "Checking";
        }

        internal static void FirebaseChecked(string status) => Firebase = status ?? "";

        internal static void FetchDone(string status, DateTime? fetchTime, DateTime? throttledUntil)
        {
            FetchStatus = status ?? "";
            FetchTime = fetchTime;
            ThrottledUntil = throttledUntil;
        }

        internal static void ActivationDone(string activation) => Activation = activation ?? "";

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

        internal static void LoadFinished(string outcome, string coreKey)
        {
            Finished = true;
            Outcome = outcome ?? "";
            CoreKey = coreKey ?? "";
            LoadSeconds = Time.realtimeSinceStartup - startTime;
        }

        internal static void Applied(string adsConfig, string coreConfig)
        {
            AppliedAds = adsConfig ?? "";
            AppliedCore = coreConfig ?? "";
            AppliedClock = DateTime.Now;
        }

        /// <summary>Forgets the load, and with <paramref name="applied"/> the configs HDCAds started with.</summary>
        internal static void Reset(bool applied = true)
        {
            entries.Clear();
            remoteValues.Clear();
            remoteOrigins.Clear();
            Started = Finished = DefaultsOnly = false;
            LoadSeconds = -1f;
            Outcome = Firebase = FetchStatus = Activation = CoreKey = "";
            FetchTime = ThrottledUntil = null;
            if (!applied)
                return;
            AppliedAds = AppliedCore = null;
        }
    }
}
