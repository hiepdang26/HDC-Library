using System;
using System.Collections.Generic;
using HDC.Ads.Internal;
using UnityEngine;

namespace HDC.Ads
{
    // Formats served through the Google Mobile Ads Unity plugin: rewarded, app open, banner and MREC views.
    internal static partial class HDCAdsSdk
    {
        private static readonly Dictionary<string, HDCGmaFullscreenAd> rewardedAds = new Dictionary<string, HDCGmaFullscreenAd>();
        private static readonly Dictionary<string, HDCGmaFullscreenAd> appOpenAds = new Dictionary<string, HDCGmaFullscreenAd>();
        private static readonly Dictionary<string, HDCGmaBannerView> bannerViews = new Dictionary<string, HDCGmaBannerView>();

        /// <summary>
        /// Serves Google test ads to this device, in every format including the native ones. Call it before
        /// ads load: ads loaded earlier are live ads.
        /// </summary>
        public static void EnableTestDevice()
        {
            HDCGma.EnableTestDevice();
            IsTestDevice = true;
        }

        /// <summary>True once <see cref="EnableTestDevice"/> made this device a Google test device.</summary>
        public static bool IsTestDevice { get; private set; }

        /// <summary>
        /// Loads every format from Google's sample ad units in place of the given ones, so all of them serve test
        /// ads. Unlike <see cref="EnableTestDevice"/>, which keeps the real ad units, this checks the ad flows
        /// without them. Set it before ads load: ads already loaded keep their ad units. Off for release.
        /// </summary>
        public static bool UseTestAdUnits { get; set; }

        // Rewarded

        /// <summary>
        /// Loads rewarded ads for <paramref name="id"/> from one ad unit. By default one ad is kept, loaded
        /// again after each show, and failed loads are retried after 2, 4, 8, … up to 64 seconds. With
        /// <paramref name="preload"/>, the plugin's preloader keeps <paramref name="bufferSize"/> ads ready
        /// (1 to 5; 0 means 2).
        /// </summary>
        public static bool LoadRewarded(string id, string adUnitId, bool preload = false, int bufferSize = 0) =>
            LoadPluginAd(rewardedAds, "LoadRewarded", id, HDCTestAdUnits.Pick(HDCAdFormat.Rewarded, adUnitId), preload, bufferSize,
                () => new HDCGmaRewardedAd(id, adUnitId, preload, bufferSize));

        /// <summary>
        /// Shows a ready rewarded ad. <paramref name="onClosed"/> runs once it closes or fails to show, with
        /// whether the player earned the reward. False when no ad is ready; a
        /// <see cref="HDCAdEventType.ShowFailed"/> event is also sent then, and a load starts.
        /// </summary>
        public static bool ShowRewarded(string id, Action<bool> onClosed = null) =>
            ShowPluginAd(rewardedAds, HDCAdFormat.Rewarded, id, onClosed);

        public static bool IsRewardedReady(string id) => rewardedAds.TryGetValue(id ?? string.Empty, out HDCGmaFullscreenAd ad) && ad.IsReady;

        public static void DestroyRewarded(string id) => DestroyPluginAd(rewardedAds, id);

        // App open

        /// <summary>Loads app open ads for <paramref name="id"/>; the options work as in <see cref="LoadRewarded"/>.</summary>
        public static bool LoadAppOpen(string id, string adUnitId, bool preload = false, int bufferSize = 0) =>
            LoadPluginAd(appOpenAds, "LoadAppOpen", id, HDCTestAdUnits.Pick(HDCAdFormat.AppOpen, adUnitId), preload, bufferSize,
                () => new HDCGmaAppOpenAd(id, adUnitId, preload, bufferSize));

        /// <summary>
        /// Shows a ready app open ad; <paramref name="onClosed"/> runs once it closes or fails to show. Ads
        /// older than four hours count as not ready.
        /// </summary>
        public static bool ShowAppOpen(string id, Action onClosed = null) =>
            ShowPluginAd(appOpenAds, HDCAdFormat.AppOpen, id, onClosed == null ? (Action<bool>)null : _ => onClosed());

        public static bool IsAppOpenReady(string id) => appOpenAds.TryGetValue(id ?? string.Empty, out HDCGmaFullscreenAd ad) && ad.IsReady;

        public static void DestroyAppOpen(string id) => DestroyPluginAd(appOpenAds, id);

        // Banner and MREC views

        /// <summary>
        /// Loads a banner or MREC view for <paramref name="id"/> from one ad unit. The view stays hidden until
        /// <see cref="ShowBannerView"/> and refreshes on the ad unit's schedule once loaded. Loading again with
        /// another ad unit or placement replaces the view.
        /// </summary>
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

        /// <summary>Shows the view now, or as soon as its first ad loads.</summary>
        public static void ShowBannerView(string id) => WithBannerView(id, view => view.Show());

        public static void HideBannerView(string id) => WithBannerView(id, view => view.Hide());

        /// <summary>The view's size in screen pixels; zero until it has been created.</summary>
        public static Vector2 GetBannerViewSizeInPixels(string id) =>
            bannerViews.TryGetValue(id ?? string.Empty, out HDCGmaBannerView view) ? view.SizeInPixels : Vector2.zero;

        /// <summary>True once the view has loaded an ad; it keeps an ad from then on.</summary>
        public static bool IsBannerViewLoaded(string id) => bannerViews.TryGetValue(id ?? string.Empty, out HDCGmaBannerView view) && view.IsLoaded;

        public static void MoveBannerView(string id, HDCAdPosition position) => WithBannerView(id, view => view.Move(position));

        /// <summary>Centers the view on a point in Unity screen pixels, such as a UI element's screen position.</summary>
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

            // Same settings: load again, which does nothing while an ad is ready or loading. New settings: start over.
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
