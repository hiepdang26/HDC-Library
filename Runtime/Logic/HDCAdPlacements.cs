using System;
using System.Collections.Generic;

namespace HDC.Ads.Logic
{
    /// <summary>
    /// The channel, position and network each ad instance last showed for, so a paid event can tell where its
    /// money came from. Channels record it right before a show; banners and the MREC when they make their units,
    /// since their views earn for as long as they stay on screen.
    /// </summary>
    internal sealed class HDCAdPlacements
    {
        private readonly Dictionary<string, (HDCAdChannel Channel, string Position, string Network)> placements =
            new Dictionary<string, (HDCAdChannel, string, string)>(StringComparer.Ordinal);

        internal void Record(string instanceId, HDCAdChannel channel, string position, string network)
        {
            if (!string.IsNullOrEmpty(instanceId))
                placements[instanceId] = (channel, position ?? "", network);
        }

        /// <summary>The placement of an instance; for one nobody recorded, an unknown channel and no network.</summary>
        internal (HDCAdChannel Channel, string Position, string Network) Find(string instanceId) =>
            instanceId != null && placements.TryGetValue(instanceId, out (HDCAdChannel, string, string) placement)
                ? placement
                : (HDCAdChannel.Unknown, "", null);
    }
}
