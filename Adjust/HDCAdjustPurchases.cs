using System.Globalization;
using UnityEngine;
#if HDC_ADJUST
using AdjustSdk;
#endif

namespace HDC.Ads
{
    internal static class HDCAdjustPurchases
    {
        internal static bool Track(bool inScene, string token, double amount, string currency, string transactionId)
        {
#if HDC_ADJUST
            if (string.IsNullOrEmpty(token))
            {
                Debug.LogWarning(HDCAdjust.Tag + (!inScene ? "HDCAdjust is not in the scene" : "HDCAdjust has no " + HDCAdjust.PlatformName + " purchase event token")
                    + ", so the purchase is not sent to Adjust.");
                return false;
            }

            if (!HDCAdjust.IsPhone)
            {
                Debug.Log(HDCAdjust.Tag + "Purchase not sent outside Android and iOS: " + amount.ToString(CultureInfo.InvariantCulture) + " " + currency);
                return false;
            }

            var purchase = new AdjustEvent(token);
            purchase.SetRevenue(amount, currency);
            if (!string.IsNullOrEmpty(transactionId))
                purchase.TransactionId = transactionId;
            Adjust.TrackEvent(purchase);
            return true;
#else
            Debug.LogWarning(HDCAdjust.Tag + "The Adjust SDK is not in the project, so the purchase is not sent.");
            return false;
#endif
        }
    }
}
