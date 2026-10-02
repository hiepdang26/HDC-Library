using HDC.Ads.Ports;

namespace HDC.Ads.Infrastructure
{
    /// <summary>The test switches of <see cref="HDCAdsSdk"/>.</summary>
    internal sealed class HDCAdsTestingPort : IAdsTesting
    {
        public bool DebugLog
        {
            get => HDCAdsSdk.DebugLog;
            set => HDCAdsSdk.DebugLog = value;
        }

        public void EnableTestDevice() => HDCAdsSdk.EnableTestDevice();

        public bool IsTestDevice => HDCAdsSdk.IsTestDevice;

        public bool UseTestAdUnits
        {
            get => HDCAdsSdk.UseTestAdUnits;
            set => HDCAdsSdk.UseTestAdUnits = value;
        }

        public bool EnableMetaTestMode(string[] extraDeviceHashes, int testAdType) =>
            HDCAdsSdk.EnableMetaTestMode(extraDeviceHashes, testAdType);

        public void DisableMetaTestMode() => HDCAdsSdk.DisableMetaTestMode();

        public bool IsMetaTestMode => HDCAdsSdk.IsMetaTestMode();

        public string MetaTestDeviceHash => HDCAdsSdk.GetMetaTestDeviceHash();
    }
}
