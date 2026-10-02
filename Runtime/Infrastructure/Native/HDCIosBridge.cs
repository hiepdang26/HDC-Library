#if UNITY_IOS && !UNITY_EDITOR
using System;
using System.Runtime.InteropServices;
using System.Text;
using AOT;
using UnityEngine;

namespace HDC.Ads.Infrastructure
{
    /// <summary>Calls the HDCAds framework through HDCAdsBridge.mm.</summary>
    internal sealed class HDCIosBridge : IHDCNativeBridge
    {
        private delegate void EventCallback(IntPtr eventJson);

        // The returned string is heap allocated by the plugin, and the marshaler frees it.
        [DllImport("__Internal")]
        private static extern string HDCAds_Call(string method, string argsJson);

        [DllImport("__Internal")]
        private static extern void HDCAds_SetEventCallback(EventCallback callback);

        // Held in a static field so the function pointer given to native code stays valid.
        private static readonly EventCallback Callback = OnEvent;
        private static Action<string> handler;

        public string Call(string method, string argsJson) => HDCAds_Call(method, argsJson);

        public void SetEventHandler(Action<string> onEvent)
        {
            handler = onEvent;
            HDCAds_SetEventCallback(Callback);
        }

        // Called directly on the main thread, which is Unity's player thread on iOS. This also works
        // while a fullscreen ad pauses Unity, when a UnitySendMessage would wait until the ad closes.
        [MonoPInvokeCallback(typeof(EventCallback))]
        private static void OnEvent(IntPtr eventJson)
        {
            try
            {
                handler?.Invoke(Utf8String(eventJson));
            }
            catch (Exception exception)
            {
                // Never let a managed exception unwind into native code.
                Debug.LogException(exception);
            }
        }

        private static string Utf8String(IntPtr pointer)
        {
            if (pointer == IntPtr.Zero)
                return string.Empty;

            int length = 0;
            while (Marshal.ReadByte(pointer, length) != 0)
                length++;

            var bytes = new byte[length];
            Marshal.Copy(pointer, bytes, 0, length);
            return Encoding.UTF8.GetString(bytes);
        }
    }
}
#endif
