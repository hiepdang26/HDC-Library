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
                OnLoadFailed(new HDCGmaFailure(-1, exception.Message));
            }
        }

        internal bool Show(Action<bool> closed)
        {
            if (!IsReady)
            {
                Emit(HDCGma.Event(Id, format, HDCAdEventType.ShowFailed, AdUnitId, null).WithError(new HDCGmaFailure(-1, "Not ready. Load first")));
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
                OnShowFailed(new HDCGmaFailure(-1, "The preloaded ad was already taken"), null);
            }
            catch (Exception exception)
            {
                OnShowFailed(new HDCGmaFailure(-1, exception.Message), null);
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

        protected void OnLoaded(HDCGmaResponse response)
        {
            loading = false;
            retry.Reset();
            Emit(HDCGma.Event(Id, format, HDCAdEventType.Loaded, AdUnitId, response));
        }

        protected void OnLoadFailed(HDCGmaFailure failure)
        {
            loading = false;
            Emit(HDCGma.Event(Id, format, HDCAdEventType.LoadFailed, AdUnitId, null).WithError(failure));
            if (Preload || LoadOnce || IsDestroyed)
                return;
            retry.Schedule(Load);
            HDCAdsTracker.RetryScheduled(format, Id, retry.Delay, retry.Attempt);
        }

        protected void OnPreloaded(HDCGmaResponse response) =>
            Emit(HDCGma.Event(Id, format, HDCAdEventType.Loaded, AdUnitId, response));

        protected void OnPreloadFailed(HDCGmaFailure failure) =>
            Emit(HDCGma.Event(Id, format, HDCAdEventType.LoadFailed, AdUnitId, null).WithError(failure));

        protected void OnEvent(string type, HDCGmaResponse response) => Emit(HDCGma.Event(Id, format, type, AdUnitId, response));

        protected void OnPaid(AdValue value, HDCGmaResponse response) =>
            Emit(HDCGma.Event(Id, format, HDCAdEventType.Paid, AdUnitId, response).WithValue(value));

        protected void OnRewarded(Reward reward, HDCGmaResponse response)
        {
            rewardEarned = true;
            HDCAdEvent adEvent = HDCGma.Event(Id, format, HDCAdEventType.Rewarded, AdUnitId, response);
            adEvent.rewardType = reward?.Type;
            adEvent.rewardAmount = reward?.Amount ?? 0d;
            Emit(adEvent);
        }

        protected void OnClosed(HDCGmaResponse response)
        {
            if (!showing)
                return;

            Emit(HDCGma.Event(Id, format, HDCAdEventType.Closed, AdUnitId, response));
            AfterShow(rewardEarned);
        }

        protected void OnShowFailed(HDCGmaFailure failure, HDCGmaResponse response)
        {
            if (!showing)
                return;

            Emit(HDCGma.Event(Id, format, HDCAdEventType.ShowFailed, AdUnitId, response).WithError(failure));
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
                OnPreloadFailed(new HDCGmaFailure(-1, "Preload failed"));
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
