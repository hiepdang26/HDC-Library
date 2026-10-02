namespace HDC.Ads.Logic
{
    /// <summary>
    /// The channel objects behind <see cref="HDCAds"/>' interfaces. Game code sees the interfaces; the library
    /// and its debug panel reach the rest of each channel here.
    /// </summary>
    internal sealed class HDCChannels
    {
        internal HDCChannels(HDCAdsContext context)
        {
            // The order they were always made in, which sets the order their handlers of shared events run.
            ForceAd = new HDCForceAds(context);
            Rewarded = new HDCRewardedAds(context);
            AppLaunch = new HDCAppLaunchAds(context, ForceAd);
            AppResume = new HDCAppResumeAds(context);
            Banner = new HDCBannerAds(context);
            Mrec = new HDCMrecAds(context);
            Popup = new HDCPopupAds(context);
            context.SdkReady += Start;
            context.AdsRemoved += HideRemovedAds;
        }

        internal HDCForceAds ForceAd { get; }
        internal HDCRewardedAds Rewarded { get; }
        internal HDCAppLaunchAds AppLaunch { get; }
        internal HDCAppResumeAds AppResume { get; }
        internal HDCBannerAds Banner { get; }
        internal HDCMrecAds Mrec { get; }
        internal HDCPopupAds Popup { get; }

        // The launch ad starts loading first.
        private void Start()
        {
            AppLaunch.OnSdkInitialized();
            AppResume.OnSdkInitialized();
            ForceAd.OnSdkInitialized();
            Rewarded.OnSdkInitialized();
            Banner.OnSdkInitialized();
            Mrec.OnSdkInitialized();
            Popup.OnSdkInitialized();
        }

        private void HideRemovedAds()
        {
            Banner.HideAll();
            Mrec.Hide();
            Popup.HideAll();
        }
    }
}
