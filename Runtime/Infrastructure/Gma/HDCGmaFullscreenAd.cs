using System;
using GoogleMobileAds.Api;
using HDC.Ads.Diagnostics;
using HDC.Ads.Domain;
using UnityEngine;

namespace HDC.Ads.Infrastructure
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
            AdUnitId = HDCTestAdUnits.Pick(format, adUnitId);
            Preload = preload;
            BufferSize = PreloadBufferSize(bufferSize);
        }

        /// <summary>The preloader keeps 1 to 5 ads; 0 or less picks the plugin's default of 2.</summary>
        internal static uint PreloadBufferSize(int bufferSize) => (uint)Mathf.Clamp(bufferSize <= 0 ? 2 : bufferSize, 1, 5);

        internal string Id { get; }
        internal string AdUnitId { get; }
        internal bool Preload { get; }
        internal uint BufferSize { get; }

        /// <summary>Loads once: no reload after a show and no retry after a failed load.</summary>
        internal bool LoadOnce { get; set; }
        protected bool IsDestroyed { get; private set; }
        protected string PreloadId => format + "_" + Id;

        internal bool IsReady => !IsDestroyed && !showing && (Preload ? HasPreloadedAd : HasLoadedAd);

        internal void Load()
        {
            if (IsDestroyed)
                return;

            if (Preload)
            {
                HDCAdsTracker.Requested(format, Id, AdUnitId);
                StartPreload();
                return;
            }

            if (loading || showing || HasLoadedAd)
                return;

            retry.Cancel();
            loading = true;
            DestroyLoadedAd();
            HDCAdsTracker.Requested(format, Id, AdUnitId);
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
                if (!Preload && !LoadOnce && !retry.IsWaiting)
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
            HDCAdsTracker.Destroyed(format, Id);
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
            if (Preload || LoadOnce || IsDestroyed)
                return;
            retry.Schedule(Load);
            HDCAdsTracker.RetryScheduled(format, Id, retry.Delay, retry.Attempt);
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
            if (!Preload && !LoadOnce)
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
}
