using System;
using HDC.Ads.Domain;
using HDC.Ads.Infrastructure;
using UnityEngine;

namespace HDC.Ads.Logic
{
    /// <summary>One ad unit of a full-screen group. Its state comes from the SDK's events for its instance id.</summary>
    internal abstract class HDCFullscreenSource
    {
        private Action onDisplayed;
        private Action<bool> onClosed;
        private bool showing;
        private bool starting;
        private bool failedWhileStarting;
        private bool rewardEarned;
        private bool destroyed;

        protected HDCFullscreenSource(string id, string format)
        {
            Id = id;
            Format = format;
            HDCAdsSdk.AdEvent += OnAdEvent;
        }

        internal string Id { get; }
        internal string Format { get; }
        internal abstract string AdUnitId { get; }
        internal Action<HDCFullscreenSource> Failed { get; set; }
        internal abstract bool IsReady { get; }
        internal bool IsShowing => showing;

        /// <summary>Native full-screen ads served as rewarded ads reward the player when they close.</summary>
        internal bool RewardsOnClose { get; set; }

        internal abstract void Load();

        internal bool Show(Action displayed, Action<bool> closed)
        {
            if (showing || destroyed)
                return false;

            showing = true;
            starting = true;
            failedWhileStarting = false;
            rewardEarned = false;
            onDisplayed = displayed;
            onClosed = closed;
            bool started;
            try
            {
                started = StartShow();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                started = false;
            }
            finally
            {
                starting = false;
            }

            if (!started)
            {
                showing = false;
                onDisplayed = null;
                onClosed = null;
                return false;
            }

            if (failedWhileStarting)
                Finish(false);
            return true;
        }

        internal void Destroy()
        {
            if (destroyed)
                return;
            destroyed = true;
            HDCAdsSdk.AdEvent -= OnAdEvent;
            DestroyAd();
            if (showing)
                Finish(false);
        }

        protected abstract bool StartShow();
        protected abstract void DestroyAd();
        protected virtual void OnOwnEvent(HDCAdEvent adEvent) { }

        private void OnAdEvent(HDCAdEvent adEvent)
        {
            if (adEvent.id != Id || adEvent.format != Format)
                return;

            OnOwnEvent(adEvent);
            switch (adEvent.type)
            {
                case HDCAdEventType.LoadFailed:
                    Failed?.Invoke(this);
                    break;
                case HDCAdEventType.Shown:
                    if (showing)
                    {
                        Action displayed = onDisplayed;
                        onDisplayed = null;
                        displayed?.Invoke();
                    }

                    break;
                case HDCAdEventType.Rewarded:
                    rewardEarned = true;
                    break;
                case HDCAdEventType.Closed:
                    if (showing && !starting)
                        Finish(rewardEarned || RewardsOnClose);
                    break;
                case HDCAdEventType.ShowFailed:
                    if (starting)
                        failedWhileStarting = true;
                    else if (showing)
                        Finish(false);
                    Failed?.Invoke(this);
                    break;
            }
        }

        private void Finish(bool rewarded)
        {
            showing = false;
            onDisplayed = null;
            Action<bool> closed = onClosed;
            onClosed = null;
            try
            {
                closed?.Invoke(rewarded);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }
    }
}
