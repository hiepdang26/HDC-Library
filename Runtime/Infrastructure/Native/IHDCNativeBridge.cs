using System;

namespace HDC.Ads.Infrastructure
{
    /// <summary>The native side of the SDK: one command entry point and one event channel.</summary>
    internal interface IHDCNativeBridge
    {
        /// <summary>Runs a command; the result is JSON: {"ok":…,"value":…,"error":…}.</summary>
        string Call(string method, string argsJson);

        /// <summary>Sets the handler that receives every event's JSON on the Unity main thread.</summary>
        void SetEventHandler(Action<string> onEvent);
    }
}
