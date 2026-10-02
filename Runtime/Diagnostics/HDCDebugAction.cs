using System;

namespace HDC.Ads.Diagnostics
{
    /// <summary>A debug panel button of a channel, which calls the channel's API.</summary>
    internal sealed class HDCDebugAction
    {
        /// <param name="name">What it does, such as "Update Position"; its button is named after it.</param>
        /// <param name="label">The button's text.</param>
        /// <param name="primary">Drawn in the accent color, for the channel's main calls.</param>
        /// <param name="run">Makes the call and returns it as text for the panel, or null when it made none.</param>
        internal HDCDebugAction(string name, string label, bool primary, Func<HDCDebugSelection, string> run)
        {
            Name = name;
            Label = label;
            Primary = primary;
            Run = run;
        }

        internal string Name { get; }
        internal string Label { get; }
        internal bool Primary { get; }
        internal Func<HDCDebugSelection, string> Run { get; }
    }
}
