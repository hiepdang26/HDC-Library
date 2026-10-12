using HDC.Ads.Ports;

namespace HDC.Ads.Infrastructure
{
    internal sealed class HDCAdsTestingPort : IAdsTesting
    {
        private readonly HDCMetaPartner meta;

        internal HDCAdsTestingPort(HDCMetaPartner meta)
        {
            this.meta = meta;
        }

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

        public bool EnableMetaTestMode(string[] extraDeviceHashes, int testAdType)
        {
            meta.TestAdType = testAdType;
            return meta.EnableTestMode(extraDeviceHashes);
        }

        public void DisableMetaTestMode() => meta.DisableTestMode();

        public bool IsMetaTestMode => meta.IsTestMode;

        public string MetaTestDeviceHash => meta.TestDeviceId;
    }
}
