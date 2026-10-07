using System;
using HDC.Ads.Domain;
using HDC.Ads.Ports;

namespace HDC.Ads.Logic
{
    internal sealed class HDCCompanionShow
    {
        private readonly string leaderId;
        private readonly IShowWithLeader withLeader;
        private bool loading;
        private bool showWhenLoaded;
        private bool showing;
        private bool destroyWhenClosed;

        internal HDCCompanionShow(string leaderId, IFullscreenAd ad)
        {
            this.leaderId = leaderId;
            Ad = ad;
            withLeader = ad as IShowWithLeader;
            ad.Event += OnEvent;
        }

        internal IFullscreenAd Ad { get; }

        internal bool OnScreen { get; private set; }

        internal event Action OnScreenChanged;

        internal void BeforeLeaderShow()
        {
            if (Ad.IsReady)
                withLeader?.ShowWithLeader(leaderId);
        }

        internal void LeaderNotShown() => withLeader?.CancelShowWithLeader(leaderId);

        internal void LeaderShown()
        {
            if (withLeader != null && withLeader.ShownWithLeader(leaderId))
                return;
            if (Ad.IsReady)
            {
                ShowNow();
                return;
            }

            showWhenLoaded = true;
            if (loading)
                return;
            loading = true;
            Ad.Load();
        }

        internal void Destroy()
        {
            showWhenLoaded = false;
            if (showing)
            {
                destroyWhenClosed = true;
                return;
            }

            Ad.Event -= OnEvent;
            Ad.Destroy();
        }

        private void ShowNow()
        {
            SetOnScreen(true);
            Ad.Show();
        }

        private void OnEvent(HDCAdEvent adEvent)
        {
            switch (adEvent.type)
            {
                case HDCAdEventType.Loaded:
                    loading = false;
                    if (!showWhenLoaded)
                        break;
                    showWhenLoaded = false;
                    ShowNow();
                    break;
                case HDCAdEventType.LoadFailed:
                    loading = false;
                    showWhenLoaded = false;
                    break;
                case HDCAdEventType.Shown:
                    showing = true;
                    SetOnScreen(true);
                    break;
                case HDCAdEventType.ShowFailed:
                    showing = false;
                    SetOnScreen(false);
                    break;
                case HDCAdEventType.Closed:
                    showing = false;
                    loading = true;
                    SetOnScreen(false);
                    if (destroyWhenClosed)
                        Destroy();
                    break;
            }
        }

        private void SetOnScreen(bool value)
        {
            if (OnScreen == value)
                return;
            OnScreen = value;
            OnScreenChanged?.Invoke();
        }
    }
}
