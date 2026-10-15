using HDC.Ads.Logic;
using UnityEngine;

namespace HDC.Onboarding
{
    internal static class HDCConsentTracking
    {
        internal const string Prefix = "4_";

        private static HDCConsentGate tracked;

        internal static void Begin()
        {
            HDCConsentGate gate = HDCOnboardingAds.Context.Consent;
            if (gate == tracked)
                return;
            if (tracked != null)
                tracked.Stepped -= Track;
            tracked = gate;
            foreach (string step in gate.Steps)
                Track(step);
            gate.Stepped += Track;
        }

        private static void Track(string step) => HDCOnboardingTracking.Track(Prefix + step);

#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForPlayMode()
        {
            if (tracked != null)
                tracked.Stepped -= Track;
            tracked = null;
        }
#endif
    }
}
