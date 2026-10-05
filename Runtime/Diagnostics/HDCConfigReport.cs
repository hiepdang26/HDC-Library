using System;
using System.Collections.Generic;
using UnityEngine;

namespace HDC.Ads.Diagnostics
{
    internal static class HDCConfigReport
    {
        private static readonly List<HDCConfigEntry> entries = new List<HDCConfigEntry>();
        private static readonly SortedDictionary<string, string> remoteValues = new SortedDictionary<string, string>(StringComparer.Ordinal);
        private static readonly Dictionary<string, string> remoteOrigins = new Dictionary<string, string>(StringComparer.Ordinal);
        private static float startTime;

        internal static bool Started { get; private set; }

        internal static bool Finished { get; private set; }

        internal static bool DefaultsOnly { get; private set; }

        internal static DateTime StartClock { get; private set; }

        internal static float LoadSeconds { get; private set; } = -1f;

        internal static string Outcome { get; private set; } = "";

        internal static string Firebase { get; private set; } = "";

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

        internal static void Reset(bool applied = true)
        {
            entries.Clear();
            remoteValues.Clear();
            remoteOrigins.Clear();
            Started = Finished = DefaultsOnly = false;
            LoadSeconds = -1f;
            Outcome = Firebase = FetchStatus = Activation = CoreKey = "";
            FetchTime = ThrottledUntil = null;
            Country = null;
            if (!applied)
                return;
            AppliedAds = AppliedCore = null;
        }
    }
}
