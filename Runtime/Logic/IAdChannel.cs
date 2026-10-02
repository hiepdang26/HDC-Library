using HDC.Ads.Diagnostics;

namespace HDC.Ads.Logic
{
    /// <summary>
    /// A channel as the library runs it. <see cref="HDCChannels"/> starts each once the SDK is ready and tells
    /// each when the player buys ad removal, the setup prefab starts them all, and the debug panel and its config
    /// check reach each through its modules. A new channel implements this and joins <see cref="HDCChannels.All"/>;
    /// nothing else names it.
    /// </summary>
    internal interface IAdChannel
    {
        /// <summary>A short key, such as FA: the debug panel's tab, and the channel its ads' events show under.</summary>
        string Key { get; }

        /// <summary>Its name, such as ForceAd.</summary>
        string Title { get; }

        IChannelDiagnostics Diagnostics { get; }

        IConfigRule ConfigRule { get; }

        /// <summary>The SDK is ready: start loading what loads on its own (autoInit).</summary>
        void OnSdkInitialized();

        /// <summary>
        /// Starts loading every group and slot the configs give the channel, for the setup prefab's Start All
        /// Channels. A channel that is off ignores it, and so does what already started.
        /// </summary>
        void InitializeAll();

        /// <summary>The player bought ad removal: hide the ads it covers.</summary>
        void OnAdsRemoved();
    }
}
