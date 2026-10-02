using GoogleMobileAds.Api;
using HDC.Ads.Domain;
using HDC.Ads.Ports;
using UnityEngine;

namespace HDC.Ads.Infrastructure
{
    internal sealed class HDCNativePopupAd : IPopupAd
    {
        private readonly HDCPopupOptions options;

        internal HDCNativePopupAd(string id, HDCAdCoreConfig.NativeUnit unit, bool reloadAfterShow)
        {
            Id = id;
            AdUnitId = unit.id;
            options = new HDCPopupOptions
            {
                timeShow = unit.timeShow,
                timeReload = reloadAfterShow ? unit.reloadTime : 0,
            };
            if (!string.IsNullOrEmpty(unit.layout))
                options.layout = unit.layout;
        }

        public string Id { get; }

        public string AdUnitId { get; }

        public bool IsRequested { get; private set; }

        public bool IsPlaced { get; private set; }

        public bool IsReady => IsRequested && HDCAdsSdk.IsPopupReady(Id);

        public bool IsDisplayable => IsRequested && HDCAdsSdk.IsPopupDisplayable(Id);

        public string State => IsRequested ? HDCAdsSdk.GetPopupState(Id) : null;

        public void Load()
        {
            if (IsRequested)
                return;
            IsRequested = HDCAdsSdk.LoadPopup(Id, new[] { AdUnitId }, options);
        }

        public bool Reload()
        {
            if (IsRequested && HDCAdsSdk.GetPopupState(Id) == "Showing")
                return false;
            if (IsRequested)
                HDCAdsSdk.DestroyPopup(Id);
            IsRequested = false;
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
            options.width = screenRect.width / scale;
            options.height = screenRect.height / scale;
            IsPlaced = true;
            if (IsRequested)
                HDCAdsSdk.UpdatePopupPlacement(Id, options.x, options.y, options.width, options.height);
        }

        public bool Show() => HDCAdsSdk.ShowPopup(Id);

        public void Hide()
        {
            if (IsRequested)
                HDCAdsSdk.HidePopup(Id);
        }
    }
}
