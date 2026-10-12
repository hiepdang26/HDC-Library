using System;
using System.Collections;
using HDC.Ads;
using HDC.Ads.Domain;
using HDC.Ads.Infrastructure;
using HDC.Ads.Ports;
using UnityEngine;

namespace HDC.Onboarding
{
    internal sealed class HDCSplashLaunchAd
    {
        private readonly float timeOut;
        private readonly HDCLayoutPicker layouts;
        private readonly string layoutGroup;
        private bool destroyed;

        internal HDCSplashLaunchAd(string position, HDCSplashLaunchChannel launch, float timeOut)
        {
            Position = position;
            this.timeOut = timeOut;
            launch = launch ?? new HDCSplashLaunchChannel();
            if (!launch.IsKnownType)
                HDCOnboardingLog.Error($"splash_config.launchChannel.launchAdType {launch.launchAdType} is not 0 (inter), 1 (native) or 2 (app open): no launch ad.");
            Type = launch.AdType;
            AdUnitId = launch.AdUnitId;
            switch (Type)
            {
                case HDCFullscreenAdType.Native:
                    layoutGroup = launch.LayoutGroup;
                    layouts = new HDCLayoutPicker(HDCOnboardingAds.Context.CoreConfig, layoutGroup);
                    Ad = new HDCNativeFullscreenAd(HDCOnboardingAds.SplashLaunchNativeId, AdUnitId, false, layouts);
                    break;
                case HDCFullscreenAdType.AppOpen:
                    Ad = new HDCGmaFullscreenAdapter(HDCAdFormat.AppOpen, new HDCGmaAppOpenAd(HDCOnboardingAds.SplashLaunchAppOpenId, AdUnitId, false, 0));
                    break;
                default:
                    Ad = new HDCNativeInterstitialAd(HDCOnboardingAds.SplashInterstitialId, AdUnitId, 1, false);
                    break;
            }

            Ad.Event += OnAdEvent;
            HDCOnboardingAds.RecordSplash(Ad.Id);
        }

        internal event Action<HDCSplashAdStatus> StatusChanged;

        internal IFullscreenAd Ad { get; }

        internal HDCFullscreenAdType Type { get; }

        internal string AdUnitId { get; }

        internal bool IsInterstitial => Type == HDCFullscreenAdType.Interstitial;

        internal string Position { get; }

        internal HDCSplashAdStatus Status { get; private set; }

        internal bool IsReady => Status == HDCSplashAdStatus.Loaded;

        internal bool IsOnScreen => Status == HDCSplashAdStatus.Loaded || Status == HDCSplashAdStatus.AdDisplayed
            || Status == HDCSplashAdStatus.AdPaidImpression;

        private bool IsNative => Type == HDCFullscreenAdType.Native;

        private string Label => Type == HDCFullscreenAdType.Native ? "Native launch splash"
            : Type == HDCFullscreenAdType.AppOpen ? "App open splash"
            : "Interstitial splash";

        internal IEnumerator Load()
        {
            Status = HDCSplashAdStatus.Loading;
            Ad.Load();
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

        internal IEnumerator Show()
        {
            HDCOnboardingAds.Context.NotifyFullscreenOpening();
            if (IsNative && !layouts.HasLayouts)
            {
                HDCOnboardingLog.Error($"{Label}: no layout for layoutGroupName '{layoutGroup}' in forceAdLayoutConfig.");
                ChangeStatus(HDCSplashAdStatus.AdShowFailed);
            }
            else if (!Ad.Show())
            {
                ChangeStatus(HDCSplashAdStatus.AdShowFailed);
            }

            while (Status != HDCSplashAdStatus.AdClosed && Status != HDCSplashAdStatus.AdShowFailed)
                yield return null;
        }

        internal void Destroy()
        {
            if (destroyed)
                return;
            destroyed = true;
            Ad.Event -= OnAdEvent;
            Ad.Destroy();
        }

        private void OnAdEvent(HDCAdEvent adEvent)
        {
            switch (adEvent.type)
            {
                case HDCAdEventType.Loaded:
                    HDCOnboardingTracking.Track($"5_{Position}_l_s_loaded");
                    HDCOnboardingLog.Log($"{Label} loaded");
                    ChangeStatus(HDCSplashAdStatus.Loaded);
                    break;
                case HDCAdEventType.Shown:
                    HDCOnboardingTracking.Track($"5_{Position}_s_s_displayed");
                    SetOverlay(true);
                    ChangeStatus(HDCSplashAdStatus.AdDisplayed);
                    break;
                case HDCAdEventType.Paid:
                    HDCOnboardingTracking.Track($"5_{Position}_s_s_paid");
                    ChangeStatus(HDCSplashAdStatus.AdPaidImpression);
                    break;
                case HDCAdEventType.Closed:
                    HDCOnboardingTracking.Track($"5_{Position}_s_s_closed");
                    SetOverlay(false);
                    ChangeStatus(HDCSplashAdStatus.AdClosed);
                    break;
                case HDCAdEventType.LoadFailed:
                    HDCOnboardingTracking.Track($"5_{Position}_l_s_failed");
                    HDCOnboardingLog.Warn($"{Label} load failed: code={adEvent.code} {adEvent.message}");
                    ChangeStatus(HDCSplashAdStatus.LoadFailed);
                    Destroy();
                    break;
                case HDCAdEventType.ShowFailed:
                    HDCOnboardingLog.Warn($"{Label} show failed: code={adEvent.code} {adEvent.message}");
                    SetOverlay(false);
                    ChangeStatus(HDCSplashAdStatus.AdShowFailed);
                    break;
            }
        }

        private void SetOverlay(bool onScreen)
        {
            if (IsNative)
                HDCOnboardingAds.Context.SetOverlayAd(Ad.Id, onScreen);
        }

        private void ChangeStatus(HDCSplashAdStatus status)
        {
            Status = status;
            StatusChanged?.Invoke(status);
        }
    }
}
