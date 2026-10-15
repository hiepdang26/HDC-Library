using System;

namespace HDC.Ads.Ports
{
    internal interface ITrackingAuthorization
    {
        HDCTrackingStatus Status { get; }

        void Request(Action<HDCTrackingStatus> onDone);
    }
}
