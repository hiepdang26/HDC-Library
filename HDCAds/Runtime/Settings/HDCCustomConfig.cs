using System;
using System.Collections.Generic;
using UnityEngine;

namespace HDC.Ads
{
    public static class HDCCustomConfig
    {
        internal const string RemoteSource = "Remote";
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

            value = Applied.TryGetValue(key, out (string Value, string Source) found) ? found.Value : fallback;
            return true;
        }

        internal static IReadOnlyDictionary<string, string> Defaults => DefaultsMap;

        private static Dictionary<string, string> DefaultsMap => defaults ?? (defaults = HDCAdsSettings.Load().CustomDefaults());

        internal static string SourceOf(string key) =>
            key != null && Applied.TryGetValue(key, out (string Value, string Source) found) ? found.Source : "";

        internal static void Declare(IDictionary<string, string> values)
        {
            defaults = new Dictionary<string, string>(values ?? new Dictionary<string, string>(), StringComparer.Ordinal);
            Applied.Clear();
            IsReady = false;
        }

        internal static void Apply(IDictionary<string, (string Value, string Source)> values)
        {
            Applied.Clear();
            foreach (KeyValuePair<string, (string Value, string Source)> pair in values)
                Applied[pair.Key] = (pair.Value.Value ?? "", pair.Value.Source ?? "");

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
    }
}
