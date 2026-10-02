using System;
using System.Collections.Generic;

namespace HDC.Ads.Logic
{
    /// <summary>
    /// The channel objects behind <see cref="HDCAds"/>' interfaces. Game code sees the interfaces; the library
    /// and its debug panel reach the rest of each channel here.
    /// </summary>
    internal sealed class HDCChannels
    {
        /// <param name="extraChannels">Channels beyond the seven, made with the context after them, such as a test's.</param>
        internal HDCChannels(HDCAdsContext context, IEnumerable<Func<HDCAdsContext, IAdChannel>> extraChannels = null)
        {
            // The order they were always made in, which sets the order their handlers of shared events run.
            ForceAd = new HDCForceAds(context);
            Rewarded = new HDCRewardedAds(context);
            AppLaunch = new HDCAppLaunchAds(context, ForceAd);
            AppResume = new HDCAppResumeAds(context);
            Banner = new HDCBannerAds(context);
            Mrec = new HDCMrecAds(context);
            Popup = new HDCPopupAds(context);

            var all = new List<IAdChannel> { AppLaunch, AppResume, Rewarded, ForceAd, Banner, Mrec, Popup };
            foreach (Func<HDCAdsContext, IAdChannel> make in extraChannels ?? new Func<HDCAdsContext, IAdChannel>[0])
                all.Add(make(context));
            All = all;
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

        /// <summary>
        /// Every channel, in the order they start once the SDK is ready, the launch ad first, and the order the
        /// debug panel lists them in.
        /// </summary>
        internal IReadOnlyList<IAdChannel> All { get; }

        private void Start()
        {
            foreach (IAdChannel channel in All)
                channel.OnSdkInitialized();
        }

        private void HideRemovedAds()
        {
            foreach (IAdChannel channel in All)
                channel.OnAdsRemoved();
        }
    }
}
