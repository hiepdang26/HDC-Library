using HDC.Ads.Ports;
using UnityEngine;

namespace HDC.Ads.Infrastructure
{
    /// <summary>Unity's console, while <see cref="HDCAdsSdk.DebugLog"/> is on.</summary>
    internal sealed class HDCUnityAdsLog : IAdsLog
    {
        public void Info(string message)
        {
            if (HDCAdsSdk.DebugLog)
                Debug.Log("[HDCAds] " + message);
        }
    }
}
