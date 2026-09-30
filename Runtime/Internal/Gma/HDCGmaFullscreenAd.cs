using System;
using GoogleMobileAds.Api;
using UnityEngine;

namespace HDC.Ads.Internal
{
    /// <summary>
    /// A full-screen ad from the Google Mobile Ads Unity plugin, kept ready for one instance id.
    /// <para>
    /// Normal mode holds one ad: it loads again after every show, retries failed loads with a growing
    /// delay, and loads when a show finds no ad. Preload mode hands loading to the plugin's preloader, which
    /// keeps a buffer of ads and reloads by itself.
    /// </para>
    /// </summary>
    internal abstract class HDCGmaFullscreenAd
    {
        private readonly string format;
        private readonly HDCRetry retry = new HDCRetry();
        private bool loading;
        private bool showing;
        private bool rewardEarned;
        private Action<bool> onClosed;

        protected HDCGmaFullscreenAd(string format, string id, string adUnitId, bool preload, int bufferSize)
        {
            this.format = format;
            Id = id;
            AdUnitId = adUnitId;
            Preload = preload;
            BufferSize = PreloadBufferSize(bufferSize);
        }

        /// <summary>The preloader keeps 1 to 5 ads; 0 or less picks the plugin's default of 2.</summary>
        internal static uint PreloadBufferSize(int bufferSize) => (uint)Mathf.Clamp(bufferSize <= 0 ? 2 : bufferSize, 1, 5);

        internal string Id { get; }
        internal string AdUnitId { get; }
        internal bool Preload { get; }
        internal uint BufferSize { get; }
        protected bool IsDestroyed { get; private set; }
        protected string PreloadId => format + "_" + Id;

        internal bool IsReady => !IsDestroyed && !showing && (Preload ? HasPreloadedAd : HasLoadedAd);

        internal void Load()
        {
            if (IsDestroyed)
                return;

            if (Preload)
            {
                StartPreload();
                return;
            }

            if (loading || showing || HasLoadedAd)
                return;

            retry.Cancel();
            loading = true;
            DestroyLoadedAd();
            try
            {
                RequestAd();
            }
            catch (Exception exception)
            {
                OnLoadFailed(null, exception.Message);
            }
        }

        /// <summary>
        /// Shows the ready ad. <paramref name="closed"/> runs once it closes or fails to show, with whether the
        /// player earned a reward.
        /// </summary>
        internal bool Show(Action<bool> closed)
        {
            if (!IsReady)
            {
                Emit(HDCGma.Event(Id, format, HDCAdEventType.ShowFailed, AdUnitId, null).WithError(null, "Not ready. Load first"));
                if (!Preload && !retry.IsWaiting)
                    Load();
                return false;
            }

            showing = true;
            rewardEarned = false;
            onClosed = closed;
            try
            {
                if (Preload ? ShowPreloadedAd() : ShowLoadedAd())
                    return true;
                OnShowFailed(null, null, "The preloaded ad was already taken");
            }
            catch (Exception exception)
            {
                OnShowFailed(null, null, exception.Message);
            }

            return false;
        }

        internal void Destroy()
        {
            IsDestroyed = true;
            retry.Reset();
            DestroyLoadedAd();
            if (Preload)
                DestroyPreloadedAds();
            Finish(false);
        }

        // Plugin calls, one set per ad class.

        protected abstract bool HasLoadedAd { get; }
        protected abstract bool HasPreloadedAd { get; }
        protected abstract void RequestAd();
        protected abstract bool ShowLoadedAd();
        protected abstract bool ShowPreloadedAd();
        protected abstract void DestroyLoadedAd();
        protected abstract void StartPreload(PreloadConfiguration configuration);
        protected abstract void DestroyPreloadedAds();

        // Callbacks from the ad classes, already on the main thread. Each ad's response is read once, when it
        // loads, so late callbacks from a closed ad still report the ad that raised them.

        protected void OnLoaded(ResponseInfo response)
        {
            loading = false;
            retry.Reset();
            Emit(HDCGma.Event(Id, format, HDCAdEventType.Loaded, AdUnitId, response));
        }

        protected void OnLoadFailed(AdError error, string fallbackMessage)
        {
            loading = false;
            Emit(HDCGma.Event(Id, format, HDCAdEventType.LoadFailed, AdUnitId, null).WithError(error, fallbackMessage));
            if (!Preload && !IsDestroyed)
                retry.Schedule(Load);
        }

