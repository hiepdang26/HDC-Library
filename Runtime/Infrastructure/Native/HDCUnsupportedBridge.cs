using System;

namespace HDC.Ads.Infrastructure
{
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
