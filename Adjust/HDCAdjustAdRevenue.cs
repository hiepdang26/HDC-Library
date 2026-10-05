using System.Globalization;

namespace HDC.Ads
{
    internal readonly struct HDCAdjustAdRevenue
    {
        internal HDCAdjustAdRevenue(string source, double revenue, string currency, string network, string unit, string placement)
        {
            Source = source ?? "";
            Revenue = revenue;
            Currency = currency ?? "";
            Network = network ?? "";
            Unit = unit ?? "";
            Placement = placement ?? "";
        }

        internal string Source { get; }

        internal double Revenue { get; }

        internal string Currency { get; }

        internal string Network { get; }

        internal string Unit { get; }

        internal string Placement { get; }

#if HDC_ADS
        internal static HDCAdjustAdRevenue From(HDCAdRevenue revenue) =>
            new HDCAdjustAdRevenue(HDCAdjust.AdMobRevenueSource, revenue.Value, revenue.Currency,
                revenue.AdSource.Length > 0 ? revenue.AdSource : revenue.Network, revenue.AdUnitId, revenue.Position);
#endif

        public override string ToString() =>
            $"{Source} {Revenue.ToString("0.########", CultureInfo.InvariantCulture)} {Currency}, network {Network}, unit {Unit}, placement {Placement}";
    }
}
