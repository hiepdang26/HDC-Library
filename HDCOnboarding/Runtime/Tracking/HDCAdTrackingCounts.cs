using System.Globalization;
using HDC.Ads;
using UnityEngine;

namespace HDC.Onboarding
{
    internal static class HDCAdTrackingCounts
    {
        internal const string SessionKey = "HDCAdTracking.Session";
        internal const string TotalKey = "HDCAdTracking.Total";
        internal const string CountPrefix = "HDCAdTracking.Count.";
        internal const string ValuePrefix = "HDCAdTracking.Value.";
        internal const string LegacySessionKey = "PlayerSessionPlay";
        internal const string LegacyTotalKey = "TotalAdPaid";

        internal static int Session => Read(SessionKey, LegacySessionKey);

        internal static void AddSession()
        {
            PlayerPrefs.SetInt(SessionKey, Session + 1);
            PlayerPrefs.Save();
        }

        internal static HDCAdPaid Count(string name, HDCAdRevenue revenue)
        {
            int namePaidCount = Read(CountPrefix + name, name) + 1;
            int totalPaidCount = Read(TotalKey, LegacyTotalKey) + 1;
            string valueKey = ValuePrefix + revenue.Currency;
            double totalValue = ReadValue(valueKey) + revenue.Value;
            PlayerPrefs.SetInt(CountPrefix + name, namePaidCount);
            PlayerPrefs.SetInt(TotalKey, totalPaidCount);
            PlayerPrefs.SetString(valueKey, totalValue.ToString("R", CultureInfo.InvariantCulture));
            PlayerPrefs.Save();
            return new HDCAdPaid(revenue, name, Session, namePaidCount, totalPaidCount, totalValue);
        }

        private static int Read(string key, string legacyKey) =>
            PlayerPrefs.HasKey(key) ? PlayerPrefs.GetInt(key, 0) : PlayerPrefs.GetInt(legacyKey, 0);

        private static double ReadValue(string key)
        {
            string text = PlayerPrefs.GetString(key, "");
            return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double value) ? value : 0d;
        }
    }
}
