namespace HDC.Ads.Ports
{
    /// <summary>The SDKs' switches for testing, behind <see cref="HDCAds.Testing"/>.</summary>
    internal interface IAdsTesting
    {
        bool DebugLog { get; set; }

        void EnableTestDevice();

        bool IsTestDevice { get; }

        bool UseTestAdUnits { get; set; }

        bool EnableMetaTestMode(string[] extraDeviceHashes, int testAdType);

        void DisableMetaTestMode();

        bool IsMetaTestMode { get; }

        string MetaTestDeviceHash { get; }
    }
}
