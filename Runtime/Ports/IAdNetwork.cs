using HDC.Ads.Domain;

namespace HDC.Ads.Ports
{
    /// <summary>
    /// An ad network: it plans and makes the ads of the units the configs keep under its key. Adding a network is
    /// writing one and registering it in HDCAdsRuntime.
    /// </summary>
    internal interface IAdNetwork
    {
        /// <summary>The key of this network's units in the configs, one of <see cref="HDCAdUnitKeys"/>.</summary>
        string UnitKey { get; }

        /// <summary>What the debug panel calls the network.</summary>
        string Name { get; }

        /// <summary>The network paid events name, <see cref="HDCAdRevenue.Network"/>.</summary>
        string RevenueNetwork { get; }

        /// <summary>The ad this network would make for a use and a unit; null when it serves neither.</summary>
        HDCAdPlan Plan(HDCAdUse use, HDCAdUnitSpec spec);

        /// <summary>Makes the full-screen ad of one of this network's plans.</summary>
        IFullscreenAd CreateFullscreen(HDCAdPlan plan);

        /// <summary>Makes the banner or MREC view of one of this network's plans.</summary>
        IViewAd CreateView(HDCAdPlan plan);

        /// <summary>Makes the popup of one of this network's plans.</summary>
        IPopupAd CreatePopup(HDCAdPlan plan);
    }
}
