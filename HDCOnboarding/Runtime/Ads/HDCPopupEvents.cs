using System;
using System.Collections.Generic;
using HDC.Ads;
using HDC.Ads.Domain;
using HDC.Ads.Infrastructure;
using UnityEngine;

namespace HDC.Onboarding
{
    internal static class HDCPopupEvents
    {
        private static readonly Dictionary<string, Counts> ByInstance = new Dictionary<string, Counts>(StringComparer.Ordinal);
        private static readonly Dictionary<string, int> HidesByPosition = new Dictionary<string, int>(StringComparer.Ordinal);
        private static bool listening;

        internal static Counts Of(string instanceId)
        {
            Listen();
            if (!ByInstance.TryGetValue(instanceId ?? "", out Counts counts))
                ByInstance[instanceId ?? ""] = counts = new Counts();
            return counts;
        }

        internal static int Hides(string position) => HidesByPosition.TryGetValue(position ?? "", out int hides) ? hides : 0;

        internal static void Hidden(string position) => HidesByPosition[position ?? ""] = Hides(position) + 1;

        private static void Listen()
        {
            if (listening)
                return;
            listening = true;
            HDCAdsSdk.AdEvent -= OnAdEvent;
            HDCAdsSdk.AdEvent += OnAdEvent;
        }

        private static void OnAdEvent(HDCAdEvent adEvent)
        {
            if (adEvent == null || adEvent.format != HDCAdFormat.Popup || !ByInstance.TryGetValue(adEvent.id ?? "", out Counts counts))
                return;
            switch (adEvent.type)
            {
                case HDCAdEventType.Shown:
                    counts.Shown++;
                    break;
                case HDCAdEventType.Closed:
                    counts.Closed++;
                    break;
                case HDCAdEventType.ShowFailed:
                    counts.Failed++;
                    break;
            }
        }

#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForPlayMode()
        {
            listening = false;
            ByInstance.Clear();
            HidesByPosition.Clear();
        }
#endif

        internal sealed class Counts
        {
            internal int Shown;
            internal int Closed;
            internal int Failed;
        }
    }
}
