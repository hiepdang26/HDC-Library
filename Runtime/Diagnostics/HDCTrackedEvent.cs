using System;
using HDC.Ads.Domain;

namespace HDC.Ads.Diagnostics
{
    /// <summary>One ad event and when it came.</summary>
    internal sealed class HDCTrackedEvent
    {
        internal HDCTrackedEvent(HDCAdEvent adEvent, DateTime clock)
        {
            Event = adEvent;
            Clock = clock;
        }

        internal HDCAdEvent Event { get; }
        internal DateTime Clock { get; }
    }
}
