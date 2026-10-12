using UnityEngine;

namespace HDC.Onboarding
{
    internal static class HDCOnboardingLog
    {
        private const string Prefix = "[Splash]";

        internal static void Log(string message) => Debug.Log($"{Runtime()} {Prefix} {message}");

        internal static void Warn(string message) => Debug.LogWarning($"{Runtime()} {Prefix} {message}");

        internal static void Error(string message) => Debug.LogError($"{Runtime()} {Prefix} {message}");

        private static string Runtime() => $"[t={Time.realtimeSinceStartup:0.000}s]";
    }
}
