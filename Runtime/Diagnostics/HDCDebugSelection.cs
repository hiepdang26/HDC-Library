using System;
using UnityEngine;

namespace HDC.Ads.Diagnostics
{
    /// <summary>What a debug panel button acts on: the group and position picked, and where popups show.</summary>
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

        /// <summary>The panel's area for ads placed over the screen, such as popups; null without one.</summary>
        internal RectTransform Area { get; }

        /// <summary>Shows a call as the panel's last call, for what happens later, such as a callback.</summary>
        internal Action<string> Record { get; }
    }
}
