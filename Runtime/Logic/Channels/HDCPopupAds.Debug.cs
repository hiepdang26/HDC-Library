using System;
using System.Collections.Generic;
using System.Linq;
using HDC.Ads.Diagnostics;
using HDC.Ads.Domain;
using HDC.Ads.Ports;

namespace HDC.Ads.Logic
{
    internal sealed partial class HDCPopupAds
    {
        private static readonly IConfigRule Rules = new ConfigRules();
        private DebugModule debug;

        IChannelDiagnostics IAdChannel.Diagnostics => debug ?? (debug = new DebugModule(this));

        IConfigRule IAdChannel.ConfigRule => Rules;

        // The debug panel's view of the channel: its groups, and the positions of each. Popups show over the
        // panel's popup area. It reads the popups without making them.
        private sealed class DebugModule : HDCChannelDiagnostics
        {
            private readonly HDCPopupAds channel;

            internal DebugModule(HDCPopupAds channel)
                : base(channel.context)
            {
                this.channel = channel;
                Actions = new[]
                {
                    new HDCDebugAction("Init", "Init", true, selection =>
                    {
                        if (selection.Group.Length == 0)
                            return null;
                        channel.Initialize(selection.Group);
                        return $"Popup.Initialize(\"{selection.Group}\")";
                    }),
                    new HDCDebugAction("Show", "Show", true, selection =>
                    {
                        if (selection.Position.Length == 0)
                            return null;
                        Place(selection);
                        return $"Popup.Show(\"{selection.Position}\") -> {channel.Show(selection.Position)}";
                    }),
                    new HDCDebugAction("Hide", "Hide", false, selection =>
                    {
                        if (selection.Position.Length == 0)
                            return null;
                        channel.Hide(selection.Position);
                        return $"Popup.Hide(\"{selection.Position}\")";
                    }),
                    new HDCDebugAction("Update Position", "UpdatePos", false, selection =>
                    {
                        Place(selection);
                        return null;
                    }),
                };
            }

            public override IReadOnlyList<HDCDebugAction> Actions { get; }

            public override IReadOnlyList<(string Label, string Call)> Api { get; } = new[]
            {
                ("Init", "HDCAds.Popup.Initialize(group)"),
                ("Place", "HDCAds.Popup.Move(position, area)"),
                ("Show / Hide", "HDCAds.Popup.Show(position), Hide(position)"),
            };

            public override string Hint => "Chọn group, rồi position của group đó.";

            public override bool UsesArea => true;

            private HDCAdCoreConfig.PopupGroup[] Configs => Context.CoreConfig.popupGroups ?? new HDCAdCoreConfig.PopupGroup[0];

            public override IReadOnlyList<string> Groups() =>
                Context.IsInitialized ? Distinct(Configs.Select(config => config?.groupName)) : new string[0];

            public override IReadOnlyList<string> Positions(string group) =>
                Context.IsInitialized
                    ? Distinct((channel.Channel.positionConfigs ?? new HDCAdsConfig.PopupPosition[0])
                        .Select(config => config?.positionName)
                        .Where(position => string.IsNullOrEmpty(group) || Context.CoreConfig.PopupGroupAt(position) == group))
                    : new string[0];

            public override List<HDCDebugGroup> UnitGroups(string group, string position, bool askNative) =>
                Configs.Where(config => config != null && !string.IsNullOrEmpty(config.groupName))
                    .Select(config => PopupGroup(config, config.groupName == group, askNative && config.groupName == group))
                    .ToList();

            public override HDCDebugInfo Describe(string group, string position)
            {
                HDCDebugInfo info = new HDCDebugInfo()
                    .Section("Configs")
                    .Needed("Enabled", channel.Channel.isEnabled)
                    .Line("Positions", channel.Channel.positionConfigs?.Length ?? 0)
                    .Line("Auto Init Groups", string.Join(", ", channel.AutoInitGroups()))
                    .Section("Gates")
                    .Gate("Disabled", channel.IsDisabled)
                    .Gate("Ads Removed", Context.IsAdsRemoved);

                if (!string.IsNullOrEmpty(position))
                {
                    bool allowed = channel.Allowed(position, out string reason);
                    info.Section("Position " + position)
                        .Line("Group", Context.CoreConfig.PopupGroupAt(position))
                        .Add("Can Show Here", allowed ? "Yes" : "No · " + reason, allowed ? HDCDebugTone.Good : HDCDebugTone.Bad);
                }

                info.Section("Group " + (string.IsNullOrEmpty(group) ? "-" : group));
                if (string.IsNullOrEmpty(group) || !channel.popups.TryGetValue(group, out Popup popup))
                    return info.Add("State", "Not started", HDCDebugTone.Muted);

                return info
                    .Line("Instance ID", popup.Ad.Id)
                    .Line("Requested", popup.Ad.IsRequested)
                    .Needed("Placed (Move)", popup.Ad.IsPlaced)
                    .Line("Native State", popup.Ad.State ?? "-");
            }

