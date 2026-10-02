using HDC.Ads.Domain;

namespace HDC.Ads.Infrastructure
{
    /// <summary>
    /// Google's sample ad units, which always serve test ads. While <see cref="HDCAdsSdk.UseTestAdUnits"/> is on,
    /// every load uses the sample unit of its format in place of the configured one.
    /// </summary>
    internal static class HDCTestAdUnits
    {
#if UNITY_IOS
        private const string AppOpen = "ca-app-pub-3940256099942544/5575463023";
        private const string AdaptiveBanner = "ca-app-pub-3940256099942544/2435281174";
        private const string FixedBanner = "ca-app-pub-3940256099942544/2934735716";
        private const string Interstitial = "ca-app-pub-3940256099942544/4411468910";
        private const string Rewarded = "ca-app-pub-3940256099942544/1712485313";
        private const string Native = "ca-app-pub-3940256099942544/3986624511";
#else
        private const string AppOpen = "ca-app-pub-3940256099942544/9257395921";
        private const string AdaptiveBanner = "ca-app-pub-3940256099942544/9214589741";
        private const string FixedBanner = "ca-app-pub-3940256099942544/6300978111";
        private const string Interstitial = "ca-app-pub-3940256099942544/1033173712";
        private const string Rewarded = "ca-app-pub-3940256099942544/5224354917";
        private const string Native = "ca-app-pub-3940256099942544/2247696110";
#endif

        /// <summary>The ad unit a load of <paramref name="format"/> uses: the sample one while test units are on.</summary>
        internal static string Pick(string format, string adUnitId)
        {
            if (!HDCAdsSdk.UseTestAdUnits || string.IsNullOrWhiteSpace(adUnitId))
                return adUnitId;

            switch (format)
            {
                case HDCAdFormat.Interstitial: return Interstitial;
                case HDCAdFormat.Rewarded: return Rewarded;
                case HDCAdFormat.AppOpen: return AppOpen;
                case HDCAdFormat.Mrec: return FixedBanner;
                case HDCAdFormat.BannerView: return AdaptiveBanner;
                // Native full-screen ads, popups and the native banner.
                default: return Native;
            }
        }

        /// <summary>The ad units a native load tries: the one sample unit of its format while test units are on.</summary>
        internal static string[] Pick(string format, string[] adUnitIds)
        {
            if (!HDCAdsSdk.UseTestAdUnits || adUnitIds == null || adUnitIds.Length == 0)
                return adUnitIds;
            return new[] { Pick(format, "test") };
        }

        /// <summary>The ad unit of a banner view: adaptive across the screen, fixed size in the corners and for MREC.</summary>
        internal static string PickBanner(HDCBannerViewPlacement placement, string adUnitId)
        {
            if (!HDCAdsSdk.UseTestAdUnits || string.IsNullOrWhiteSpace(adUnitId))
                return adUnitId;
            return placement == HDCBannerViewPlacement.FullBottom || placement == HDCBannerViewPlacement.FullTop ? AdaptiveBanner : FixedBanner;
        }
    }
}
