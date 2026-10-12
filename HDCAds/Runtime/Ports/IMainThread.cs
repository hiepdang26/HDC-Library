using System;

namespace HDC.Ads.Ports
{
    internal interface IMainThread
    {
        event Action Ticked;

        event Action<bool> ApplicationPaused;
    }
}
