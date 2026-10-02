using System.Collections.Generic;
using HDC.Ads.Domain;

namespace HDC.Ads.Diagnostics
{
    internal interface IConfigRule
    {
        IEnumerable<HDCConfigFinding> Check(HDCAdsConfig ads, HDCAdCoreConfig core);

        IEnumerable<string> Positions(HDCAdsConfig ads);
    }
}
