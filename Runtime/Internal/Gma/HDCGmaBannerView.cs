using System;
using GoogleMobileAds.Api;
using UnityEngine;

namespace HDC.Ads.Internal
{
    /// <summary>
    /// A banner or MREC view from the Google Mobile Ads Unity plugin. The view is created hidden and refreshes
    /// itself once loaded; until the first ad loads, failed loads are retried with a growing delay.
    /// </summary>
    internal sealed class HDCGmaBannerView
    {
        private readonly string format;
        private readonly HDCRetry retry = new HDCRetry();
        private BannerView view;
        private ResponseInfo response;
        private bool destroyed;

        internal HDCGmaBannerView(string id, string adUnitId, HDCBannerViewPlacement placement)
        {
            Id = id;
            AdUnitId = adUnitId;
            Placement = placement;
            format = placement == HDCBannerViewPlacement.Mrec ? HDCAdFormat.Mrec : HDCAdFormat.BannerView;
        }

        internal string Id { get; }
        internal string AdUnitId { get; }
        internal HDCBannerViewPlacement Placement { get; }
        internal bool IsLoaded { get; private set; }
        internal bool IsShowing { get; private set; }

        internal Vector2 SizeInPixels => view == null ? Vector2.zero : new Vector2(view.GetWidthInPixels(), view.GetHeightInPixels());

        internal void Load()
        {
            if (view != null || destroyed)
                return;

            view = new BannerView(AdUnitId, Size(), Position());
            // A new view shows itself; keep it hidden until the game asks for it.
            view.Hide();
            Attach(view);
            view.LoadAd(new AdRequest());
        }

        /// <summary>Shows the view now, or as soon as its first ad loads.</summary>
        internal void Show()
        {
            if (view == null || IsShowing)
                return;

            IsShowing = true;
            view.Show();
            if (IsLoaded)
                Emit(HDCAdEventType.Shown, response);
        }

        internal void Hide()
        {
            if (view == null || !IsShowing)
                return;

            IsShowing = false;
            view.Hide();
            Emit(HDCAdEventType.Closed, response);
        }

        internal void Destroy()
        {
            destroyed = true;
            retry.Reset();
            IsShowing = false;
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

        /// <summary>Centers the view on a point in Unity screen pixels (origin at the bottom left).</summary>
        internal void Move(Vector2 screenPoint)
        {
            if (view == null)
                return;

            // The plugin places views in density-independent points from the top left.
            float scale = MobileAds.Utils.GetDeviceScale();
            if (scale <= 0f)
                scale = 1f;
            Vector2 size = new Vector2(view.GetWidthInPixels(), view.GetHeightInPixels()) / scale;
            if (size.x <= 0f || size.y <= 0f)
                size = Placement == HDCBannerViewPlacement.Mrec ? new Vector2(300f, 250f) : new Vector2(320f, 50f);

            float x = screenPoint.x / scale - size.x / 2f;
            float y = (Screen.height - screenPoint.y) / scale - size.y / 2f;
            view.SetPosition(Mathf.RoundToInt(x), Mathf.RoundToInt(y));
        }

        private AdSize Size()
        {
            switch (Placement)
            {
                case HDCBannerViewPlacement.Mrec:
                    return AdSize.MediumRectangle;
                case HDCBannerViewPlacement.FullBottom:
                case HDCBannerViewPlacement.FullTop:
                    return AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(AdSize.FullWidth);
                default:
                    return AdSize.Banner;
            }
        }

        private AdPosition Position()
        {
            switch (Placement)
            {
                case HDCBannerViewPlacement.FullTop:
                    return AdPosition.Top;
                case HDCBannerViewPlacement.TopLeft:
                    return AdPosition.TopLeft;
                case HDCBannerViewPlacement.TopRight:
                    return AdPosition.TopRight;
                case HDCBannerViewPlacement.BottomLeft:
                    return AdPosition.BottomLeft;
                case HDCBannerViewPlacement.BottomRight:
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

            // Every refresh loads a new ad, with its own response.
            response = banner.GetResponseInfo();
            retry.Reset();
            bool firstAd = !IsLoaded;
            IsLoaded = true;
            Emit(HDCAdEventType.Loaded, response);
            if (firstAd && IsShowing)
                Emit(HDCAdEventType.Shown, response);
        }

        private void OnLoadFailed(BannerView banner, LoadAdError error)
        {
            if (banner != view)
                return;

            HDCAdsSdk.Emit(HDCGma.Event(Id, format, HDCAdEventType.LoadFailed, AdUnitId, null).WithError(error, "Load failed"));
            // After the first ad, the view keeps its ad and refreshes on its own schedule.
            if (!IsLoaded)
                retry.Schedule(() => view?.LoadAd(new AdRequest()));
        }

        private void OnEvent(BannerView banner, string type)
        {
            if (banner == view)
                Emit(type, response);
        }

        private void Emit(string type, ResponseInfo info) => HDCAdsSdk.Emit(HDCGma.Event(Id, format, type, AdUnitId, info));
    }
}
