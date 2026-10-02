namespace HDC.Ads.Ports
{
    /// <summary>
    /// A mediation partner inside Google Mobile Ads, such as Meta Audience Network, with the setup only it needs.
    /// Adding a partner is one class and one line in HDCAdsRuntime.
    /// </summary>
    internal interface IMediationPartner
    {
        /// <summary>The partner's name, as the debug panel shows it.</summary>
        string Name { get; }

        /// <summary>The adapter class Google Mobile Ads reports the partner's start under, on this platform.</summary>
        string AdapterClass { get; }

        /// <summary>True when the partner has a test mode of its own, apart from the Google test device.</summary>
        bool HasTestMode { get; }

        bool IsTestMode { get; }

        /// <summary>This device's id in the partner's test mode, to register it from another device.</summary>
        string TestDeviceId { get; }

        /// <summary>Asks the partner for test ads on this device and the extra ones. False when it cannot.</summary>
        bool EnableTestMode(string[] extraDeviceIds);

        void DisableTestMode();
    }
}
