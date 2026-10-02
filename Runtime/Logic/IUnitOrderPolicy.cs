using System.Collections.Generic;

namespace HDC.Ads.Logic
{
    /// <summary>The order a slot's networks load and show in, from the slot's mediationPriority and useBackup.</summary>
    internal interface IUnitOrderPolicy
    {
        /// <summary>Network unit keys, first to last: the slot's pick, then with backups the others.</summary>
        IEnumerable<string> Order(int mediationPriority, bool useBackup);

        /// <summary>The unit key a mediationPriority value picks; null for a value no network serves.</summary>
        string KeyFor(int mediationPriority);
    }
}
