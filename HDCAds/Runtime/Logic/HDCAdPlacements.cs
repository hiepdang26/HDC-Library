using System;
using System.Collections.Generic;

namespace HDC.Ads.Logic
{
    internal sealed class HDCAdPlacements
    {
        private readonly Dictionary<string, (HDCAdChannel Channel, string Position, string Network, string Group)> placements =
            new Dictionary<string, (HDCAdChannel, string, string, string)>(StringComparer.Ordinal);

        internal void Record(string instanceId, HDCAdChannel channel, string position, string network, string group = "")
        {
            if (!string.IsNullOrEmpty(instanceId))
                placements[instanceId] = (channel, position ?? "", network, group ?? "");
        }

        internal (HDCAdChannel Channel, string Position, string Network, string Group) Find(string instanceId) =>
            instanceId != null && placements.TryGetValue(instanceId, out (HDCAdChannel, string, string, string) placement)
                ? placement
                : (HDCAdChannel.Unknown, "", null, "");
    }
}
