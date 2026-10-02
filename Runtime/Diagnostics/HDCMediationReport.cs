using System.Collections.Generic;
using System.Linq;

namespace HDC.Ads.Diagnostics
{
    /// <summary>How Google Mobile Ads started its mediation adapters, for the debug panel.</summary>
    internal static class HDCMediationReport
    {
        private static readonly List<HDCAdapterStatus> adapters = new List<HDCAdapterStatus>();

        /// <summary>True once Google Mobile Ads reported its start.</summary>
        internal static bool Received { get; private set; }

        /// <summary>The adapters in the order Google Mobile Ads listed them.</summary>
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
