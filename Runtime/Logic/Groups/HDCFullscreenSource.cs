using System;
using HDC.Ads.Domain;
using HDC.Ads.Ports;
using UnityEngine;

namespace HDC.Ads.Logic
{
    internal sealed class HDCFullscreenSource
    {
        private readonly IFullscreenAd ad;
        private readonly HDCCompanionShow companion;
        private Action onDisplayed;
        private Action<bool> onClosed;
        private bool showing;
        private bool starting;
        private bool failedWhileStarting;
        private bool rewardEarned;
        private bool destroyed;

        internal HDCFullscreenSource(IFullscreenAd ad, IAdNetwork network)
        {
            this.ad = ad;
            Network = network;
            ad.Event += OnAdEvent;
            if (ad is IFullscreenCompanion withCompanion && withCompanion.Companion != null)
            {
                companion = new HDCCompanionShow(ad.Id, withCompanion.Companion);
                companion.OnScreenChanged += () => CompanionShowingChanged?.Invoke(this);
            }
        }

        internal IAdNetwork Network { get; }

        internal IFullscreenAd Companion => companion?.Ad;

        internal bool CompanionShowing => companion?.OnScreen ?? false;

        internal event Action<HDCFullscreenSource> CompanionShowingChanged;

        internal string Id => ad.Id;
        internal string Format => ad.Format;
        internal string AdUnitId => ad.AdUnitId;
        internal Action<HDCFullscreenSource> Failed { get; set; }
        internal bool IsReady => ad.IsReady;
        internal bool IsShowing => showing;

        internal bool RewardsOnClose { get; set; }

        internal void Load() => ad.Load();

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
                companion?.BeforeLeaderShow();
                started = ad.Show();
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
                companion?.LeaderNotShown();
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
            ad.Event -= OnAdEvent;
            companion?.LeaderNotShown();
            companion?.Destroy();
            ad.Destroy();
            if (showing)
                Finish(false);
        }

        private void OnAdEvent(HDCAdEvent adEvent)
        {
            switch (adEvent.type)
            {
                case HDCAdEventType.LoadFailed:
                    Failed?.Invoke(this);
                    break;
                case HDCAdEventType.Shown:
                    companion?.LeaderShown();
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
                    companion?.LeaderNotShown();
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
