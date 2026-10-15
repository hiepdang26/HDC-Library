using GoogleMobileAds.Api;
using HDC.Ads.Domain;
using HDC.Ads.Ports;
using UnityEngine;

namespace HDC.Ads.Infrastructure
{
    internal sealed class HDCNativePopupAd : HDCSdkAd, IPopupAd
    {
        private const string LoadingState = "Loading";
        private const string LoadedState = "Loaded";
        private const string DisplayableState = "Displayable";
        private const string ShowingState = "Showing";
        private const string ClosedState = "Closed";
        private const string FailedState = "Failed";
        private const float EdgePixels = 1f;

        internal const float WiderThanAnyScreen = 100000f;

        private readonly HDCPopupOptions options;
        private bool loading;
        private bool showWhenLoaded;

        internal HDCNativePopupAd(string id, HDCAdCoreConfig.NativeUnit unit, bool reloadAfterShow)
            : base(id, HDCAdFormat.Popup)
        {
            AdUnitId = unit.id;
            options = new HDCPopupOptions
            {
                layout = HDCAdLayouts.Popup(unit.layout),
                timeShow = unit.timeShow,
                timeReload = reloadAfterShow ? unit.reloadTime : 0,
            };
        }

        public string AdUnitId { get; }

        public bool IsRequested { get; private set; }

        public bool IsPlaced { get; private set; }

        public bool IsReady => IsRequested && HDCAdsSdk.IsPopupReady(Id);

        public bool IsDisplayable => IsRequested && HDCAdsSdk.IsPopupDisplayable(Id);

        public string State => IsRequested ? HDCAdsSdk.GetPopupState(Id) : null;

        public void Load()
        {
            if (IsRequested && (loading || !IsSpent()))
                return;
            IsRequested = HDCAdsSdk.LoadPopup(Id, new[] { AdUnitId }, options);
            loading = IsRequested;
        }

        public bool Reload()
        {
            if (!IsRequested)
            {
                Load();
                return IsRequested;
            }

            string state = HDCAdsSdk.GetPopupState(Id);
            if (state == ShowingState)
                return false;
            if (loading || state == LoadingState || state == LoadedState || state == DisplayableState)
                return true;
            HDCAdsSdk.DestroyPopup(Id);
            IsRequested = false;
            loading = false;
            Load();
            return IsRequested;
        }

        public void Place(Rect screenRect)
        {
            if (Screen.width <= 0 || Screen.height <= 0 || screenRect.width <= 0f || screenRect.height <= 0f)
                return;

            float scale = MobileAds.Utils.GetDeviceScale();
            if (scale <= 0f)
                scale = 1f;
            options.x = Mathf.Clamp01(screenRect.center.x / Screen.width);
            options.y = Mathf.Clamp01(screenRect.center.y / Screen.height);
            options.width = WidthOf(screenRect, Screen.width, scale);
            options.height = screenRect.height / scale;
            IsPlaced = true;
            if (IsRequested)
                HDCAdsSdk.UpdatePopupPlacement(Id, options.x, options.y, options.width, options.height);
        }

        internal static float WidthOf(Rect screenRect, float screenWidth, float scale) =>
            SpansScreenWidth(screenRect, screenWidth) ? WiderThanAnyScreen : screenRect.width / scale;

        internal static bool SpansScreenWidth(Rect screenRect, float screenWidth) =>
            screenRect.xMin <= EdgePixels && screenRect.xMax >= screenWidth - EdgePixels;

        public bool Show()
        {
            if (!IsRequested)
                return HDCAdsSdk.ShowPopup(Id);
            if (loading || HDCAdsSdk.GetPopupState(Id) == LoadingState)
            {
                showWhenLoaded = true;
                return true;
            }

            return HDCAdsSdk.ShowPopup(Id);
        }

        public void Hide()
        {
            showWhenLoaded = false;
            if (IsRequested)
                HDCAdsSdk.HidePopup(Id);
        }

        protected override void OnOwnEvent(HDCAdEvent adEvent)
        {
            if (adEvent.type == HDCAdEventType.Loaded)
            {
                loading = false;
                if (!showWhenLoaded)
                    return;
                showWhenLoaded = false;
                HDCAdsSdk.ShowPopup(Id);
            }
            else if (adEvent.type == HDCAdEventType.LoadFailed)
            {
                loading = false;
                showWhenLoaded = false;
            }
        }

        private bool IsSpent()
        {
            string state = HDCAdsSdk.GetPopupState(Id);
            return state == FailedState || (state == ClosedState && !HDCAdsSdk.IsPopupDisplayable(Id));
        }
    }
}
