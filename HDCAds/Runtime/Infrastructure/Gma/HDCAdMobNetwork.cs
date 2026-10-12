using HDC.Ads.Domain;
using HDC.Ads.Ports;

namespace HDC.Ads.Infrastructure
{
    internal sealed class HDCAdMobNetwork : IAdNetwork
    {
        public string UnitKey => HDCAdUnitKeys.AdMob;

        public string Name => "AdMob";

        public string RevenueNetwork => HDCAdRevenue.AdMob;

        public HDCAdPlan Plan(HDCAdUse use, HDCAdUnitSpec spec)
        {
            if (!(spec.Unit is HDCAdCoreConfig.AdmobUnit unit) || string.IsNullOrEmpty(unit.id))
                return null;
            switch (use)
            {
                case HDCAdUse.ForceAd:
                    return new HDCAdPlan(this, use, HDCAdNames.AdMobForceAd(spec.SlotName), HDCAdFormat.Interstitial, unit.id, spec);
                case HDCAdUse.Rewarded:
                    return new HDCAdPlan(this, use, HDCAdNames.AdMobRewarded, HDCAdFormat.Rewarded, unit.id, spec);
                case HDCAdUse.AppOpen:
                    return new HDCAdPlan(this, use, HDCAdNames.AdMobAppOpen, HDCAdFormat.AppOpen, unit.id, spec);
                case HDCAdUse.AppResume:
                    return new HDCAdPlan(this, use, HDCAdNames.AdMobAppResume, HDCAdFormat.Interstitial, unit.id, spec);
                case HDCAdUse.Banner:
                    return new HDCAdPlan(this, use, HDCAdNames.AdMobBanner, HDCAdFormat.BannerView, unit.id, spec);
                case HDCAdUse.Mrec:
                    return new HDCAdPlan(this, use, HDCAdNames.AdMobMrec, HDCAdFormat.Mrec, unit.id, spec);
                default:
                    return null;
            }
        }

        public IFullscreenAd CreateFullscreen(HDCAdPlan plan)
        {
            var unit = (HDCAdCoreConfig.AdmobUnit)plan.Spec.Unit;
            switch (plan.Use)
            {
                case HDCAdUse.ForceAd:
                case HDCAdUse.AppResume:
                    var interstitial = new HDCGmaInterstitialAd(plan.InstanceId, unit.id, unit.preloadAd, unit.adBufferSize)
                    {
                        LoadOnce = !plan.Spec.ReloadAfterShow && !unit.preloadAd,
                    };
                    return new HDCGmaFullscreenAdapter(plan.Format, interstitial);
                case HDCAdUse.Rewarded:
                    return new HDCGmaFullscreenAdapter(plan.Format, new HDCGmaRewardedAd(plan.InstanceId, unit.id, unit.preloadAd, unit.adBufferSize));
                case HDCAdUse.AppOpen:
                    return new HDCGmaFullscreenAdapter(plan.Format, new HDCGmaAppOpenAd(plan.InstanceId, unit.id, unit.preloadAd, unit.adBufferSize));
                default:
                    return null;
            }
        }

        public IViewAd CreateView(HDCAdPlan plan) =>
            new HDCGmaViewAd(plan.InstanceId, plan.AdUnitId, plan.Use == HDCAdUse.Mrec ? HDCBannerViewPlacement.Mrec : HDCBannerViewPlacement.FullBottom);

        public IPopupAd CreatePopup(HDCAdPlan plan) => null;
    }
}
