using System;
using System.Collections.Generic;
using UnityEngine;

namespace HDC.Ads
{
    public static class HDCCustomConfig
    {
        internal const string SavedPrefix = "HDCAds.Custom.";
        internal const string RemoteSource = "Remote";
        internal const string SavedSource = "Saved";
        internal const string DefaultSource = "Default";
        internal const string CountrySource = "Country";

        private static readonly Dictionary<string, (string Value, string Source)> Applied =
            new Dictionary<string, (string, string)>(StringComparer.Ordinal);
        private static readonly HashSet<string> Warned = new HashSet<string>(StringComparer.Ordinal);
        private static Dictionary<string, string> defaults;

        public static event Action Updated;

        public static bool IsReady { get; private set; }

        public static IReadOnlyCollection<string> Keys => DefaultsMap.Keys;

        public static string Get(string key) => TryGet(key, out string value) ? value : "";

        public static bool TryGet(string key, out string value)
        {
            value = "";
            if (string.IsNullOrEmpty(key) || !DefaultsMap.TryGetValue(key, out string fallback))
            {
                if (Warned.Add(key ?? ""))
                    Debug.LogWarning($"[HDCAds] custom config '{key}' is not declared: add it in HDC > Edit configs > Custom keys.");
                return false;
            }

            if (Applied.TryGetValue(key, out (string Value, string Source) found))
            {
                value = found.Value;
                return true;
            }

            string saved = SavedValue(key);
            value = saved.Length > 0 ? saved : fallback;
            return true;
        }

        internal static IReadOnlyDictionary<string, string> Defaults => DefaultsMap;

        private static Dictionary<string, string> DefaultsMap => defaults ?? (defaults = HDCAdsSettings.Load().CustomDefaults());

        internal static string SourceOf(string key) =>
            key != null && Applied.TryGetValue(key, out (string Value, string Source) found) ? found.Source : "";

        internal static string SavedValue(string key)
        {
            try
            {
                return PlayerPrefs.GetString(SavedPrefix + key, "");
            }
            catch (Exception)
            {
                return "";
            }
        }

        internal static void Declare(IDictionary<string, string> values)
        {
            defaults = new Dictionary<string, string>(values ?? new Dictionary<string, string>(), StringComparer.Ordinal);
            Applied.Clear();
            IsReady = false;
        }

        internal static void Apply(IDictionary<string, (string Value, string Source)> values, bool save)
        {
            Applied.Clear();
            foreach (KeyValuePair<string, (string Value, string Source)> pair in values)
            {
                Applied[pair.Key] = (pair.Value.Value ?? "", pair.Value.Source ?? "");
                if (save)
                    Save(pair.Key, pair.Value.Value);
            }

            if (save)
                PlayerPrefs.Save();
            IsReady = true;
            Action handlers = Updated;
            if (handlers == null)
                return;
            foreach (Action handler in handlers.GetInvocationList())
            {
                try
                {
                    handler();
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }
        }

        internal static void UseDefaults()
        {
            var values = new Dictionary<string, (string Value, string Source)>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, string> pair in Defaults)
            {
                string saved = SavedValue(pair.Key);
                values[pair.Key] = saved.Length > 0 ? (saved, SavedSource) : (pair.Value, DefaultSource);
            }

            Apply(values, false);
        }

        internal static void UseCountry()
        {
            var values = new Dictionary<string, (string Value, string Source)>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, string> pair in HDCAdsSettings.Load().CustomCountryValues())
            {
                if (Defaults.ContainsKey(pair.Key))
                    values[pair.Key] = (pair.Value, CountrySource);
            }

            Apply(values, false);
        }

        internal static void Reset()
        {
            Applied.Clear();
            Warned.Clear();
            defaults = null;
            IsReady = false;
            Updated = null;
        }

#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForPlayMode() => Reset();
#endif

        private static void Save(string key, string value)
        {
            try
            {
                PlayerPrefs.SetString(SavedPrefix + key, value ?? "");
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[HDCAds] cannot save custom config '{key}': {exception.Message}");
            }
        }
    }
}
