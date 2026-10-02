using System;
using System.Collections.Generic;
using HDC.Ads.Diagnostics;
using HDC.Ads.Domain;
using UnityEngine;

namespace HDC.Ads.Infrastructure
{
    internal static partial class HDCAdsSdk
    {
        private const string LogTag = "[HDCAds]";

        private static readonly Dictionary<string, InterstitialLoad> interstitialLoads = new Dictionary<string, InterstitialLoad>();
        private static IHDCNativeBridge bridge;
        private static Action initializedCallbacks;

        public static event Action<HDCAdEvent> AdEvent;

        public static bool IsInitialized { get; private set; }

        public static bool DebugLog { get; set; }

        public static void Initialize(Action onInitialized = null)
        {
            if (onInitialized != null)
            {
                if (IsInitialized)
                    onInitialized();
                else
                    initializedCallbacks += onInitialized;
            }

            HDCGma.Initialize();
            Call<BoolResult>("init", "{\"debugLog\":" + (DebugLog ? "true" : "false") + "}");
        }

        public static bool LoadInterstitial(string id, string[] adUnitIds, int bufferSize = 1, bool autoReload = true)
        {
            adUnitIds = HDCTestAdUnits.Pick(HDCAdFormat.Interstitial, adUnitIds);
            string argsJson = HDCJson.Args(id, new InterstitialFields { bufferSize = bufferSize, autoReload = autoReload }, adUnitIds);
            if (!Call<BoolResult>("interstitial.load", argsJson).ok)
                return false;

            if (!interstitialLoads.TryGetValue(id, out InterstitialLoad load))
                interstitialLoads[id] = load = new InterstitialLoad();
            load.ArgsJson = argsJson;
            load.Retry.Cancel();
            HDCAdsTracker.Requested(HDCAdFormat.Interstitial, id, Units(adUnitIds));
            return true;
        }

        public static bool ShowInterstitial(string id, bool immersiveMode = true)
        {
            if (Call("interstitial.show", id, new ShowInterstitialFields { immersiveMode = immersiveMode }).value)
                return true;

            if (interstitialLoads.TryGetValue(id ?? string.Empty, out InterstitialLoad load) && !load.Retry.IsWaiting)
            {
                HDCAdsTracker.Requested(HDCAdFormat.Interstitial, id, null);
                Call<BoolResult>("interstitial.load", load.ArgsJson);
            }

            return false;
        }

        public static bool IsInterstitialReady(string id) => Call("interstitial.isReady", id).value;

        public static void DestroyInterstitial(string id)
        {
            if (interstitialLoads.TryGetValue(id ?? string.Empty, out InterstitialLoad load))
            {
                load.Retry.Reset();
                interstitialLoads.Remove(id);
            }

            Call("interstitial.destroy", id);
            HDCAdsTracker.Destroyed(HDCAdFormat.Interstitial, id);
        }

        public static bool LoadFullscreen(string id, string[] adUnitIds, bool reloadAfterShow = true)
        {
            adUnitIds = HDCTestAdUnits.Pick(HDCAdFormat.Fullscreen, adUnitIds);
            return Requested(HDCAdFormat.Fullscreen, id, adUnitIds,
                Call("fullscreen.load", id, new FullscreenLoadFields { reloadAfterShow = reloadAfterShow }, adUnitIds).ok);
        }

        public static bool ShowFullscreen(string id, HDCFullscreenOptions options = null) =>
            Call("fullscreen.show", id, options ?? new HDCFullscreenOptions()).value;

        public static void HideFullscreen(string id) => Call("fullscreen.hide", id);

        public static bool IsFullscreenReady(string id) => Call("fullscreen.isReady", id).value;

        public static void DestroyFullscreen(string id)
        {
            Call("fullscreen.destroy", id);
            HDCAdsTracker.Destroyed(HDCAdFormat.Fullscreen, id);
        }

        public static bool LoadPopup(string id, string[] adUnitIds, HDCPopupOptions options = null)
        {
            adUnitIds = HDCTestAdUnits.Pick(HDCAdFormat.Popup, adUnitIds);
            return Requested(HDCAdFormat.Popup, id, adUnitIds, Call("popup.load", id, options ?? new HDCPopupOptions(), adUnitIds).ok);
        }

