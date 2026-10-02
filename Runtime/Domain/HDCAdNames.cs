namespace HDC.Ads.Domain
{
    /// <summary>
    /// The names HDCLib gives things, in one place: the instance ids ads load with, which their events carry and
    /// the debug panel reads, and the keys of the values kept across sessions.
    /// </summary>
    internal static class HDCAdNames
    {
        // Instance ids start with their channel's prefix, which the debug panel sorts events by.
        internal const string ForceAdPrefix = "fa_";
        internal const string RewardedPrefix = "rw_";
        internal const string AppOpenPrefix = "ao_";
        internal const string BannerPrefix = "bn_";
        internal const string MrecPrefix = "mrec_";
        internal const string PopupPrefix = "pu_";

        internal const string AdMobRewarded = "rw_plugin";
        internal const string NativeRewarded = "rw_native";
        internal const string AdMobAppOpen = "ao_plugin";
        internal const string NativeAppResume = "native_resume";
        internal const string NativeBanner = "bn_native";
        internal const string AdMobMrec = "mrec_plugin";

        // Values kept across sessions.
        internal const string AdsRemovedKey = "REMOVEADS";
        internal const string ForceAdTotalKey = "fa_total_impression";
        internal const string RewardedCountKey = "rw_count";

        internal static string AdMobForceAd(string group) => "fa_plugin_" + group;

        internal static string NativeForceAd(string group) => ForceAdPrefix + group;

        internal static string NativeInterstitialForceAd(string group) => "fa_interstitial_" + group;

        internal static string AdMobBanner(HDCBannerSlot slot) => "bn_plugin_" + slot;

        internal static string NativePopup(string group) => PopupPrefix + group;

        internal static string ForceAdCountKey(string position) => "fa_count_" + position;
    }
}
