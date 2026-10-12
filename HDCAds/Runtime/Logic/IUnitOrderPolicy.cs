using System.Collections.Generic;

namespace HDC.Ads.Logic
{
    internal interface IUnitOrderPolicy
    {
        IEnumerable<string> Order(int mediationPriority, bool useBackup);

        string KeyFor(int mediationPriority);
    }
}
