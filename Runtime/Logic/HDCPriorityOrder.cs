using System.Collections.Generic;
using HDC.Ads.Domain;

namespace HDC.Ads.Logic
{
    internal sealed class HDCPriorityOrder : IUnitOrderPolicy
    {
        private static readonly string[] Keys = { HDCAdUnitKeys.AdMob, HDCAdUnitKeys.Native, null };

        private static readonly int[] BackupOrder = { 0, 2, 1 };

        public string KeyFor(int mediationPriority) =>
            mediationPriority >= 0 && mediationPriority < Keys.Length ? Keys[mediationPriority] : null;

        public IEnumerable<string> Order(int mediationPriority, bool useBackup)
        {
            var priorities = new List<int> { mediationPriority };
            if (useBackup)
            {
                foreach (int backup in BackupOrder)
                {
                    if (!priorities.Contains(backup))
                        priorities.Add(backup);
                }
            }

            var keys = new List<string>();
            foreach (int priority in priorities)
            {
                string key = KeyFor(priority);
                if (key != null)
                    keys.Add(key);
            }

            return keys;
        }
    }
}
