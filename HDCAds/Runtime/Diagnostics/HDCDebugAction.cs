using System;

namespace HDC.Ads.Diagnostics
{
    internal sealed class HDCDebugAction
    {
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
