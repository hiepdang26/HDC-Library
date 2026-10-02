using System.Collections.Generic;

namespace HDC.Ads.Diagnostics
{
    /// <summary>
    /// What the debug panel shows and does for a channel: the groups and positions to pick, the buttons, the
    /// groups of ad units with their state, the channel's own state, its part of the ad units map and the events
    /// that are its own. The panel has no code of its own for any channel.
    /// </summary>
    internal interface IChannelDiagnostics
    {
        /// <summary>The groups to pick from, from the configs; none for a channel without groups.</summary>
        IReadOnlyList<string> Groups();

        /// <summary>The positions to pick from: those of <paramref name="group"/>, or every one when it is empty.</summary>
        IReadOnlyList<string> Positions(string group);

        /// <summary>What the panel calls a position, such as Placement for banners.</summary>
        string PositionLabel { get; }

        /// <summary>What to pick before pressing the buttons, in Vietnamese.</summary>
        string Hint { get; }

        /// <summary>The subtitle of the position picker, in Vietnamese.</summary>
        string PositionHint { get; }

        /// <summary>The channel shows ads over a part of the screen the panel gives it, such as popups.</summary>
        bool UsesArea { get; }

        IReadOnlyList<HDCDebugAction> Actions { get; }

        /// <summary>The HDCAds calls behind the buttons, for game code to copy.</summary>
        IReadOnlyList<(string Label, string Call)> Api { get; }

        /// <summary>
        /// The channel's groups with their ad units, the selected one marked. With <paramref name="askNative"/> the
        /// selected group may ask the native side for state its events do not carry, which costs a call.
        /// </summary>
        List<HDCDebugGroup> UnitGroups(string group, string position, bool askNative);

        /// <summary>The channel's configs, runtime state and gates, with the selected group and position.</summary>
        HDCDebugInfo Describe(string group, string position);

        /// <summary>Adds the channel's ad units and positions, as the configs set them, to the ad units map.</summary>
        void MapUnits(HDCDebugInfo map);

        /// <summary>True for the instance ids of the channel's ads, which their events carry.</summary>
        bool Owns(string instanceId);
    }
}
