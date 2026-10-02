using System.Collections.Generic;
using System.Linq;

namespace HDC.Ads.Diagnostics
{
    internal static class HDCMediationReport
    {
        private static readonly List<HDCAdapterStatus> adapters = new List<HDCAdapterStatus>();

        internal static bool Received { get; private set; }

        internal static IReadOnlyList<HDCAdapterStatus> Adapters => adapters;

        internal static HDCAdapterStatus Find(string adapterClass) =>
            adapters.FirstOrDefault(adapter => adapter.AdapterClass == adapterClass);

        internal static void AdaptersStarted(IEnumerable<HDCAdapterStatus> statuses)
        {
            adapters.Clear();
            adapters.AddRange(statuses);
            Received = true;
        }

        internal static void Reset()
        {
            adapters.Clear();
            Received = false;
        }
    }
}
