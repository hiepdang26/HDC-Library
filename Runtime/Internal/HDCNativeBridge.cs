using System;

namespace HDC.Ads.Internal
{
    /// <summary>The native side of the SDK: one command entry point and one event channel.</summary>
    internal interface IHDCNativeBridge
    {
        /// <summary>Runs a command; the result is JSON: {"ok":…,"value":…,"error":…}.</summary>
        string Call(string method, string argsJson);

        /// <summary>Sets the handler that receives every event's JSON on the Unity main thread.</summary>
        void SetEventHandler(Action<string> onEvent);
    }

    internal static class HDCNativeBridge
    {
        internal static IHDCNativeBridge Create()
        {
#if UNITY_EDITOR
            return new HDCEditorBridge();
#elif UNITY_IOS
            return new HDCIosBridge();
#elif UNITY_ANDROID
            return new HDCAndroidBridge();
#else
            return new HDCUnsupportedBridge();
#endif
        }
    }

    /// <summary>Platforms without the native library: every command fails.</summary>
    internal sealed class HDCUnsupportedBridge : IHDCNativeBridge
    {
        public string Call(string method, string argsJson) =>
            "{\"ok\":false,\"error\":\"HDC ads are not available on this platform\"}";

        public void SetEventHandler(Action<string> onEvent)
        {
        }
    }
}
