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
    }
}
