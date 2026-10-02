using System.Collections.Generic;
using HDC.Ads.Domain;

namespace HDC.Ads.Diagnostics
{
    /// <summary>A channel's checks of the configs, which the Remote Config page of the debug panel runs.</summary>
    internal interface IConfigRule
    {
        /// <summary>What is wrong in the channel's part of the configs, each with what to fix.</summary>
        IEnumerable<HDCConfigFinding> Check(HDCAdsConfig ads, HDCAdCoreConfig core);

        /// <summary>
        /// The positions the game shows the channel's ads at, as the ads config lists them, repeats included: the
        /// check reports a position listed twice, or by two channels.
        /// </summary>
        IEnumerable<string> Positions(HDCAdsConfig ads);
    }
}
