using UnityEngine;

namespace HDC.Onboarding
{
    public static class HDCFirstSession
    {
        public static bool IsFirstOpen { get; private set; }

        internal static void CheckAndCacheFirstOpenData() => IsFirstOpen = PlayerPrefs.GetInt(HDCOnboardingKeys.FirstOpen, 0) == 0;

        internal static void EndFirstOpenSession()
        {
            PlayerPrefs.SetInt(HDCOnboardingKeys.FirstOpen, 1);
            PlayerPrefs.Save();
        }

#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForPlayMode() => IsFirstOpen = false;
#endif
    }
}
