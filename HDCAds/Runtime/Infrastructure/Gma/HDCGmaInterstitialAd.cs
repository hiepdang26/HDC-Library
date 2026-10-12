using GoogleMobileAds.Api;
using HDC.Ads.Domain;

namespace HDC.Ads.Infrastructure
{
    internal sealed class HDCGmaInterstitialAd : HDCGmaFullscreenAd
    {
        private InterstitialAd ad;

        internal HDCGmaInterstitialAd(string id, string adUnitId, bool preload, int bufferSize)
            : base(HDCAdFormat.Interstitial, id, adUnitId, preload, bufferSize)
        {
        }

        protected override bool HasLoadedAd => ad != null && ad.CanShowAd();

        protected override bool HasPreloadedAd => InterstitialAdPreloader.IsAdAvailable(PreloadId);

        protected override void RequestAd() =>
            InterstitialAd.Load(AdUnitId, new AdRequest(), (loaded, error) => HDCMainThread.Post(() =>
            {
                if (loaded == null || error != null || IsDestroyed)
                {
                    loaded?.Destroy();
                    if (!IsDestroyed)
                        OnLoadFailed(error, "No ad returned");
                    return;
                }

                OnLoaded(Attach(loaded));
            }));

        protected override bool ShowLoadedAd()
        {
            ad.Show();
            return true;
        }

        protected override bool ShowPreloadedAd()
        {
            InterstitialAd preloaded = InterstitialAdPreloader.DequeueAd(PreloadId);
            if (preloaded == null)
                return false;
            Attach(preloaded);
            return ShowLoadedAd();
        }

        protected override void DestroyLoadedAd()
        {
            ad?.Destroy();
            ad = null;
        }

        protected override void StartPreload(PreloadConfiguration configuration) =>
            InterstitialAdPreloader.Preload(
                PreloadId,
                configuration,
                (_, preloaded) => HDCMainThread.Post(() => OnPreloaded(preloaded)),
                (_, error) => HDCMainThread.Post(() => OnPreloadFailed(error)),
                null);

        protected override void DestroyPreloadedAds() => InterstitialAdPreloader.Destroy(PreloadId);

        private ResponseInfo Attach(InterstitialAd loaded)
        {
            ResponseInfo info = loaded.GetResponseInfo();
            ad = loaded;
            loaded.OnAdFullScreenContentOpened += () => HDCMainThread.Post(() => OnEvent(HDCAdEventType.Shown, info));
            loaded.OnAdImpressionRecorded += () => HDCMainThread.Post(() => OnEvent(HDCAdEventType.Impression, info));
            loaded.OnAdClicked += () => HDCMainThread.Post(() => OnEvent(HDCAdEventType.Clicked, info));
            loaded.OnAdPaid += value => HDCMainThread.Post(() => OnPaid(value, info));
            loaded.OnAdFullScreenContentClosed += () => HDCMainThread.Post(() => OnClosed(info));
            loaded.OnAdFullScreenContentFailed += error => HDCMainThread.Post(() => OnShowFailed(error, info, "Show failed"));
            return info;
        }
    }
}
