using HDC.Ads.Diagnostics;

namespace HDC.Ads.Logic
{
    internal interface IAdChannel
    {
        string Key { get; }

        string Title { get; }

        IChannelDiagnostics Diagnostics { get; }

        IConfigRule ConfigRule { get; }

        void OnSdkInitialized();

        void InitializeAll();

        void OnAdsRemoved();
    }
}
