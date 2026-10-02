using System.Collections.Generic;

namespace HDC.Ads.Diagnostics
{
    internal interface IChannelDiagnostics
    {
        IReadOnlyList<string> Groups();

        IReadOnlyList<string> Positions(string group);

        string PositionLabel { get; }

        string Hint { get; }

        string PositionHint { get; }

        bool UsesArea { get; }

        IReadOnlyList<HDCDebugAction> Actions { get; }

        IReadOnlyList<(string Label, string Call)> Api { get; }

        List<HDCDebugGroup> UnitGroups(string group, string position, bool askNative);

        HDCDebugInfo Describe(string group, string position);

        void MapUnits(HDCDebugInfo map);

        bool Owns(string instanceId);
    }
}