        public static void UpdatePopupPlacement(string id, float x, float y, float width, float height) =>
            Call("popup.updatePlacement", id, new PlacementFields { x = x, y = y, width = width, height = height });

        public static bool ShowPopup(string id) => Call("popup.show", id).value;

        public static void ClosePopup(string id) => Call("popup.close", id);

        public static void HidePopup(string id) => Call("popup.hide", id);

        public static void StopPopup(string id) => Call("popup.stop", id);

        public static void DestroyPopup(string id)
        {
            Call("popup.destroy", id);
            HDCAdsTracker.Destroyed(HDCAdFormat.Popup, id);
        }

        public static bool IsPopupReady(string id) => Call("popup.isReady", id).value;

        public static bool IsPopupDisplayable(string id) => Call("popup.isDisplayable", id).value;

        public static string GetPopupState(string id) =>
            Call<StringResult>("popup.state", HDCJson.Args(id)).value ?? string.Empty;

        public static bool LoadBanner(string id, string[] adUnitIds, HDCBannerOptions options = null)
        {
            adUnitIds = HDCTestAdUnits.Pick(HDCAdFormat.Banner, adUnitIds);
            return Requested(HDCAdFormat.Banner, id, adUnitIds, Call("banner.load", id, options ?? new HDCBannerOptions(), adUnitIds).ok);
        }

        public static void ShowBanner(string id) => Call("banner.show", id);

        public static void HideBanner(string id) => Call("banner.hide", id);

        public static bool ExpandBanner(string id, bool enableClick = true) =>
            Call("banner.expand", id, new ExpandFields { enableClick = enableClick }).value;

        public static void CollapseBanner(string id) => Call("banner.collapse", id);

        public static void DestroyBanner(string id)
        {
            Call("banner.destroy", id);
            HDCAdsTracker.Destroyed(HDCAdFormat.Banner, id);
        }

        public static bool EnableMetaTestMode(string[] extraDeviceHashes = null, int testAdType = 0) =>
            Call<BoolResult>(
                "meta.enableTestMode",
                JsonUtility.ToJson(new MetaFields { deviceHashes = extraDeviceHashes ?? new string[0], testAdType = testAdType })).value;

        public static void DisableMetaTestMode() => Call<BoolResult>("meta.disableTestMode", "{}");

        public static bool IsMetaTestMode() => Call<BoolResult>("meta.isTestMode", "{}").value;

        public static string GetMetaTestDeviceHash() =>
            Call<StringResult>("meta.deviceHash", "{}").value ?? string.Empty;

        internal static void ResetStatics()
        {
            interstitialLoads.Clear();
            rewardedAds.Clear();
            appOpenAds.Clear();
            bannerViews.Clear();
            HDCAdsTracker.Reset();
            bridge = null;
            initializedCallbacks = null;
            AdEvent = null;
            IsInitialized = false;
            DebugLog = false;
            IsTestDevice = false;
            UseTestAdUnits = false;
        }

        private static bool Requested(string format, string id, string[] adUnitIds, bool accepted)
        {
            if (accepted)
                HDCAdsTracker.Requested(format, id, Units(adUnitIds));
            return accepted;
        }

        private static string Units(string[] adUnitIds) => adUnitIds == null ? null : string.Join(", ", adUnitIds);

        private static BoolResult Call(string method, string id, object fields = null, string[] adUnitIds = null) =>
            Call<BoolResult>(method, HDCJson.Args(id, fields, adUnitIds));

        private static bool IsQuery(string method) =>
            method.EndsWith(".state", StringComparison.Ordinal) || method.EndsWith(".isReady", StringComparison.Ordinal)
            || method.EndsWith(".isDisplayable", StringComparison.Ordinal) || method == "meta.isTestMode" || method == "meta.deviceHash";

