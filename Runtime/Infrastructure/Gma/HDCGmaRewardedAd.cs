using GoogleMobileAds.Api;
using HDC.Ads.Domain;

namespace HDC.Ads.Infrastructure
{
    internal sealed class HDCGmaRewardedAd : HDCGmaFullscreenAd
    {
        private RewardedAd ad;
        private ResponseInfo response;

        internal HDCGmaRewardedAd(string id, string adUnitId, bool preload, int bufferSize)
            : base(HDCAdFormat.Rewarded, id, adUnitId, preload, bufferSize)
        {
        }

        protected override bool HasLoadedAd => ad != null && ad.CanShowAd();

        protected override bool HasPreloadedAd => RewardedAdPreloader.IsAdAvailable(PreloadId);

        protected override void RequestAd() =>
            RewardedAd.Load(AdUnitId, new AdRequest(), (loaded, error) => HDCMainThread.Post(() =>
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
            ResponseInfo shown = response;
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
                (_, preloaded) => HDCMainThread.Post(() => OnPreloaded(preloaded)),
                (_, error) => HDCMainThread.Post(() => OnPreloadFailed(error)),
                null);

        protected override void DestroyPreloadedAds() => RewardedAdPreloader.Destroy(PreloadId);

        private ResponseInfo Attach(RewardedAd loaded)
        {
            ResponseInfo info = loaded.GetResponseInfo();
            ad = loaded;
            response = info;
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
