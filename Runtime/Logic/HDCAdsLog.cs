using System;
using HDC.Ads.Infrastructure;
using UnityEngine;

namespace HDC.Ads.Logic
{
    /// <summary>Small helpers shared by the channels: debug logging, safe PlayerPrefs and safe callbacks.</summary>
    internal static class HDCAdsLog
    {
        internal static void Info(string message)
        {
            if (HDCAdsSdk.DebugLog)
                Debug.Log("[HDCAds] " + message);
        }

        internal static int GetInt(string key)
        {
            try
            {
                return PlayerPrefs.GetInt(key, 0);
            }
            catch (Exception)
            {
                return 0;
            }
        }

        internal static void SetInt(string key, int value)
        {
            try
            {
                PlayerPrefs.SetInt(key, value);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[HDCAds] cannot save {key}: {exception.Message}");
            }
        }

        /// <summary>Runs a game callback; an exception is logged instead of breaking the ad flow.</summary>
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
