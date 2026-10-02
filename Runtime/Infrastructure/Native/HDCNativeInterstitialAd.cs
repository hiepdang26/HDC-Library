using System;
using HDC.Ads.Ports;

namespace HDC.Ads.Infrastructure
{
    internal sealed class HDCNativeInterstitialAd : HDCSdkAd, IFullscreenAd
    {
        private readonly int bufferSize;
        private readonly bool autoReload;

        internal HDCNativeInterstitialAd(string id, string adUnitId, int bufferSize, bool autoReload)
            : base(id, HDCAdFormat.Interstitial)
        {
            AdUnitId = adUnitId;
            this.bufferSize = Math.Max(1, bufferSize);
            this.autoReload = autoReload;
        }

        public string AdUnitId { get; }

        public bool IsReady => HDCAdsSdk.IsInterstitialReady(Id);

        public void Load() => HDCAdsSdk.LoadInterstitial(Id, new[] { AdUnitId }, bufferSize, autoReload);

        public bool Show() => HDCAdsSdk.ShowInterstitial(Id);

        public void Destroy()
        {
            StopEvents();
            HDCAdsSdk.DestroyInterstitial(Id);
        }
    }
}
