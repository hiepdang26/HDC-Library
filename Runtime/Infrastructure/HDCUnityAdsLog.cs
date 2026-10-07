using System;
using HDC.Ads.Ports;
using UnityEngine;

namespace HDC.Ads.Infrastructure
{
    internal sealed class HDCUnityAdsLog : IAdsLog
    {
        public void Info(string message)
        {
            if (HDCAdsSdk.DebugLog)
                Debug.Log("[HDCAds] " + message);
        }

        public void Warning(string message) => Debug.LogWarning("[HDCAds] " + message);

        public void Exception(Exception exception) => Debug.LogException(exception);
    }
}
