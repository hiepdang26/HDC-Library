using System.Collections.Generic;

namespace HDC.Ads.Diagnostics
{
    /// <summary>A group, slot or channel, for the debug panel: its settings and state, and its ad units in order.</summary>
    internal sealed class HDCDebugGroup
    {
        /// <summary>What the panel calls it: Group, Placement or Channel.</summary>
        internal string Kind = "Group";

        internal string Name;
        internal bool Selected;
        internal readonly HDCDebugInfo Details = new HDCDebugInfo();
        internal readonly List<HDCDebugUnit> Units = new List<HDCDebugUnit>();
    }
}
