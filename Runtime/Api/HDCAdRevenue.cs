using System.Globalization;

namespace HDC.Ads
{
    public sealed class HDCAdRevenue
    {
        public const string AdMob = "AdMob";

        internal HDCAdRevenue(HDCAdChannel channel, string position, string format, string network, string adSource,
            string adUnitId, double value, string currency, int precision)
        {
            Channel = channel;
            Position = position ?? "";
            Format = format ?? "";
            Network = network ?? "";
            AdSource = adSource ?? "";
            AdUnitId = adUnitId ?? "";
            Value = value;
            Currency = currency ?? "";
            Precision = precision;
        }

        public HDCAdChannel Channel { get; }

        public string Position { get; }

        public string Format { get; }

        public string Network { get; }

        public string AdSource { get; }

        public string AdUnitId { get; }

        public double Value { get; }

        public string Currency { get; }

        public int Precision { get; }

        public override string ToString()
        {
            string where = Position.Length > 0 ? Channel + " " + Position : Channel.ToString();
            string source = AdSource.Length > 0 ? AdSource : Network;
            return $"{where} {Format} {Value.ToString("0.######", CultureInfo.InvariantCulture)} {Currency} from {source}";
        }
    }
}
