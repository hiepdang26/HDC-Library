using GoogleMobileAds.Api;
using HDC.Ads.Domain;

namespace HDC.Ads.Infrastructure
{
    internal sealed class HDCGmaRewardedAd : HDCGmaFullscreenAd
    {
        private RewardedAd ad;
        private HDCGmaResponse response;

        internal HDCGmaRewardedAd(string id, string adUnitId, bool preload, int bufferSize)
            : base(HDCAdFormat.Rewarded, id, adUnitId, preload, bufferSize)
        {
        }

        protected override bool HasLoadedAd => ad != null && ad.CanShowAd();

        protected override bool HasPreloadedAd => RewardedAdPreloader.IsAdAvailable(PreloadId);

        protected override void RequestAd() =>
            RewardedAd.Load(AdUnitId, new AdRequest(), (loaded, error) =>
            {
                HDCGmaFailure failure = loaded == null || error != null ? HDCGmaFailure.Read(error, "No ad returned") : null;
                HDCMainThread.Post(() =>
                {
                    if (failure != null || IsDestroyed)
                    {
                        loaded?.Destroy();
                        if (!IsDestroyed)
                            OnLoadFailed(failure);
                        return;
                    }

                    OnLoaded(Attach(loaded));
                });
            });

        protected override bool ShowLoadedAd()
        {
            HDCGmaResponse shown = response;
            ad.Show(reward => HDCMainThread.Post(() => OnRewarded(reward, shown)));
            return true;
        }

        protected override bool ShowPreloadedAd()
        {
            RewardedAd preloaded = RewardedAdPreloader.DequeueAd(PreloadId);
            if (preloaded == null)
                return false;
            Attach(preloaded);
            return ShowLoadedAd();
        }

        protected override void DestroyLoadedAd()
        {
            ad?.Destroy();
            ad = null;
            response = null;
        }

        protected override void StartPreload(PreloadConfiguration configuration) =>
            RewardedAdPreloader.Preload(
                PreloadId,
                configuration,
                (_, preloaded) =>
                {
                    HDCGmaResponse read = HDCGmaResponse.Read(preloaded);
                    HDCMainThread.Post(() => OnPreloaded(read));
                },
                (_, error) =>
                {
                    HDCGmaFailure failure = HDCGmaFailure.Read(error, "Preload failed");
                    HDCMainThread.Post(() => OnPreloadFailed(failure));
                },
                null);

        protected override void DestroyPreloadedAds() => RewardedAdPreloader.Destroy(PreloadId);

        private HDCGmaResponse Attach(RewardedAd loaded)
        {
            HDCGmaResponse info = HDCGmaResponse.Read(loaded.GetResponseInfo());
            ad = loaded;
            response = info;
            loaded.OnAdFullScreenContentOpened += () => HDCMainThread.Post(() => OnEvent(HDCAdEventType.Shown, info));
            loaded.OnAdImpressionRecorded += () => HDCMainThread.Post(() => OnEvent(HDCAdEventType.Impression, info));
            loaded.OnAdClicked += () => HDCMainThread.Post(() => OnEvent(HDCAdEventType.Clicked, info));
            loaded.OnAdPaid += value => HDCMainThread.Post(() => OnPaid(value, info));
            loaded.OnAdFullScreenContentClosed += () => HDCMainThread.Post(() => OnClosed(info));
            loaded.OnAdFullScreenContentFailed += error =>
            {
                HDCGmaFailure failure = HDCGmaFailure.Read(error, "Show failed");
                HDCMainThread.Post(() => OnShowFailed(failure, info));
            };
            return info;
        }
    }
}
