using System;
using System.Collections.Generic;

namespace HDC.Ads.Logic
{
    internal sealed class HDCAdPlacements
    {
        private readonly Dictionary<string, (HDCAdChannel Channel, string Position, string Network)> placements =
            new Dictionary<string, (HDCAdChannel, string, string)>(StringComparer.Ordinal);

        internal void Record(string instanceId, HDCAdChannel channel, string position, string network)
        {
            if (!string.IsNullOrEmpty(instanceId))
                placements[instanceId] = (channel, position ?? "", network);
        }

        internal (HDCAdChannel Channel, string Position, string Network) Find(string instanceId) =>
            instanceId != null && placements.TryGetValue(instanceId, out (HDCAdChannel, string, string) placement)
                ? placement
                : (HDCAdChannel.Unknown, "", null);
    }
}