        protected void OnPreloaded(ResponseInfo response) =>
            Emit(HDCGma.Event(Id, format, HDCAdEventType.Loaded, AdUnitId, response));

        protected void OnPreloadFailed(AdError error) =>
            Emit(HDCGma.Event(Id, format, HDCAdEventType.LoadFailed, AdUnitId, null).WithError(error, "Preload failed"));

        protected void OnEvent(string type, ResponseInfo response) => Emit(HDCGma.Event(Id, format, type, AdUnitId, response));

        protected void OnPaid(AdValue value, ResponseInfo response) =>
            Emit(HDCGma.Event(Id, format, HDCAdEventType.Paid, AdUnitId, response).WithValue(value));

        protected void OnRewarded(Reward reward, ResponseInfo response)
        {
            rewardEarned = true;
            HDCAdEvent adEvent = HDCGma.Event(Id, format, HDCAdEventType.Rewarded, AdUnitId, response);
            adEvent.rewardType = reward?.Type;
            adEvent.rewardAmount = reward?.Amount ?? 0d;
            Emit(adEvent);
        }

        protected void OnClosed(ResponseInfo response)
        {
            // The plugin can report both a failure and a close for one show; only the first one counts.
            if (!showing)
                return;

            Emit(HDCGma.Event(Id, format, HDCAdEventType.Closed, AdUnitId, response));
            AfterShow(rewardEarned);
        }

        protected void OnShowFailed(AdError error, ResponseInfo response, string fallbackMessage)
        {
            if (!showing)
                return;

            Emit(HDCGma.Event(Id, format, HDCAdEventType.ShowFailed, AdUnitId, response).WithError(error, fallbackMessage));
            AfterShow(false);
        }

        private void AfterShow(bool rewarded)
        {
            DestroyLoadedAd();
            Finish(rewarded);
            if (!Preload)
                Load();
        }

        private void Finish(bool rewarded)
        {
            showing = false;
            Action<bool> callback = onClosed;
            onClosed = null;
            try
            {
                callback?.Invoke(rewarded);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private void StartPreload()
        {
            try
            {
                StartPreload(new PreloadConfiguration { AdUnitId = AdUnitId, BufferSize = BufferSize, Request = new AdRequest() });
            }
            catch (Exception exception)
            {
                OnPreloadFailed(null);
                Debug.LogException(exception);
            }
        }

        private void Emit(HDCAdEvent adEvent)
        {
            if (!IsDestroyed)
                HDCAdsSdk.Emit(adEvent);
        }
    }

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

    internal sealed class HDCGmaAppOpenAd : HDCGmaFullscreenAd
    {
        // Google expires app open ads four hours after they load.
        private const float ExpirySeconds = 4 * 3600;

        private AppOpenAd ad;
        private float loadedAt;

        internal HDCGmaAppOpenAd(string id, string adUnitId, bool preload, int bufferSize)
            : base(HDCAdFormat.AppOpen, id, adUnitId, preload, bufferSize)
        {
        }

        protected override bool HasLoadedAd =>
            ad != null && ad.CanShowAd() && Time.realtimeSinceStartup - loadedAt < ExpirySeconds;

        protected override bool HasPreloadedAd => AppOpenAdPreloader.IsAdAvailable(PreloadId);

        protected override void RequestAd() =>
            AppOpenAd.Load(AdUnitId, new AdRequest(), (loaded, error) => HDCMainThread.Post(() =>
            {
                if (loaded == null || error != null || IsDestroyed)
                {
                    loaded?.Destroy();
                    if (!IsDestroyed)
                        OnLoadFailed(error, "No ad returned");
                    return;
                }

                loadedAt = Time.realtimeSinceStartup;
                OnLoaded(Attach(loaded));
            }));

        protected override bool ShowLoadedAd()
        {
            ad.Show();
            return true;
        }

        protected override bool ShowPreloadedAd()
        {
            AppOpenAd preloaded = AppOpenAdPreloader.DequeueAd(PreloadId);
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
            AppOpenAdPreloader.Preload(
                PreloadId,
                configuration,
                (_, preloaded) => HDCMainThread.Post(() => OnPreloaded(preloaded)),
                (_, error) => HDCMainThread.Post(() => OnPreloadFailed(error)),
                null);

        protected override void DestroyPreloadedAds() => AppOpenAdPreloader.Destroy(PreloadId);

        private ResponseInfo Attach(AppOpenAd loaded)
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
