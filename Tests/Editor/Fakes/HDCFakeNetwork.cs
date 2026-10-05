using System.Collections.Generic;
using System.Linq;
using HDC.Ads.Domain;
using HDC.Ads.Ports;

namespace HDC.Ads.Tests
{
    internal sealed class HDCFakeNetwork : IAdNetwork
    {
        private readonly HDCFakeSdk sdk;

        internal HDCFakeNetwork(string unitKey, HDCFakeSdk sdk)
        {
            UnitKey = unitKey;
            this.sdk = sdk;
        }

        public string UnitKey { get; }

        public string Name => "Fake " + UnitKey;

        public string RevenueNetwork => "Fake";

        internal List<HDCFakeAd> Ads { get; } = new List<HDCFakeAd>();

        internal bool ForceAdsHaveCompanions { get; set; }

        public HDCAdPlan Plan(HDCAdUse use, HDCAdUnitSpec spec)
        {
            string adUnitId = (spec.Unit as HDCAdCoreConfig.AdmobUnit)?.id ?? (spec.Unit as HDCAdCoreConfig.NativeUnit)?.id;
            if (string.IsNullOrEmpty(adUnitId))
                return null;
            string slot = use == HDCAdUse.Banner ? spec.BannerSlot.ToString() : spec.SlotName;
            return new HDCAdPlan(this, use, $"fake_{UnitKey}_{use}_{slot}", Format(use), adUnitId, spec);
        }

        public IFullscreenAd CreateFullscreen(HDCAdPlan plan)
        {
            HDCFakeAd ad = Made(new HDCFakeAd(plan, sdk));
            if (ForceAdsHaveCompanions && plan.Use == HDCAdUse.ForceAd)
                ad.CompanionAd = new HDCFakeAd(new HDCAdPlan(this, plan.Use, plan.InstanceId + "_companion", HDCAdFormat.Fullscreen, "companion-unit", plan.Spec), sdk);
            return ad;
        }

        public IViewAd CreateView(HDCAdPlan plan) => Made(new HDCFakeAd(plan, sdk));

        public IPopupAd CreatePopup(HDCAdPlan plan) => null;

        internal HDCFakeAd Ad(HDCAdUse use) => Ads.Single(ad => ad.Use == use);

        private HDCFakeAd Made(HDCFakeAd ad)
        {
            Ads.Add(ad);
            return ad;
        }

        private static string Format(HDCAdUse use)
        {
            switch (use)
            {
                case HDCAdUse.Rewarded: return HDCAdFormat.Rewarded;
                case HDCAdUse.AppOpen: return HDCAdFormat.AppOpen;
                case HDCAdUse.AppResume: return HDCAdFormat.Fullscreen;
                case HDCAdUse.Banner: return HDCAdFormat.BannerView;
                case HDCAdUse.Mrec: return HDCAdFormat.Mrec;
                case HDCAdUse.Popup: return HDCAdFormat.Popup;
                default: return HDCAdFormat.Interstitial;
            }
        }
    }
}
