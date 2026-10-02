using System.Globalization;

namespace HDC.Ads
{
    /// <summary>The channels of <see cref="HDCAds"/>.</summary>
    public enum HDCAdChannel
    {
        /// <summary>An ad that no channel showed, so none can claim it.</summary>
        Unknown,
        ForceAd,
        Rewarded,
        AppLaunch,
        AppResume,
        Banner,
        Mrec,
        Popup,
    }

    /// <summary>
    /// What one ad impression earned, as the ad network reported it, and where the game showed the ad. See
    /// <see cref="HDCAds.Revenue"/>.
    /// </summary>
    public sealed class HDCAdRevenue
    {
        /// <summary>The network of every ad today: Google Mobile Ads, with its mediation.</summary>
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

        /// <summary>
        /// The game position for force ads, rewarded ads and popups, the slot (such as "FullBottom") for
        /// banners, empty for the other channels.
        /// </summary>
        public string Position { get; }

        /// <summary>The ad's format, one of the <see cref="HDCAdFormat"/> values.</summary>
        public string Format { get; }

        /// <summary>The ad network HDC loaded the ad through: <see cref="AdMob"/>.</summary>
        public string Network { get; }

        /// <summary>
        /// The ad source that filled the ad through the network's mediation, such as "AdMob Network" or
        /// "Meta Audience Network"; empty when the network did not say.
        /// </summary>
        public string AdSource { get; }

        public string AdUnitId { get; }

        /// <summary>The revenue, in <see cref="Currency"/> units.</summary>
        public double Value { get; }

        /// <summary>The ISO 4217 code of <see cref="Value"/>'s currency, such as "USD".</summary>
        public string Currency { get; }

        /// <summary>
        /// How exact <see cref="Value"/> is, as Google Mobile Ads grades it: 0 unknown, 1 estimated, 2 publisher
        /// provided, 3 precise.
        /// </summary>
        public int Precision { get; }

        public override string ToString()
        {
            string where = Position.Length > 0 ? Channel + " " + Position : Channel.ToString();
            string source = AdSource.Length > 0 ? AdSource : Network;
            return $"{where} {Format} {Value.ToString("0.######", CultureInfo.InvariantCulture)} {Currency} from {source}";
        }
    }
}
