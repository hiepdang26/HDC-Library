using System;
using UnityEngine;

namespace HDC.Onboarding
{
    public static class HDCOnboardingTracking
    {
        private static string firstOpenPrefix;

        public static event Action<string> Tracked;

        internal static void Track(string value)
        {
            if (firstOpenPrefix == null)
                firstOpenPrefix = HDCFirstSession.IsFirstOpen ? "f" : "n";
            HDCFirebaseAnalytics.LogEvent($"{firstOpenPrefix}_{value}");
            HDCFirebaseAnalytics.LogEvent($"a_{value}");
            Action<string> handlers = Tracked;
            if (handlers == null)
                return;
            foreach (Action<string> handler in handlers.GetInvocationList())
            {
                try
                {
                    handler(value);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }
        }

#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForPlayMode()
        {
            firstOpenPrefix = null;
            Tracked = null;
        }
#endif
    }
}
