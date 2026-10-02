using System;
using UnityEngine;

namespace HDC.Ads.Logic
{
    internal static class HDCCallbacks
    {
        internal static void Run(Action action)
        {
            if (action == null)
                return;
            try
            {
                action();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }
    }
}