            public override void MapUnits(HDCDebugInfo map)
            {
                foreach (HDCAdCoreConfig.PopupGroup group in Configs)
                {
                    if (group == null)
                        continue;
                    map.Section("Popup · " + group.groupName)
                        .Line("Positions", Join(group.positionNames))
                        .Line("Native Unit", Dash(group.androidUnit?.id))
                        .Line("Layout", Dash(group.androidUnit?.layout));
                }
            }

            public override bool Owns(string instanceId) => instanceId.StartsWith(HDCAdNames.PopupPrefix, StringComparison.Ordinal);

            // Places the position's popup over the panel's popup area.
            private void Place(HDCDebugSelection selection)
            {
                if (selection.Area == null || selection.Position.Length == 0)
                    return;
                channel.Move(selection.Position, selection.Area);
                selection.Record($"Popup.Move(\"{selection.Position}\", popup area)");
            }

            private HDCDebugGroup PopupGroup(HDCAdCoreConfig.PopupGroup config, bool selected, bool askNative)
            {
                bool created = channel.popups.TryGetValue(config.groupName, out Popup popup);
                HDCAdPlan plan = created ? null : Context.Groups.PopupPlan(config.groupName);
                string id = popup?.Ad.Id ?? plan?.InstanceId ?? HDCAdNames.NativePopup(config.groupName);
                HDCAdRecord record = HDCAdsTracker.Find(HDCAdFormat.Popup, id);
                bool requested = popup?.Ad.IsRequested ?? false;
                string nativeState = requested && askNative ? popup.Ad.State : null;
                var group = new HDCDebugGroup { Name = config.groupName, Selected = selected };
                group.Details
                    .Line("Positions", Join(config.positionNames))
                    .Needed("Placed (Move)", popup?.Ad.IsPlaced ?? false)
                    .Line("Reload After Show", !config.disablePostInitReload)
                    .Line("Layout", config.androidUnit?.layout)
                    .Line("Native State", nativeState ?? "-");
                group.Units.Add(new HDCDebugUnit
                {
                    Index = 1,
                    Name = UnitName((popup?.Network ?? plan?.Network)?.Name, HDCAdFormat.Popup),
                    Format = HDCAdFormat.Popup,
                    Id = id,
                    AdUnitId = record?.AdUnitId ?? config.androidUnit?.id,
                    Created = created,
                    Started = requested,
                    Ready = requested && (nativeState == "Displayable" || nativeState == "Loaded"),
                    OnScreen = nativeState == "Showing",
                    NativeState = nativeState,
                    Record = record,
                });
                return group;
            }
        }

        private sealed class ConfigRules : IConfigRule
        {
            public IEnumerable<HDCConfigFinding> Check(HDCAdsConfig ads, HDCAdCoreConfig core)
            {
                if (!(ads.popupChannel?.isEnabled ?? false))
                    yield break;
                HDCAdCoreConfig.PopupGroup[] groups = (core.popupGroups ?? new HDCAdCoreConfig.PopupGroup[0]).Where(group => group != null).ToArray();
                if (groups.Length == 0)
                    yield return HDCConfigFinding.Error("PU: bật popupChannel nhưng ad core config không có popupGroups.");
                foreach (string position in Positions(ads).Where(p => !string.IsNullOrEmpty(p) && string.IsNullOrEmpty(core.PopupGroupAt(p))))
                    yield return HDCConfigFinding.Error($"PU: position '{position}' không thuộc popup group nào.");
                foreach (HDCAdCoreConfig.PopupGroup group in groups.Where(g => string.IsNullOrEmpty(g.androidUnit?.id)))
                    yield return HDCConfigFinding.Error($"PU: group '{group.groupName}' không có androidUnit.id.");
            }

            public IEnumerable<string> Positions(HDCAdsConfig ads) =>
                (ads.popupChannel?.positionConfigs ?? new HDCAdsConfig.PopupPosition[0]).Where(config => config != null).Select(config => config.positionName);
        }
    }
}
