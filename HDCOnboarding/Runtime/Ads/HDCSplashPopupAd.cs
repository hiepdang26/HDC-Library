using System;
using System.Collections;
using HDC.Ads;
using HDC.Ads.Domain;
using HDC.Ads.Infrastructure;
using UnityEngine;

namespace HDC.Onboarding
{
    internal sealed class HDCSplashPopupAd
    {
        private readonly string instanceId;
        private readonly float timeOut;
        private readonly float timeShowing;
        private readonly bool tracking;
        private bool firstTimeCallLoad = true;
        private int paidImpressions;
        private int shownCount;
        private int closedCount;
        private int failedCount;

        internal HDCSplashPopupAd(string groupName, string position, float timeOut, float timeShowing, bool tracking = true)
        {
            GroupName = groupName ?? "";
            Position = position;
            this.timeOut = timeOut;
            this.timeShowing = timeShowing;
            this.tracking = tracking;
            instanceId = HDCAdNames.NativePopup(GroupName);
            HDCAdsSdk.AdEvent += OnAdEvent;
        }

        internal event Action<HDCSplashAdStatus> StatusChanged;

        internal string GroupName { get; }

        internal string Position { get; }

        internal HDCSplashAdStatus Status { get; private set; }

        internal string AdSourceAdapterClassName { get; private set; } = "";

        internal bool IsReady => Status == HDCSplashAdStatus.Loaded;

        internal IEnumerator Load()
        {
            ChangeStatus(HDCSplashAdStatus.Loading);
            if (firstTimeCallLoad)
            {
                HDCAds.Popup.Initialize(GroupName);
                firstTimeCallLoad = false;
            }
            else
            {
                HDCAds.Popup.Reinitialize(GroupName);
            }

            if (Status == HDCSplashAdStatus.Loading && HDCAds.Popup.IsGroupReady(GroupName))
                ChangeStatus(HDCSplashAdStatus.Loaded);
            float waited = 0f;
            while (Status == HDCSplashAdStatus.Loading && waited < timeOut)
            {
                yield return null;
                waited += Time.deltaTime;
            }

            if (Status != HDCSplashAdStatus.Loading)
                yield break;
            HDCOnboardingTracking.Track($"5_{Position}_l_s_timeout");
            ChangeStatus(HDCSplashAdStatus.LoadTimeOut);
        }

        internal IEnumerator Show(RectTransform area)
        {
            int paid = paidImpressions;
            int shown = shownCount;
            int closed = closedCount;
            int failed = failedCount;
            if (!HDCOnboardingAds.ShowPopup(GroupName, area))
            {
                HDCOnboardingLog.Warn($"Popup {GroupName} did not show");
                yield break;
            }

            bool waitForPaid = true;
            bool skipped = false;
            bool shownAfterSkip = false;
            float waited = 0f;
            float sinceShow = 0f;
            while (waited < timeShowing || (waitForPaid && paidImpressions == paid))
            {
                yield return null;
                waited += Time.deltaTime;
                if (skipped && !shownAfterSkip && shownCount != shown)
                {
                    shownAfterSkip = true;
                    waited = 0f;
                }

                if (!waitForPaid || paidImpressions != paid)
                    continue;
                if (failedCount != failed || (shownCount != shown && closedCount != closed))
                {
                    waitForPaid = false;
                    continue;
                }

                if (shownCount != shown)
                    continue;
                if (!skipped)
                {
                    if (closedCount == closed)
                        continue;
                    skipped = true;
                    closed = closedCount;
                    HDCOnboardingLog.Log($"Popup {GroupName} closed before it showed: showing it again until it does");
                }

                sinceShow += Time.unscaledDeltaTime;
                if (sinceShow < HDCOnboardingAds.PopupRetrySeconds)
                    continue;
                sinceShow = 0f;
                if (!HDCAds.Popup.CanShow(GroupName))
                {
                    waitForPaid = false;
                    continue;
                }

                HDCOnboardingAds.ShowPopup(GroupName, area);
            }
        }

        internal void Release() => HDCAdsSdk.AdEvent -= OnAdEvent;

        private void OnAdEvent(HDCAdEvent adEvent)
        {
            if (adEvent == null || adEvent.id != instanceId || adEvent.format != HDCAdFormat.Popup)
                return;
            switch (adEvent.type)
            {
                case HDCAdEventType.LoadFailed:
                    Track("l_s_failed");
                    HDCOnboardingLog.Warn($"Popup {GroupName} load failed: code={adEvent.code} {adEvent.message}");
                    ChangeStatus(HDCSplashAdStatus.LoadFailed);
                    break;
                case HDCAdEventType.Loaded:
                    Track("l_s_loaded");
                    AdSourceAdapterClassName = !string.IsNullOrEmpty(adEvent.adSource) ? adEvent.adSource : adEvent.adapter ?? "";
                    ChangeStatus(HDCSplashAdStatus.Loaded);
                    break;
                case HDCAdEventType.Paid:
                    Track("s_s_paid");
                    paidImpressions++;
                    ChangeStatus(HDCSplashAdStatus.AdPaidImpression);
                    break;
                case HDCAdEventType.Shown:
                    Track("s_s_displayed");
                    shownCount++;
                    ChangeStatus(HDCSplashAdStatus.AdDisplayed);
                    break;
                case HDCAdEventType.Closed:
                    closedCount++;
                    break;
                case HDCAdEventType.ShowFailed:
                    HDCOnboardingLog.Warn($"Popup {GroupName} show failed: code={adEvent.code} {adEvent.message}");
                    failedCount++;
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
