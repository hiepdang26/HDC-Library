using System;
using HDC.Ads.Internal;
using UnityEngine;

namespace HDC.Ads
{
    /// <summary>
    /// Direct access to the native ads SDK: load, show and query ads by instance id. Call it from the
    /// Unity main thread. Events arrive through <see cref="AdEvent"/>, also on the main thread.
    /// </summary>
    public static class HDCAdsSdk
    {
        private const string LogTag = "[HDCAds]";

        private static IHDCNativeBridge bridge;
        private static Action initializedCallbacks;

        /// <summary>Raised for every ad event: loaded, failed, shown, impression, clicked, paid, closed.</summary>
        public static event Action<HDCAdEvent> AdEvent;

        /// <summary>True once the SDK has sent <see cref="HDCAdEventType.Initialized"/>.</summary>
        public static bool IsInitialized { get; private set; }

        /// <summary>Logs every command and event, in Unity and in the native log.</summary>
        public static bool DebugLog { get; set; }

        /// <summary>
        /// Starts the SDK. <paramref name="onInitialized"/> runs once it is ready, right away when it already is.
        /// </summary>
        public static void Initialize(Action onInitialized = null)
        {
            if (onInitialized != null)
            {
                if (IsInitialized)
                    onInitialized();
                else
                    initializedCallbacks += onInitialized;
            }

            Call("init", new InitArgs { debugLog = DebugLog });
        }

        /// <summary>
        /// Loads interstitials for <paramref name="id"/>. The ad units are tried in order, and
        /// <paramref name="bufferSize"/> ads per unit are kept ready. With <paramref name="autoReload"/>,
        /// the next ad loads after each show.
        /// </summary>
        public static bool LoadInterstitial(string id, string[] adUnitIds, int bufferSize = 1, bool autoReload = true)
        {
            var args = new InterstitialLoadArgs
            {
                id = id,
                adUnitIds = adUnitIds,
                bufferSize = bufferSize,
                autoReload = autoReload,
            };
            return Call("interstitial.load", args).ok;
        }

        /// <summary>
        /// Shows a ready interstitial. False when none is ready; a <see cref="HDCAdEventType.ShowFailed"/>
        /// event is also sent then.
        /// </summary>
        public static bool ShowInterstitial(string id, bool immersiveMode = true) =>
            Call("interstitial.show", new InterstitialShowArgs { id = id, immersiveMode = immersiveMode }).value;

        public static bool IsInterstitialReady(string id) =>
            Call("interstitial.isReady", new IdArgs { id = id }).value;

        public static void DestroyInterstitial(string id) =>
            Call("interstitial.destroy", new IdArgs { id = id });

        private static CallResult Call(string method, object args)
        {
            EnsureBridge();
            string argsJson = JsonUtility.ToJson(args);
            if (DebugLog)
                Debug.Log($"{LogTag} call {method} {argsJson}");

            string resultJson;
            try
            {
                resultJson = bridge.Call(method, argsJson);
            }
            catch (Exception exception)
            {
                Debug.LogError($"{LogTag} {method} failed: {exception.Message}");
                return new CallResult { error = exception.Message };
            }

            CallResult result = ParseResult(resultJson);
            if (!result.ok)
                Debug.LogWarning($"{LogTag} {method} failed: {result.error}");
            return result;
        }

        private static CallResult ParseResult(string resultJson)
        {
            try
            {
                return JsonUtility.FromJson<CallResult>(resultJson) ?? new CallResult { error = "Empty result" };
            }
            catch (Exception exception)
            {
                return new CallResult { error = $"Invalid result {resultJson}: {exception.Message}" };
            }
        }

        private static void EnsureBridge()
        {
            if (bridge != null)
                return;

            HDCMainThread.EnsureCreated();
            bridge = HDCNativeBridge.Create();
            bridge.SetEventHandler(OnEventJson);
        }

        // Always runs on the main thread: the bridges hand events over there.
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

            if (adEvent == null)
                return;
            if (DebugLog)
                Debug.Log($"{LogTag} event {adEvent}");

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

        // One failing subscriber must not stop the others, nor unwind into native code.
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

#pragma warning disable 0649 // Assigned by JsonUtility.
        [Serializable]
        private sealed class CallResult
        {
            public bool ok;
            public bool value;
            public string error;
        }
#pragma warning restore 0649

        [Serializable]
        private sealed class InitArgs
        {
            public bool debugLog;
        }

        [Serializable]
        private sealed class IdArgs
        {
            public string id;
        }

        [Serializable]
        private sealed class InterstitialLoadArgs
        {
            public string id;
            public string[] adUnitIds;
            public int bufferSize;
            public bool autoReload;
        }

        [Serializable]
        private sealed class InterstitialShowArgs
        {
            public string id;
            public bool immersiveMode;
        }
    }
}
