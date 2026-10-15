#if UNITY_IOS && !UNITY_EDITOR
using System;
using System.Runtime.InteropServices;
using AOT;
using HDC.Ads.Ports;

namespace HDC.Ads.Infrastructure
{
    internal sealed class HDCAppTracking : ITrackingAuthorization
    {
        private delegate void StatusCallback(int status);

        [DllImport("__Internal")]
        private static extern int HDCAds_TrackingStatus();

        [DllImport("__Internal")]
        private static extern void HDCAds_RequestTracking(StatusCallback callback);

        private static readonly StatusCallback Callback = OnStatus;
        private static Action<HDCTrackingStatus> pending;

        public HDCTrackingStatus Status => ToStatus(HDCAds_TrackingStatus());

        public void Request(Action<HDCTrackingStatus> onDone)
        {
            HDCMainThread.EnsureCreated();
            pending = onDone;
            HDCAds_RequestTracking(Callback);
        }

        [MonoPInvokeCallback(typeof(StatusCallback))]
        private static void OnStatus(int status)
        {
            Action<HDCTrackingStatus> done = pending;
            pending = null;
            HDCMainThread.Post(() => done?.Invoke(ToStatus(status)));
        }

        private static HDCTrackingStatus ToStatus(int status) =>
            status >= (int)HDCTrackingStatus.NotDetermined && status <= (int)HDCTrackingStatus.Authorized
                ? (HDCTrackingStatus)status
                : HDCTrackingStatus.Unsupported;
    }
}
#endif
