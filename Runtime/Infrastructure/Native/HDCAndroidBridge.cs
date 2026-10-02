#if UNITY_ANDROID && !UNITY_EDITOR
using System;
using UnityEngine;

namespace HDC.Ads.Infrastructure
{
    internal sealed class HDCAndroidBridge : IHDCNativeBridge
    {
        private const string BridgeClassName = "com.hdc.adsmultiplatform.unity.HdcUnityBridge";
        private const string ListenerInterfaceName = "com.hdc.adsmultiplatform.unity.HdcUnityListener";

        private readonly AndroidJavaClass bridgeClass = new AndroidJavaClass(BridgeClassName);

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

            public void onEvent(string eventJson)
            {
                HDCMainThread.Post(() => handler(eventJson));
            }
        }
    }
}
#endif
