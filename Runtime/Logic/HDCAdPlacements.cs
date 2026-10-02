using System;
using System.Collections.Generic;

namespace HDC.Ads
{
    /// <summary>
    /// The channel and position each ad instance last showed for, so a paid event can tell where its money came
    /// from. Channels record it right before a show; banners and the MREC when they make their units, since
    /// their views earn for as long as they stay on screen.
    /// </summary>
    internal static class HDCAdPlacements
    {
        private static readonly Dictionary<string, (HDCAdChannel Channel, string Position)> placements =
            new Dictionary<string, (HDCAdChannel, string)>(StringComparer.Ordinal);

        internal static void Record(string instanceId, HDCAdChannel channel, string position = "")
        {
            if (!string.IsNullOrEmpty(instanceId))
                placements[instanceId] = (channel, position ?? "");
        }

        internal static (HDCAdChannel Channel, string Position) Find(string instanceId) =>
            instanceId != null && placements.TryGetValue(instanceId, out (HDCAdChannel, string) placement)
                ? placement
                : (HDCAdChannel.Unknown, "");

        internal static void Reset() => placements.Clear();
    }
}
