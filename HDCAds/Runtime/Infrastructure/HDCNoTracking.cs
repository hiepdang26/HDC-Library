using System;
using HDC.Ads.Ports;

namespace HDC.Ads.Infrastructure
{
    internal sealed class HDCNoTracking : ITrackingAuthorization
    {
        public HDCTrackingStatus Status => HDCTrackingStatus.Unsupported;

        public void Request(Action<HDCTrackingStatus> onDone) => onDone?.Invoke(HDCTrackingStatus.Unsupported);
    }
}
