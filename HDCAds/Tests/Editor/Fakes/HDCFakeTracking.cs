using System;
using HDC.Ads.Ports;

namespace HDC.Ads.Tests
{
    internal sealed class HDCFakeTracking : ITrackingAuthorization
    {
        private Action<HDCTrackingStatus> pending;

        public HDCTrackingStatus Status { get; set; } = HDCTrackingStatus.Unsupported;

        internal int Requests { get; private set; }

        public void Request(Action<HDCTrackingStatus> onDone)
        {
            Requests++;
            pending = onDone;
        }

        internal void Answer(HDCTrackingStatus status)
        {
            Status = status;
            Action<HDCTrackingStatus> done = pending;
            pending = null;
            done?.Invoke(status);
        }
    }
}
