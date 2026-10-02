using System;

namespace HDC.Ads.Diagnostics
{
    /// <summary>A failed load or show: the ad SDK's error code, or -1 for an error without one.</summary>
    internal sealed class HDCAdError
    {
        internal int Code;
        internal string Message;
        internal string AdUnitId;
        internal DateTime Clock;
    }
}
