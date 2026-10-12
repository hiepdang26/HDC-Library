using System.Collections.Generic;
using HDC.Ads;

namespace HDC.Onboarding
{
    public sealed class HDCAdPaid
    {
        private const int MaxTextLength = 100;

        internal HDCAdPaid(HDCAdRevenue revenue, string name, int sessionCount, int namePaidCount, int totalPaidCount, double totalValue)
        {
            Revenue = revenue;
            Name = name ?? "";
            SessionCount = sessionCount;
            NamePaidCount = namePaidCount;
            TotalPaidCount = totalPaidCount;
            TotalValue = totalValue;
        }

        public HDCAdRevenue Revenue { get; }

        public string Name { get; }

        public int SessionCount { get; }

        public int NamePaidCount { get; }

        public int TotalPaidCount { get; }

        public double TotalValue { get; }

        internal static string ChannelName(HDCAdChannel channel)
        {
            switch (channel)
            {
                case HDCAdChannel.ForceAd:
                    return "force_ad";
                case HDCAdChannel.Rewarded:
                    return "rewarded";
                case HDCAdChannel.AppLaunch:
                    return "app_launch";
                case HDCAdChannel.AppResume:
                    return "app_resume";
                case HDCAdChannel.Banner:
                    return "banner";
                case HDCAdChannel.Mrec:
                    return "mrec";
                case HDCAdChannel.Popup:
                    return "popup";
                default:
                    return "unknown";
            }
        }

        internal IReadOnlyList<KeyValuePair<string, object>> Parameters()
        {
            var parameters = new List<KeyValuePair<string, object>>();
            AddText(parameters, "ad_name", Name);
            AddText(parameters, "ad_channel", ChannelName(Revenue.Channel));
            AddText(parameters, "ad_position", Revenue.Position);
            AddText(parameters, "ad_group", Revenue.Group);
            AddText(parameters, "ad_format", Revenue.Format);
            AddText(parameters, "ad_network", Revenue.Network);
            AddText(parameters, "ad_source", Revenue.AdSource);
            AddText(parameters, "ad_unit", Revenue.AdUnitId);
            parameters.Add(new KeyValuePair<string, object>("value", Revenue.Value));
            AddText(parameters, "currency", Revenue.Currency);
            parameters.Add(new KeyValuePair<string, object>("precision", (long)Revenue.Precision));
            parameters.Add(new KeyValuePair<string, object>("session_count", (long)SessionCount));
            parameters.Add(new KeyValuePair<string, object>("name_paid_count", (long)NamePaidCount));
            parameters.Add(new KeyValuePair<string, object>("total_paid_count", (long)TotalPaidCount));
            parameters.Add(new KeyValuePair<string, object>("total_value", TotalValue));
            return parameters;
        }

        private static void AddText(List<KeyValuePair<string, object>> parameters, string key, string value)
        {
            if (string.IsNullOrEmpty(value))
                return;
            parameters.Add(new KeyValuePair<string, object>(key, value.Length > MaxTextLength ? value.Substring(0, MaxTextLength) : value));
        }
    }
}
