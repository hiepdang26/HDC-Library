#if UNITY_IOS && !UNITY_EDITOR
using System;
using System.Runtime.InteropServices;
using System.Text;
using AOT;
using UnityEngine;

namespace HDC.Ads.Infrastructure
{
    internal sealed class HDCIosBridge : IHDCNativeBridge
    {
        private delegate void EventCallback(IntPtr eventJson);

        [DllImport("__Internal")]
        private static extern string HDCAds_Call(string method, string argsJson);

        [DllImport("__Internal")]
        private static extern void HDCAds_SetEventCallback(EventCallback callback);

        private static readonly EventCallback Callback = OnEvent;
        private static Action<string> handler;

        public string Call(string method, string argsJson) => HDCAds_Call(method, argsJson);

        public void SetEventHandler(Action<string> onEvent)
        {
            handler = onEvent;
            HDCAds_SetEventCallback(Callback);
        }

        [MonoPInvokeCallback(typeof(EventCallback))]
        private static void OnEvent(IntPtr eventJson)
        {
            try
            {
                handler?.Invoke(Utf8String(eventJson));
            }
            catch (Exception exception)
            {
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
