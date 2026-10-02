using HDC.Ads.Ports;

namespace HDC.Ads.Tests
{
    /// <summary>Test switches that only remember what they were set to.</summary>
    internal sealed class HDCFakeTesting : IAdsTesting
    {
        public bool DebugLog { get; set; }

        public bool IsTestDevice { get; private set; }

        public bool UseTestAdUnits { get; set; }

        public bool IsMetaTestMode { get; private set; }

        public string MetaTestDeviceHash => "fake-hash";

        public void EnableTestDevice() => IsTestDevice = true;

        public bool EnableMetaTestMode(string[] extraDeviceHashes, int testAdType) => IsMetaTestMode = true;

        public void DisableMetaTestMode() => IsMetaTestMode = false;
    }
}
