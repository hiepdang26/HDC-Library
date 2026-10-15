using System;
using GoogleMobileAds.Api;
using HDC.Ads.Diagnostics;
using HDC.Ads.Domain;
using UnityEngine;

namespace HDC.Ads.Infrastructure
{
    internal sealed class HDCGmaBannerView
    {
        internal const float RecoveryBaseSeconds = 20f;
        internal const float RecoveryStepSeconds = 5f;
        internal const float RecoveryMaxSeconds = 40f;
        internal static readonly Vector2 MrecPoints = new Vector2(300f, 250f);
        private static readonly Vector2 BannerPoints = new Vector2(320f, 50f);

        private readonly string format;
        private BannerView view;
        private HDCGmaResponse response;
        private bool destroyed;
        private bool presented;
        private int recoveryFails;
        private int recoveryWatch;
        private HDCAdPosition? requestedPreset;
        private Vector2? requestedCenter;
        private HDCAdPosition? appliedPreset;
        private Vector2Int appliedPresetScreen;
        private Vector2Int? appliedPosition;

        internal HDCGmaBannerView(string id, string adUnitId, HDCBannerViewPlacement placement)
        {
            Id = id;
            AdUnitId = HDCTestAdUnits.PickBanner(placement, adUnitId);
            Placement = placement;
            format = placement == HDCBannerViewPlacement.Mrec ? HDCAdFormat.Mrec : HDCAdFormat.BannerView;
        }

        internal string Id { get; }
        internal string AdUnitId { get; }
        internal HDCBannerViewPlacement Placement { get; }
        internal bool IsLoaded { get; private set; }
        internal bool IsShowing { get; private set; }

        internal Vector2 SizeInPixels => view == null ? Vector2.zero : HDCNativeScreens.Current.ToPixels(SizePoints());

        internal static float RecoveryDelay(int fails) => Math.Min(RecoveryBaseSeconds + RecoveryStepSeconds * Math.Max(fails, 0), RecoveryMaxSeconds);

        internal void Load()
        {
            if (destroyed)
                return;
            if (view == null)
                Create();
            else if (!presented)
                Rebuild();
        }

        internal void Show()
        {
            if (view == null || IsShowing)
                return;

            IsShowing = true;
            if (IsLoaded)
                Present();
        }

        internal void Hide()
        {
            if (view == null || !IsShowing)
                return;

            IsShowing = false;
            if (!presented)
                return;
            presented = false;
            view.Hide();
            Emit(HDCAdEventType.Closed, response);
        }

        internal void Destroy()
        {
            destroyed = true;
            recoveryWatch++;
            HDCAdsTracker.Destroyed(format, Id);
            IsShowing = false;
            presented = false;
            IsLoaded = false;
            view?.Destroy();
            view = null;
        }

        internal void Move(HDCAdPosition position)
        {
            requestedPreset = position;
            requestedCenter = null;
            ApplyPreset(position);
        }

        internal void Move(Vector2 screenPoint)
        {
            requestedCenter = screenPoint;
            requestedPreset = null;
            ApplyCenter(screenPoint);
        }

        private void ApplyPreset(HDCAdPosition position)
        {
            if (view == null)
                return;

            var screen = new Vector2Int(Screen.width, Screen.height);
            if (appliedPreset == position && appliedPresetScreen == screen)
                return;
            appliedPreset = position;
            appliedPresetScreen = screen;
            appliedPosition = null;
            view.SetPosition(GmaPosition(position));
        }

        private void ApplyCenter(Vector2 screenPoint)
        {
            if (view == null)
                return;

            Vector2Int position = HDCNativeScreens.Current.CustomPosition(screenPoint, SizePoints());
            if (appliedPosition == position)
                return;
            appliedPosition = position;
            appliedPreset = null;
            view.SetPosition(position.x, position.y);
        }

        private void ApplyRequested()
        {
            appliedPreset = null;
            appliedPosition = null;
            if (requestedCenter.HasValue)
                ApplyCenter(requestedCenter.Value);
            else if (requestedPreset.HasValue)
                ApplyPreset(requestedPreset.Value);
        }

        private Vector2 SizePoints()
        {
            if (Placement == HDCBannerViewPlacement.Mrec)
                return MrecPoints;

            float scale = HDCAdsSdk.PixelsPerPoint;
            var size = new Vector2(view.GetWidthInPixels(), view.GetHeightInPixels()) / scale;
            return size.x > 0f && size.y > 0f ? size : BannerPoints;
        }

