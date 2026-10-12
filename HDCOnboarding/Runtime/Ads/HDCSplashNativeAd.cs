using System;
using System.Collections;
using HDC.Ads.Domain;
using HDC.Ads.Infrastructure;

namespace HDC.Onboarding
{
    internal sealed class HDCSplashNativeAd
    {
        private readonly bool tracking;
        private readonly HDCLayoutPicker layouts;
        private string armedLeader;

        internal HDCSplashNativeAd(string position, string adUnitId, string layoutGroup, bool tracking = true)
        {
            Position = position;
            this.tracking = tracking;
            layouts = new HDCLayoutPicker(HDCOnboardingAds.Context.CoreConfig, layoutGroup);
            Ad = new HDCNativeFullscreenAd(HDCOnboardingAds.SplashNativeAfterId, adUnitId ?? "", false, layouts);
            Ad.Event += OnAdEvent;
            HDCOnboardingAds.RecordSplash(Ad.Id);
        }

        internal event Action<HDCSplashAdStatus> StatusChanged;

        internal HDCNativeFullscreenAd Ad { get; }

        internal string Position { get; }

        internal HDCSplashAdStatus Status { get; private set; }

        internal bool IsReady => Status == HDCSplashAdStatus.Loaded;

        internal bool HasLayouts => layouts.HasLayouts;

        internal IEnumerator Load()
        {
            ChangeStatus(HDCSplashAdStatus.Loading);
            Ad.Load();
            while (Status == HDCSplashAdStatus.Loading)
                yield return null;
        }

        internal void ArmUnder(string leaderId)
        {
            if (!HasLayouts)
                return;
            if (IsReady)
                Ad.ShowWithLeader(leaderId);
            else if (Status == HDCSplashAdStatus.Loading)
                Ad.ShowWithLeaderOnceLoaded(leaderId);
            else
                return;
            armedLeader = leaderId;
        }

        internal IEnumerator Show(Func<bool> leaderOnScreen)
        {
            if (!HasLayouts)
            {
                HDCOnboardingLog.Error("Cant get layout config");
                yield break;
            }

            while (leaderOnScreen != null && leaderOnScreen())
                yield return null;
            if (armedLeader != null)
            {
                string leader = armedLeader;
                armedLeader = null;
                if (Ad.ShownWithLeader(leader))
                {
                    yield return WaitClosed();
                    yield break;
                }

                Ad.CancelShowWithLeader(leader);
            }

            HDCOnboardingAds.Context.NotifyFullscreenOpening();
            if (!Ad.Show())
                ChangeStatus(HDCSplashAdStatus.AdShowFailed);
            yield return WaitClosed();
        }

        private IEnumerator WaitClosed()
        {
            while (Status != HDCSplashAdStatus.AdClosed && Status != HDCSplashAdStatus.AdShowFailed)
                yield return null;
        }

        private void OnAdEvent(HDCAdEvent adEvent)
        {
            switch (adEvent.type)
            {
                case HDCAdEventType.Loaded:
                    Track("l_s_loaded");
                    ChangeStatus(HDCSplashAdStatus.Loaded);
                    break;
                case HDCAdEventType.Closed:
                    Track("s_s_closed");
                    HDCOnboardingAds.Context.SetOverlayAd(Ad.Id, false);
                    ChangeStatus(HDCSplashAdStatus.AdClosed);
                    break;
                case HDCAdEventType.Shown:
                    Track("s_s_displayed");
                    HDCOnboardingAds.Context.SetOverlayAd(Ad.Id, true);
                    ChangeStatus(HDCSplashAdStatus.AdDisplayed);
                    break;
                case HDCAdEventType.Paid:
                    Track("s_s_paid");
                    ChangeStatus(HDCSplashAdStatus.AdPaidImpression);
                    break;
                case HDCAdEventType.LoadFailed:
                    Track("l_s_failed");
                    HDCOnboardingLog.Warn($"Native after interstitials load failed: code={adEvent.code} {adEvent.message}");
                    ChangeStatus(HDCSplashAdStatus.LoadFailed);
                    break;
                case HDCAdEventType.ShowFailed:
                    HDCOnboardingLog.Warn($"Native after interstitials show failed: code={adEvent.code} {adEvent.message}");
                    HDCOnboardingAds.Context.SetOverlayAd(Ad.Id, false);
                    ChangeStatus(HDCSplashAdStatus.AdShowFailed);
                    break;
            }
        }

        private void Track(string step)
        {
            if (tracking)
                HDCOnboardingTracking.Track($"5_{Position}_{step}");
        }

        private void ChangeStatus(HDCSplashAdStatus status)
        {
            Status = status;
            StatusChanged?.Invoke(status);
        }
    }
}
