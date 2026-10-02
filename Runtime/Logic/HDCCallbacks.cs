using System;
using UnityEngine;

namespace HDC.Ads.Logic
{
    /// <summary>Runs the game's callbacks: an exception is logged instead of breaking the ad flow.</summary>
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