        private static AdPosition GmaPosition(HDCAdPosition position)
        {
            switch (position)
            {
                case HDCAdPosition.Top:
                    return AdPosition.Top;
                case HDCAdPosition.TopLeft:
                    return AdPosition.TopLeft;
                case HDCAdPosition.TopRight:
                    return AdPosition.TopRight;
                case HDCAdPosition.BottomLeft:
                    return AdPosition.BottomLeft;
                case HDCAdPosition.BottomRight:
                    return AdPosition.BottomRight;
                case HDCAdPosition.Center:
                    return AdPosition.Center;
                default:
                    return AdPosition.Bottom;
            }
        }

        private void Create()
        {
            view = new BannerView(AdUnitId, Size(), Position());
            ApplyRequested();
            view.Hide();
            Attach(view);
            Request();
        }

        private void Rebuild()
        {
            BannerView old = view;
            view = null;
            IsLoaded = false;
            response = null;
            old.Destroy();
            Create();
        }

        private void Request()
        {
            HDCAdsTracker.Requested(format, Id, AdUnitId);
            view.LoadAd(new AdRequest());
            WatchRecovery();
        }

        private void WatchRecovery()
        {
            int watch = ++recoveryWatch;
            HDCMainThread.PostDelayed(RecoveryDelay(recoveryFails), () =>
            {
                if (watch != recoveryWatch || destroyed || view == null || IsLoaded || presented)
                    return;
                Rebuild();
            });
        }

        private void Present()
        {
            presented = true;
            view.Show();
            Emit(HDCAdEventType.Shown, response);
        }

        private AdSize Size()
        {
            switch (Placement)
            {
                case HDCBannerViewPlacement.Mrec:
                    return AdSize.MediumRectangle;
                default:
                    return AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(AdSize.FullWidth);
            }
        }

        private AdPosition Position()
        {
            switch (Placement)
            {
                case HDCBannerViewPlacement.Mrec:
                    return AdPosition.BottomRight;
                default:
                    return AdPosition.Bottom;
            }
        }

        private void Attach(BannerView banner)
        {
            banner.OnBannerAdLoaded += () =>
            {
                HDCGmaResponse read = HDCGmaResponse.Read(banner.GetResponseInfo());
                HDCMainThread.Post(() => OnLoaded(banner, read));
            };
            banner.OnBannerAdLoadFailed += error =>
            {
                HDCGmaFailure failure = HDCGmaFailure.Read(error, "Load failed");
                HDCMainThread.Post(() => OnLoadFailed(banner, failure));
            };
            banner.OnAdImpressionRecorded += () => HDCMainThread.Post(() => OnEvent(banner, HDCAdEventType.Impression));
            banner.OnAdClicked += () => HDCMainThread.Post(() => OnEvent(banner, HDCAdEventType.Clicked));
            banner.OnAdPaid += value => HDCMainThread.Post(() =>
            {
                if (banner == view)
                    HDCAdsSdk.Emit(HDCGma.Event(Id, format, HDCAdEventType.Paid, AdUnitId, response).WithValue(value));
            });
        }

        private void OnLoaded(BannerView banner, HDCGmaResponse read)
        {
            if (banner != view)
                return;

            response = read;
            recoveryFails = 0;
            recoveryWatch++;
            IsLoaded = true;
            Emit(HDCAdEventType.Loaded, response);
            if (IsShowing && !presented)
                Present();
        }

        private void OnLoadFailed(BannerView banner, HDCGmaFailure failure)
        {
            if (banner != view)
                return;

            HDCAdsSdk.Emit(HDCGma.Event(Id, format, HDCAdEventType.LoadFailed, AdUnitId, null).WithError(failure));
            if (IsLoaded)
                return;
            recoveryFails++;
            WatchRecovery();
            HDCAdsTracker.RetryScheduled(format, Id, RecoveryDelay(recoveryFails), recoveryFails);
        }

        private void OnEvent(BannerView banner, string type)
        {
            if (banner == view)
                Emit(type, response);
        }

        private void Emit(string type, HDCGmaResponse info) => HDCAdsSdk.Emit(HDCGma.Event(Id, format, type, AdUnitId, info));
    }
}
