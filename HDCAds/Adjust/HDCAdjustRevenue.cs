using UnityEngine;
#if HDC_ADJUST
using AdjustSdk;
#endif

namespace HDC.Ads
{
    internal static class HDCAdjustRevenue
    {
        internal static int Count { get; private set; }

        internal static HDCAdjustAdRevenue? Last { get; private set; }

        internal static void Reset()
        {
            Count = 0;
            Last = null;
        }

#if HDC_ADS && HDC_ADJUST
        internal static void Send(HDCAdRevenue revenue)
        {
            HDCAdjustAdRevenue adRevenue = HDCAdjustAdRevenue.From(revenue);
            Count++;
            Last = adRevenue;
            if (!HDCAdjust.IsPhone)
            {
                if (HDCAds.Testing.DebugLog)
                    Debug.Log(HDCAdjust.Tag + "Ad revenue not sent outside Android and iOS: " + adRevenue);
                return;
            }

            var tracked = new AdjustAdRevenue(adRevenue.Source);
            tracked.SetRevenue(adRevenue.Revenue, adRevenue.Currency);
            tracked.AdRevenueNetwork = adRevenue.Network;
            tracked.AdRevenueUnit = adRevenue.Unit;
            tracked.AdRevenuePlacement = adRevenue.Placement;
            Adjust.TrackAdRevenue(tracked);
            if (HDCAds.Testing.DebugLog)
                Debug.Log(HDCAdjust.Tag + "Ad revenue sent: " + adRevenue);
        }
#endif
    }
}
