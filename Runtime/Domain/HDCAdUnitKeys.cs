namespace HDC.Ads.Domain
{
    /// <summary>The keys of the ad core config that hold each network's ad units.</summary>
    internal static class HDCAdUnitKeys
    {
        /// <summary>Units served by the Google Mobile Ads plugin.</summary>
        internal const string AdMob = "admobUnit";

        /// <summary>Units served by the native library, on Android and iOS alike.</summary>
        internal const string Native = "androidUnit";
    }
}
