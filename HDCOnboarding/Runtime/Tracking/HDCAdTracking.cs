using System;
using HDC.Ads;
using UnityEngine;

namespace HDC.Onboarding
{
    [DisallowMultipleComponent]
    [AddComponentMenu("HDC/Onboarding/HDC Ad Tracking")]
    public sealed class HDCAdTracking : MonoBehaviour
    {
        public const string EventName = "hdc_ad_paid";

        [Tooltip("Ad groups or positions to track. Every paid impression of one of them is counted and sent to Firebase Analytics as hdc_ad_paid. The splash interstitial and the native after it use the position splash.")]
        [SerializeField] private string[] trackedAds = { "splash", "native_splash", "native_intro1", "native_intro2", "native_intro_full" };

        private static HDCAdTracking current;
        private static bool sessionCounted;

        public static event Action<HDCAdPaid> Tracked;

        public static int SessionCount => HDCAdTrackingCounts.Session;

        private void Awake()
        {
            if (current != null && current != this)
            {
                Destroy(this);
                return;
            }

            current = this;
            if (transform.parent != null)
                transform.SetParent(null, false);
            DontDestroyOnLoad(gameObject);
            if (!sessionCounted)
            {
                sessionCounted = true;
                HDCAdTrackingCounts.AddSession();
            }

            HDCAds.Revenue += OnRevenue;
        }

        private void OnDestroy()
        {
            if (current != this)
                return;
            HDCAds.Revenue -= OnRevenue;
            current = null;
        }

        private void OnRevenue(HDCAdRevenue revenue)
        {
            string name = TrackedName(revenue);
            if (name == null)
                return;

            HDCAdPaid paid = HDCAdTrackingCounts.Count(name, revenue);
            HDCFirebaseAnalytics.LogEvent(EventName, paid.Parameters());
            Action<HDCAdPaid> handlers = Tracked;
            if (handlers == null)
                return;
            foreach (Action<HDCAdPaid> handler in handlers.GetInvocationList())
            {
                try
                {
                    handler(paid);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }
        }

        private string TrackedName(HDCAdRevenue revenue)
        {
            if (trackedAds == null)
                return null;
            foreach (string name in trackedAds)
            {
                if (!string.IsNullOrEmpty(name) && name == revenue.Group)
                    return name;
            }

            foreach (string name in trackedAds)
            {
                if (!string.IsNullOrEmpty(name) && name == revenue.Position)
                    return name;
            }

            return null;
        }

#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForPlayMode()
        {
            current = null;
            sessionCounted = false;
            Tracked = null;
        }
#endif
    }
}
