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

        private readonly string format;
        private BannerView view;
        private ResponseInfo response;
        private bool destroyed;
        private bool presented;
        private int recoveryFails;
        private int recoveryWatch;

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

        internal Vector2 SizeInPixels => view == null ? Vector2.zero : new Vector2(view.GetWidthInPixels(), view.GetHeightInPixels());

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
            switch (position)
            {
                case HDCAdPosition.Top:
                    view?.SetPosition(AdPosition.Top);
                    break;
                case HDCAdPosition.TopLeft:
                    view?.SetPosition(AdPosition.TopLeft);
                    break;
                case HDCAdPosition.TopRight:
                    view?.SetPosition(AdPosition.TopRight);
                    break;
                case HDCAdPosition.BottomLeft:
                    view?.SetPosition(AdPosition.BottomLeft);
                    break;
                case HDCAdPosition.BottomRight:
                    view?.SetPosition(AdPosition.BottomRight);
                    break;
                case HDCAdPosition.Center:
                    view?.SetPosition(AdPosition.Center);
                    break;
                default:
                    view?.SetPosition(AdPosition.Bottom);
                    break;
            }
        }

        internal void Move(Vector2 screenPoint)
        {
            if (view == null)
                return;

            float scale = HDCAdsSdk.PixelsPerPoint;
            Vector2 size = new Vector2(view.GetWidthInPixels(), view.GetHeightInPixels()) / scale;
            if (size.x <= 0f || size.y <= 0f)
                size = Placement == HDCBannerViewPlacement.Mrec ? new Vector2(300f, 250f) : new Vector2(320f, 50f);

            Vector2Int position = CustomPosition(screenPoint, size, scale, Screen.height, CustomPositionOrigin());
            view.SetPosition(position.x, position.y);
        }

        internal static Vector2Int CustomPosition(Vector2 screenPoint, Vector2 sizePoints, float scale, float screenHeight, Vector2 originPixels)
        {
            float x = (screenPoint.x - originPixels.x) / scale - sizePoints.x / 2f;
            float y = (screenHeight - screenPoint.y - originPixels.y) / scale - sizePoints.y / 2f;
            return new Vector2Int(Mathf.RoundToInt(x), Mathf.RoundToInt(y));
        }

        internal static Vector2 CustomPositionOrigin()
        {
#if UNITY_IOS && !UNITY_EDITOR
            Rect safeArea = Screen.safeArea;
            return new Vector2(safeArea.xMin, Screen.height - safeArea.yMax);
#else
            return Vector2.zero;
#endif
        }

        private void Create()
        {
            view = new BannerView(AdUnitId, Size(), Position());
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
            banner.OnBannerAdLoaded += () => HDCMainThread.Post(() => OnLoaded(banner));
            banner.OnBannerAdLoadFailed += error => HDCMainThread.Post(() => OnLoadFailed(banner, error));
            banner.OnAdImpressionRecorded += () => HDCMainThread.Post(() => OnEvent(banner, HDCAdEventType.Impression));
            banner.OnAdClicked += () => HDCMainThread.Post(() => OnEvent(banner, HDCAdEventType.Clicked));
            banner.OnAdPaid += value => HDCMainThread.Post(() =>
            {
                if (banner == view)
                    HDCAdsSdk.Emit(HDCGma.Event(Id, format, HDCAdEventType.Paid, AdUnitId, response).WithValue(value));
            });
        }

        private void OnLoaded(BannerView banner)
        {
            if (banner != view)
                return;

            response = banner.GetResponseInfo();
            recoveryFails = 0;
            recoveryWatch++;
            IsLoaded = true;
            Emit(HDCAdEventType.Loaded, response);
            if (IsShowing && !presented)
                Present();
        }

        private void OnLoadFailed(BannerView banner, LoadAdError error)
        {
            if (banner != view)
                return;

            HDCAdsSdk.Emit(HDCGma.Event(Id, format, HDCAdEventType.LoadFailed, AdUnitId, null).WithError(error, "Load failed"));
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

        private void Emit(string type, ResponseInfo info) => HDCAdsSdk.Emit(HDCGma.Event(Id, format, type, AdUnitId, info));
    }
}
