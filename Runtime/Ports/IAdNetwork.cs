using HDC.Ads.Domain;

namespace HDC.Ads.Ports
{
    internal interface IAdNetwork
    {
        string UnitKey { get; }

        string Name { get; }

        string RevenueNetwork { get; }

        HDCAdPlan Plan(HDCAdUse use, HDCAdUnitSpec spec);

        IFullscreenAd CreateFullscreen(HDCAdPlan plan);

        IViewAd CreateView(HDCAdPlan plan);

        IPopupAd CreatePopup(HDCAdPlan plan);
    }
}
