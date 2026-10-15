using System;
using GoogleMobileAds.Api;
using UnityEngine;

namespace HDC.Ads.Infrastructure
{
    internal sealed class HDCGmaResponse
    {
        internal HDCGmaResponse(string responseId, string adapter, string adSource)
        {
            ResponseId = responseId;
            Adapter = adapter;
            AdSource = adSource;
        }

        internal string ResponseId { get; }

        internal string Adapter { get; }

        internal string AdSource { get; }

        internal static HDCGmaResponse Read(ResponseInfo response)
        {
            if (response == null)
                return null;
            try
            {
                return new HDCGmaResponse(response.GetResponseId(), response.GetMediationAdapterClassName(),
                    response.GetLoadedAdapterResponseInfo()?.AdSourceName);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[HDCAds] cannot read the ad response: " + exception.Message);
                return null;
            }
        }
    }
}
