using System;
using System.Collections.Generic;
using HDC.Ads.Domain;
using UnityEngine;

namespace HDC.Ads.Infrastructure
{
    internal static partial class HDCAdsSdk
    {
        private static readonly Dictionary<string, HDCGmaFullscreenAd> rewardedAds = new Dictionary<string, HDCGmaFullscreenAd>();
        private static readonly Dictionary<string, HDCGmaFullscreenAd> appOpenAds = new Dictionary<string, HDCGmaFullscreenAd>();
        private static readonly Dictionary<string, HDCGmaBannerView> bannerViews = new Dictionary<string, HDCGmaBannerView>();

        public static void EnableTestDevice()
        {
            HDCGma.EnableTestDevice();
            IsTestDevice = true;
        }

        public static bool IsTestDevice { get; private set; }

        public static bool UseTestAdUnits { get; set; }

        public static bool LoadRewarded(string id, string adUnitId, bool preload = false, int bufferSize = 0) =>
            LoadPluginAd(rewardedAds, "LoadRewarded", id, HDCTestAdUnits.Pick(HDCAdFormat.Rewarded, adUnitId), preload, bufferSize,
                () => new HDCGmaRewardedAd(id, adUnitId, preload, bufferSize));

        public static bool ShowRewarded(string id, Action<bool> onClosed = null) =>
            ShowPluginAd(rewardedAds, HDCAdFormat.Rewarded, id, onClosed);

        public static bool IsRewardedReady(string id) => rewardedAds.TryGetValue(id ?? string.Empty, out HDCGmaFullscreenAd ad) && ad.IsReady;

        public static void DestroyRewarded(string id) => DestroyPluginAd(rewardedAds, id);

        public static bool LoadAppOpen(string id, string adUnitId, bool preload = false, int bufferSize = 0) =>
            LoadPluginAd(appOpenAds, "LoadAppOpen", id, HDCTestAdUnits.Pick(HDCAdFormat.AppOpen, adUnitId), preload, bufferSize,
                () => new HDCGmaAppOpenAd(id, adUnitId, preload, bufferSize));

        public static bool ShowAppOpen(string id, Action onClosed = null) =>
            ShowPluginAd(appOpenAds, HDCAdFormat.AppOpen, id, onClosed == null ? (Action<bool>)null : _ => onClosed());

        public static bool IsAppOpenReady(string id) => appOpenAds.TryGetValue(id ?? string.Empty, out HDCGmaFullscreenAd ad) && ad.IsReady;

        public static void DestroyAppOpen(string id) => DestroyPluginAd(appOpenAds, id);

        public static bool LoadBannerView(string id, string adUnitId, HDCBannerViewPlacement placement)
        {
            adUnitId = HDCTestAdUnits.PickBanner(placement, adUnitId);
            if (!CheckPluginArgs("LoadBannerView", id, adUnitId))
                return false;

            if (bannerViews.TryGetValue(id, out HDCGmaBannerView view) && (view.AdUnitId != adUnitId || view.Placement != placement))
            {
                view.Destroy();
                view = null;
            }

            if (view == null)
                bannerViews[id] = view = new HDCGmaBannerView(id, adUnitId, placement);
            view.Load();
            return true;
        }

        public static void ShowBannerView(string id) => WithBannerView(id, view => view.Show());

        public static void HideBannerView(string id) => WithBannerView(id, view => view.Hide());

        public static Vector2 GetBannerViewSizeInPixels(string id) =>
            bannerViews.TryGetValue(id ?? string.Empty, out HDCGmaBannerView view) ? view.SizeInPixels : Vector2.zero;

        public static bool IsBannerViewLoaded(string id) => bannerViews.TryGetValue(id ?? string.Empty, out HDCGmaBannerView view) && view.IsLoaded;

        public static void MoveBannerView(string id, HDCAdPosition position) => WithBannerView(id, view => view.Move(position));

        public static void MoveBannerView(string id, Vector2 screenPoint) => WithBannerView(id, view => view.Move(screenPoint));

        public static void DestroyBannerView(string id)
        {
            if (!bannerViews.TryGetValue(id ?? string.Empty, out HDCGmaBannerView view))
                return;
            view.Destroy();
            bannerViews.Remove(id);
        }

        private static bool LoadPluginAd(Dictionary<string, HDCGmaFullscreenAd> ads, string method, string id, string adUnitId,
            bool preload, int bufferSize, Func<HDCGmaFullscreenAd> create)
        {
            if (!CheckPluginArgs(method, id, adUnitId))
                return false;

            if (ads.TryGetValue(id, out HDCGmaFullscreenAd ad) && (ad.AdUnitId != adUnitId || ad.Preload != preload
                || ad.BufferSize != HDCGmaFullscreenAd.PreloadBufferSize(bufferSize)))
            {
                ad.Destroy();
                ad = null;
            }

            if (ad == null)
                ads[id] = ad = create();
            ad.Load();
            return true;
        }

        private static bool ShowPluginAd(Dictionary<string, HDCGmaFullscreenAd> ads, string format, string id, Action<bool> onClosed)
        {
            if (DebugLog)
                Debug.Log($"{LogTag} show {format}/{id}");
            if (ads.TryGetValue(id ?? string.Empty, out HDCGmaFullscreenAd ad))
                return ad.Show(onClosed);

            Emit(new HDCAdEvent { id = id, format = format, type = HDCAdEventType.ShowFailed, code = -1, message = "Not loaded. Load first" });
            return false;
        }

        private static void DestroyPluginAd(Dictionary<string, HDCGmaFullscreenAd> ads, string id)
        {
            if (!ads.TryGetValue(id ?? string.Empty, out HDCGmaFullscreenAd ad))
                return;
            ad.Destroy();
            ads.Remove(id);
        }

        private static void WithBannerView(string id, Action<HDCGmaBannerView> action)
        {
            if (bannerViews.TryGetValue(id ?? string.Empty, out HDCGmaBannerView view))
                action(view);
            else
                Debug.LogWarning($"{LogTag} no banner view {id}. Load it first");
        }

        private static bool CheckPluginArgs(string method, string id, string adUnitId)
        {
            HDCMainThread.EnsureCreated();
            if (DebugLog)
                Debug.Log($"{LogTag} {method} {id} {adUnitId}");
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(adUnitId))
            {
                Debug.LogWarning($"{LogTag} {method} failed: id and adUnitId are required");
                return false;
            }

            return true;
        }
    }
}
