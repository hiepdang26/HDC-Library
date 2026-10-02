using System;

namespace HDC.Ads.Infrastructure
{
    internal interface IHDCNativeBridge
    {
        string Call(string method, string argsJson);

        void SetEventHandler(Action<string> onEvent);
    }
}
