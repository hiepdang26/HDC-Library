using System;
using GoogleMobileAds.Api;
using HDC.Ads.Diagnostics;
using HDC.Ads.Domain;
using UnityEngine;

namespace HDC.Ads.Infrastructure
{
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

        internal static uint PreloadBufferSize(int bufferSize) => (uint)Mathf.Clamp(bufferSize <= 0 ? 2 : bufferSize, 1, 5);

        internal string Id { get; }
        internal string AdUnitId { get; }
        internal bool Preload { get; }
        internal uint BufferSize { get; }

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

        protected abstract bool HasLoadedAd { get; }
        protected abstract bool HasPreloadedAd { get; }
        protected abstract void RequestAd();
        protected abstract bool ShowLoadedAd();
        protected abstract bool ShowPreloadedAd();
        protected abstract void DestroyLoadedAd();
        protected abstract void StartPreload(PreloadConfiguration configuration);
        protected abstract void DestroyPreloadedAds();

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
