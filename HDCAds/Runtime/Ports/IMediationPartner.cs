namespace HDC.Ads.Ports
{
    internal interface IMediationPartner
    {
        string Name { get; }

        string AdapterClass { get; }

        bool HasTestMode { get; }

        bool IsTestMode { get; }

        string TestDeviceId { get; }

        bool EnableTestMode(string[] extraDeviceIds);

        void DisableTestMode();
    }
}
