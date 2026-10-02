using HDC.Ads.Domain;
using HDC.Ads.Ports;
using UnityEngine;

namespace HDC.Ads.Infrastructure
{
    /// <summary>The native banner along the bottom of the screen, which can expand.</summary>
    internal sealed class HDCNativeBannerAd : HDCSdkAd, IViewAd
    {
        private readonly string[] adUnitIds;
        private readonly HDCBannerOptions options;

        internal HDCNativeBannerAd(string id, string[] adUnitIds, HDCBannerOptions options) : base(id, HDCAdFormat.Banner)
        {
            this.adUnitIds = adUnitIds;
            this.options = options;
        }

        public string AdUnitId => string.Join(", ", adUnitIds);

        // The native side lays the banner out; its size is not reported.
        public Vector2 SizeInPixels => Vector2.zero;

        public void Load() => HDCAdsSdk.LoadBanner(Id, adUnitIds, options);

        public void Show() => HDCAdsSdk.ShowBanner(Id);

        public void Hide() => HDCAdsSdk.HideBanner(Id);

        public bool Expand(bool enableClick) => HDCAdsSdk.ExpandBanner(Id, enableClick);

        // The native banner stays along the bottom.
        public void Move(HDCAdPosition position)
        {
        }

        public void Move(Vector2 screenPoint)
        {
        }
    }
}
