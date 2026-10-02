using System.Collections.Generic;
using System.Linq;
using HDC.Ads.Domain;
using HDC.Ads.Ports;

namespace HDC.Ads.Infrastructure
{
    /// <summary>
    /// The native library, serving the "androidUnit" units on Android and iOS alike: native full-screen ads, or
    /// interstitials, for force ads, rewarded and resume ads, the bottom banner and popups.
    /// </summary>
    internal sealed class HDCNativeNetwork : IAdNetwork
    {
        public string UnitKey => HDCAdUnitKeys.Native;

        public string Name => "Native";

        // The native library loads its ads through Google Mobile Ads too.
        public string RevenueNetwork => HDCAdRevenue.AdMob;

        public HDCAdPlan Plan(HDCAdUse use, HDCAdUnitSpec spec)
        {
            if (!(spec.Unit is HDCAdCoreConfig.NativeUnit unit))
                return null;
            if (use == HDCAdUse.Banner)
            {
                // Only the bottom slot has the native banner, which loads from all of the unit's ids.
                bool hasUnit = !string.IsNullOrEmpty(unit.id) || (unit.ids != null && unit.ids.Length > 0);
                return spec.BannerSlot == HDCBannerSlot.FullBottom && hasUnit
                    ? new HDCAdPlan(this, use, HDCAdNames.NativeBanner, HDCAdFormat.Banner, string.Join(", ", BannerUnitIds(unit)), spec)
                    : null;
            }

            if (string.IsNullOrEmpty(unit.id))
                return null;
            switch (use)
            {
                case HDCAdUse.ForceAd:
                    return SwitchesToInterstitial(unit)
                        ? new HDCAdPlan(this, use, HDCAdNames.NativeInterstitialForceAd(spec.SlotName), HDCAdFormat.Interstitial, unit.id, spec)
                        : new HDCAdPlan(this, use, HDCAdNames.NativeForceAd(spec.SlotName), HDCAdFormat.Fullscreen, unit.id, spec);
                case HDCAdUse.Rewarded:
                    return new HDCAdPlan(this, use, HDCAdNames.NativeRewarded, HDCAdFormat.Fullscreen, unit.id, spec);
                case HDCAdUse.AppResume:
                    return new HDCAdPlan(this, use, HDCAdNames.NativeAppResume, HDCAdFormat.Fullscreen, unit.id, spec);
                case HDCAdUse.Popup:
                    return new HDCAdPlan(this, use, HDCAdNames.NativePopup(spec.SlotName), HDCAdFormat.Popup, unit.id, spec);
                default:
                    return null;
            }
        }

        public IFullscreenAd CreateFullscreen(HDCAdPlan plan)
        {
            var unit = (HDCAdCoreConfig.NativeUnit)plan.Spec.Unit;
            if (plan.Format == HDCAdFormat.Interstitial)
            {
                HDCAdCoreConfig.Interstitials interstitials = unit.androidInterstitials;
                int bufferSize = interstitials.isPreloadAd && interstitials.bufferSize > 0 ? interstitials.bufferSize : 1;
                return new HDCNativeInterstitialAd(plan.InstanceId, unit.id, bufferSize, plan.Spec.ReloadAfterShow);
            }

            var layouts = new HDCLayoutPicker(plan.Spec.CoreConfig, unit.layoutGroupName);
            return new HDCNativeFullscreenAd(plan.InstanceId, unit.id, plan.Spec.ReloadAfterShow, layouts);
        }

        public IViewAd CreateView(HDCAdPlan plan)
        {
            var unit = (HDCAdCoreConfig.NativeUnit)plan.Spec.Unit;
            var options = new HDCBannerOptions { layoutNames = unit.layouts ?? new string[0], timeReload = unit.reloadTime };
            return new HDCNativeBannerAd(plan.InstanceId, BannerUnitIds(unit), options);
        }

        public IPopupAd CreatePopup(HDCAdPlan plan) =>
            new HDCNativePopupAd(plan.InstanceId, (HDCAdCoreConfig.NativeUnit)plan.Spec.Unit, plan.Spec.ReloadAfterShow);

        private static bool SwitchesToInterstitial(HDCAdCoreConfig.NativeUnit unit) =>
            unit.androidInterstitials != null && unit.androidInterstitials.switchToInterstitialAndroid;

        // The banner's ids: id first, then ids, trimmed and without repeats.
        private static string[] BannerUnitIds(HDCAdCoreConfig.NativeUnit unit)
        {
            var ids = new List<string>();
            foreach (string adUnitId in new[] { unit.id }.Concat(unit.ids ?? new string[0]))
            {
                if (!string.IsNullOrWhiteSpace(adUnitId) && !ids.Contains(adUnitId.Trim()))
                    ids.Add(adUnitId.Trim());
            }

            return ids.ToArray();
        }
    }
}
