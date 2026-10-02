using HDC.Ads.Ports;

namespace HDC.Ads.Infrastructure
{
    /// <summary>A rewarded, app open or interstitial ad of the Google Mobile Ads plugin.</summary>
    internal sealed class HDCGmaFullscreenAdapter : HDCSdkAd, IFullscreenAd
    {
        private readonly HDCGmaFullscreenAd ad;

        internal HDCGmaFullscreenAdapter(string format, HDCGmaFullscreenAd ad) : base(ad.Id, format)
        {
            this.ad = ad;
        }

        public string AdUnitId => ad.AdUnitId;

        public bool IsReady => ad.IsReady;

        public void Load() => ad.Load();

        public bool Show() => ad.Show(null);

        public void Destroy()
        {
            StopEvents();
            ad.Destroy();
        }
    }
}
