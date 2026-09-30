#if UNITY_ANDROID && !UNITY_EDITOR
using System;
using UnityEngine;

namespace HDC.Ads.Internal
{
    /// <summary>Calls the Android library's HdcUnityBridge through JNI.</summary>
    internal sealed class HDCAndroidBridge : IHDCNativeBridge
    {
        private const string BridgeClassName = "com.hdc.adsmultiplatform.unity.HdcUnityBridge";
        private const string ListenerInterfaceName = "com.hdc.adsmultiplatform.unity.HdcUnityListener";

        private readonly AndroidJavaClass bridgeClass = new AndroidJavaClass(BridgeClassName);

        // Kept so the proxy lives as long as the Java side holds it.
        private EventListener listener;

        public string Call(string method, string argsJson) =>
            bridgeClass.CallStatic<string>("call", method, argsJson);

        public void SetEventHandler(Action<string> onEvent)
        {
            listener = new EventListener(onEvent);
            bridgeClass.CallStatic("setListener", listener);
        }

        private sealed class EventListener : AndroidJavaProxy
        {
            private readonly Action<string> handler;

            public EventListener(Action<string> handler)
                : base(ListenerInterfaceName)
            {
                this.handler = handler;
            }

            // HdcUnityListener.onEvent(String). It runs on the Android UI thread, so the event is handed
            // to the Unity main thread.
            // ReSharper disable once InconsistentNaming
            public void onEvent(string eventJson)
            {
                HDCMainThread.Post(() => handler(eventJson));
            }
        }
    }
}
#endif
