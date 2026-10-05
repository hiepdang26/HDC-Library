using System;

namespace HDC.Ads.Ports
{
    internal interface IFullscreenCompanion
    {
        IFullscreenAd Companion { get; }

        event Action CompanionOpening;
    }
}
