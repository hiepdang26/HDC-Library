using System;
using HDC.Ads.Ports;

namespace HDC.Ads.Logic
{
    internal static class HDCCallbacks
    {
        internal static void Run(Action action, IAdsLog log)
        {
            if (action == null)
                return;
            try
            {
                action();
            }
            catch (Exception exception)
            {
                log.Exception(exception);
            }
        }
    }
}
