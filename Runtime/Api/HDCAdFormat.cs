namespace HDC.Ads
{
    /// <summary>The formats ads come in, as <see cref="HDCAdRevenue.Format"/> names them.</summary>
    public static class HDCAdFormat
    {
        /// <summary>The SDK itself, for its own events; no ad has it.</summary>
        public const string Sdk = "sdk";

        public const string Interstitial = "interstitial";

        /// <summary>A native full-screen ad.</summary>
        public const string Fullscreen = "fullscreen";

        /// <summary>The native banner, which can expand.</summary>
        public const string Banner = "banner";

        /// <summary>A native popup.</summary>
        public const string Popup = "popup";

        public const string Rewarded = "rewarded";
        public const string AppOpen = "appOpen";

        /// <summary>A Google Mobile Ads banner, adaptive or 320x50.</summary>
        public const string BannerView = "bannerView";

        /// <summary>A Google Mobile Ads 300x250 medium rectangle.</summary>
        public const string Mrec = "mrec";
    }
}