        private static TResult Call<TResult>(string method, string argsJson)
            where TResult : Result, new()
        {
            EnsureBridge();
            if (DebugLog && !IsQuery(method))
                Debug.Log($"{LogTag} call {method} {argsJson}");

            TResult result;
            try
            {
                result = JsonUtility.FromJson<TResult>(bridge.Call(method, argsJson)) ?? new TResult { error = "Empty result" };
            }
            catch (Exception exception)
            {
                result = new TResult { error = exception.Message };
            }

            if (!result.ok)
                Debug.LogWarning($"{LogTag} {method} failed: {result.error}");
            return result;
        }

        private static void EnsureBridge()
        {
            if (bridge != null)
                return;

            HDCMainThread.EnsureCreated();
            bridge = HDCNativeBridge.Create();
            bridge.SetEventHandler(OnEventJson);
        }

        private static void OnEventJson(string eventJson)
        {
            HDCAdEvent adEvent;
            try
            {
                adEvent = JsonUtility.FromJson<HDCAdEvent>(eventJson);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"{LogTag} invalid event {eventJson}: {exception.Message}");
                return;
            }

            Emit(adEvent);
        }

        internal static void Emit(HDCAdEvent adEvent)
        {
            if (adEvent == null)
                return;
            HDCAdsTracker.Record(adEvent);
            if (DebugLog)
                Debug.Log($"{LogTag} event {adEvent}");
            if (adEvent.format == HDCAdFormat.Interstitial)
                RetryInterstitial(adEvent);

            if (adEvent.format == HDCAdFormat.Sdk && adEvent.type == HDCAdEventType.Initialized)
            {
                IsInitialized = true;
                Action callbacks = initializedCallbacks;
                initializedCallbacks = null;
                if (callbacks != null)
                {
                    foreach (Action callback in callbacks.GetInvocationList())
                        Invoke(callback);
                }
            }

            Action<HDCAdEvent> handlers = AdEvent;
            if (handlers == null)
                return;
            foreach (Action<HDCAdEvent> handler in handlers.GetInvocationList())
                Invoke(() => handler(adEvent));
        }

        private static void RetryInterstitial(HDCAdEvent adEvent)
        {
            if (!interstitialLoads.TryGetValue(adEvent.id ?? string.Empty, out InterstitialLoad load))
                return;

            if (adEvent.type == HDCAdEventType.Loaded)
            {
                load.Retry.Reset();
            }
            else if (adEvent.type == HDCAdEventType.LoadFailed && !load.Retry.IsWaiting)
            {
                string id = adEvent.id;
                load.Retry.Schedule(() =>
                {
                    if (!interstitialLoads.TryGetValue(id, out InterstitialLoad current) || current != load)
                        return;
                    HDCAdsTracker.Requested(HDCAdFormat.Interstitial, id, null);
                    Call<BoolResult>("interstitial.load", load.ArgsJson);
                });
                HDCAdsTracker.RetryScheduled(HDCAdFormat.Interstitial, id, load.Retry.Delay, load.Retry.Attempt);
            }
        }

        private static void Invoke(Action action)
        {
            try
            {
                action();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

#pragma warning disable 0649
        [Serializable]
        private class Result
        {
            public bool ok;
            public string error;
        }

        [Serializable]
        private sealed class BoolResult : Result
        {
            public bool value;
        }

        [Serializable]
        private sealed class StringResult : Result
        {
            public string value;
        }
#pragma warning restore 0649

        private sealed class InterstitialLoad
        {
            internal readonly HDCRetry Retry = new HDCRetry();
            internal string ArgsJson;
        }

        [Serializable]
        private sealed class InterstitialFields
        {
            public int bufferSize;
            public bool autoReload;
        }

        [Serializable]
        private sealed class ShowInterstitialFields
        {
            public bool immersiveMode;
        }

        [Serializable]
        private sealed class FullscreenLoadFields
        {
            public bool reloadAfterShow;
        }

        [Serializable]
        private sealed class PlacementFields
        {
            public float x;
            public float y;
            public float width;
            public float height;
        }

        [Serializable]
        private sealed class ExpandFields
        {
            public bool enableClick;
        }

        [Serializable]
        private sealed class MetaFields
        {
            public string[] deviceHashes;
            public int testAdType;
        }
    }
}
