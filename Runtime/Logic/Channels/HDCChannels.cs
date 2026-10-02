using System;
using System.Collections.Generic;

namespace HDC.Ads.Logic
{
    internal sealed class HDCChannels
    {
        internal HDCChannels(HDCAdsContext context, IEnumerable<Func<HDCAdsContext, IAdChannel>> extraChannels = null)
        {
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
