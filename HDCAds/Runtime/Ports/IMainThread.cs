using System;

namespace HDC.Ads.Ports
{
    internal interface IMainThread
    {
        event Action Ticked;

        event Action LateTicked;

        event Action<bool> ApplicationPaused;
    }
}
