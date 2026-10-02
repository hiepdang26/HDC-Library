using System;
using HDC.Ads.Infrastructure;

namespace HDC.Ads.Logic
{
    /// <summary>A native unit served as an interstitial.</summary>
    internal sealed class HDCNativeInterstitialSource : HDCFullscreenSource
    {
        private readonly string adUnitId;
        private readonly int bufferSize;
        private readonly bool autoReload;

        internal HDCNativeInterstitialSource(string id, string adUnitId, int bufferSize, bool autoReload)
            : base(id, HDCAdFormat.Interstitial)
        {
            this.adUnitId = adUnitId;
            this.bufferSize = Math.Max(1, bufferSize);
            this.autoReload = autoReload;
        }

        internal override string AdUnitId => adUnitId;

        internal override bool IsReady => HDCAdsSdk.IsInterstitialReady(Id);

        internal override void Load() => HDCAdsSdk.LoadInterstitial(Id, new[] { adUnitId }, bufferSize, autoReload);

        protected override bool StartShow() => HDCAdsSdk.ShowInterstitial(Id);

        protected override void DestroyAd() => HDCAdsSdk.DestroyInterstitial(Id);
    }
}
