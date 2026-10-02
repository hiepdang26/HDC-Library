
using HDC.Ads.Infrastructure;

namespace HDC.Ads.Logic
{
    /// <summary>A rewarded, app open or interstitial ad from the Google Mobile Ads plugin.</summary>
    internal sealed class HDCPluginFullscreenSource : HDCFullscreenSource
    {
        private readonly HDCGmaFullscreenAd ad;

        internal HDCPluginFullscreenSource(string format, HDCGmaFullscreenAd ad) : base(ad.Id, format)
        {
            this.ad = ad;
        }

        internal override string AdUnitId => ad.AdUnitId;

        internal override bool IsReady => ad.IsReady;

        internal override void Load() => ad.Load();

        protected override bool StartShow() => ad.Show(null);

        protected override void DestroyAd() => ad.Destroy();
    }
}
