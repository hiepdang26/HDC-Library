using System;
using HDC.Ads.Domain;
using HDC.Ads.Ports;

namespace HDC.Ads.Infrastructure
{
    internal sealed class HDCNativeInterstitialAd : HDCSdkAd, IFullscreenAd, IFullscreenCompanion
    {
        private readonly int bufferSize;
        private readonly bool autoReload;
        private readonly HDCNativeAfterInterstitial after;

        internal HDCNativeInterstitialAd(string id, string adUnitId, int bufferSize, bool autoReload, HDCNativeAfterInterstitial after = null)
            : base(id, HDCAdFormat.Interstitial)
        {
            AdUnitId = adUnitId;
            this.bufferSize = Math.Max(1, bufferSize);
            this.autoReload = autoReload;
            this.after = after;
        }

        public string AdUnitId { get; }

        public bool IsReady => HDCAdsSdk.IsInterstitialReady(Id);

        public IFullscreenAd Companion => after?.Ad;

        public event Action CompanionOpening
        {
            add
            {
                if (after != null)
                    after.Opening += value;
            }
            remove
            {
                if (after != null)
                    after.Opening -= value;
            }
        }

        public void Load() => HDCAdsSdk.LoadInterstitial(Id, new[] { AdUnitId }, bufferSize, autoReload);

        public bool Show()
        {
            after?.BeforeInterstitialShow(Id);
            bool started = HDCAdsSdk.ShowInterstitial(Id);
            if (!started)
                after?.InterstitialNotShown(Id);
            return started;
        }

        public void Destroy()
        {
            StopEvents();
            after?.InterstitialNotShown(Id);
            after?.Destroy();
            HDCAdsSdk.DestroyInterstitial(Id);
        }

        protected override void OnOwnEvent(HDCAdEvent adEvent)
        {
            if (after == null)
                return;
            if (adEvent.type == HDCAdEventType.Shown)
                after.InterstitialShown(Id);
            else if (adEvent.type == HDCAdEventType.ShowFailed)
                after.InterstitialNotShown(Id);
        }
    }
}
