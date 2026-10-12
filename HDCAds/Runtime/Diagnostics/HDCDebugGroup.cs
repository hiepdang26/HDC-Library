using System.Collections.Generic;

namespace HDC.Ads.Diagnostics
{
    internal sealed class HDCDebugGroup
    {
        internal string Kind = "Group";

        internal string Name;
        internal bool Selected;
        internal readonly HDCDebugInfo Details = new HDCDebugInfo();
        internal readonly List<HDCDebugUnit> Units = new List<HDCDebugUnit>();
    }
}
