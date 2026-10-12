using UnityEngine;
using UnityEngine.EventSystems;
#if HDC_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace HDC.Onboarding
{
    internal static class HDCOnboardingInput
    {
        internal static void EnsureEventSystem()
        {
            if (EventSystem.current != null)
                return;
#if UNITY_2023_1_OR_NEWER
            bool found = Object.FindFirstObjectByType<EventSystem>() != null;
#else
            bool found = Object.FindObjectOfType<EventSystem>() != null;
#endif
            if (found)
                return;
#if HDC_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
#elif ENABLE_LEGACY_INPUT_MANAGER
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
#else
            Debug.LogWarning("[Splash] The onboarding scenes need an EventSystem with an input module.");
#endif
        }
    }
}
