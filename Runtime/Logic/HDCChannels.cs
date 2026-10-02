namespace HDC.Ads
{
    /// <summary>
    /// The channel objects behind <see cref="HDCAds"/>' interfaces. Game code sees the interfaces; the library
    /// and its debug panel reach the rest of each channel here.
    /// </summary>
    internal sealed class HDCChannels
    {
        // The order they were always made in, which sets the order their handlers of shared events run.
        internal readonly HDCForceAds ForceAd = new HDCForceAds();
        internal readonly HDCRewardedAds Rewarded = new HDCRewardedAds();
        internal readonly HDCAppLaunchAds AppLaunch = new HDCAppLaunchAds();
        internal readonly HDCAppResumeAds AppResume = new HDCAppResumeAds();
        internal readonly HDCBannerAds Banner = new HDCBannerAds();
        internal readonly HDCMrecAds Mrec = new HDCMrecAds();
        internal readonly HDCPopupAds Popup = new HDCPopupAds();
    }
}
