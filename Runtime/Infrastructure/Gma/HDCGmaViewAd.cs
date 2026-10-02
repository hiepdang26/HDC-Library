using HDC.Ads.Domain;
using HDC.Ads.Ports;
using UnityEngine;

namespace HDC.Ads.Infrastructure
{
    /// <summary>A banner or MREC view of the Google Mobile Ads plugin.</summary>
    internal sealed class HDCGmaViewAd : HDCSdkAd, IViewAd
    {
        private readonly HDCBannerViewPlacement placement;

        internal HDCGmaViewAd(string id, string adUnitId, HDCBannerViewPlacement placement)
            : base(id, placement == HDCBannerViewPlacement.Mrec ? HDCAdFormat.Mrec : HDCAdFormat.BannerView)
        {
            AdUnitId = adUnitId;
            this.placement = placement;
        }

        public string AdUnitId { get; }

        public Vector2 SizeInPixels => HDCAdsSdk.GetBannerViewSizeInPixels(Id);

        public void Load() => HDCAdsSdk.LoadBannerView(Id, AdUnitId, placement);

        public void Show() => HDCAdsSdk.ShowBannerView(Id);

        public void Hide() => HDCAdsSdk.HideBannerView(Id);

        public bool Expand(bool enableClick) => false;

        public void Move(HDCAdPosition position) => HDCAdsSdk.MoveBannerView(Id, position);

        public void Move(Vector2 screenPoint) => HDCAdsSdk.MoveBannerView(Id, screenPoint);
    }
}
