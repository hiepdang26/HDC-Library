using System;
using GoogleMobileAds.Api;

namespace HDC.Ads.Infrastructure
{
    internal sealed class HDCGmaFailure
    {
        internal HDCGmaFailure(int code, string message)
        {
            Code = code;
            Message = message;
        }

        internal int Code { get; }

        internal string Message { get; }

        internal static HDCGmaFailure Read(AdError error, string fallbackMessage)
        {
            if (error == null)
                return new HDCGmaFailure(-1, fallbackMessage);
            try
            {
                return new HDCGmaFailure(error.GetCode(), error.GetMessage());
            }
            catch (Exception)
            {
                return new HDCGmaFailure(-1, error.ToString());
            }
        }
    }
}
