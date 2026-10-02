using HDC.Ads.Domain;
using HDC.Ads.Infrastructure;

namespace HDC.Ads.Logic
{
    /// <summary>A banner or MREC view from the Google Mobile Ads plugin.</summary>
    internal sealed class HDCPluginRectSource : HDCRectSource
    {
        private readonly string adUnitId;
        private readonly HDCBannerViewPlacement placement;

        internal HDCPluginRectSource(string id, string adUnitId, HDCBannerViewPlacement placement)
            : base(id, placement == HDCBannerViewPlacement.Mrec ? HDCAdFormat.Mrec : HDCAdFormat.BannerView)
        {
            this.adUnitId = adUnitId;
            this.placement = placement;
        }

        internal override string AdUnitId => adUnitId;

        internal override void Load() => HDCAdsSdk.LoadBannerView(Id, adUnitId, placement);

        internal override void Show() => HDCAdsSdk.ShowBannerView(Id);

        internal override void Hide() => HDCAdsSdk.HideBannerView(Id);
    }
}
