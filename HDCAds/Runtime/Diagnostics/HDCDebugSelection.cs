using System;
using UnityEngine;

namespace HDC.Ads.Diagnostics
{
    internal sealed class HDCDebugSelection
    {
        internal HDCDebugSelection(string group, string position, RectTransform area, Action<string> record)
        {
            Group = group ?? string.Empty;
            Position = position ?? string.Empty;
            Area = area;
            Record = record;
        }

        internal string Group { get; }
        internal string Position { get; }

        internal RectTransform Area { get; }

        internal Action<string> Record { get; }
    }
}
