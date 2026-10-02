using System;
using System.Collections.Generic;
using System.Linq;
using HDC.Ads.Diagnostics;
using HDC.Ads.Domain;

namespace HDC.Ads.Logic
{
    internal sealed partial class HDCBannerAds
    {
        private static readonly IConfigRule Rules = new ConfigRules();
        private DebugModule debug;

        IChannelDiagnostics IAdChannel.Diagnostics => debug ?? (debug = new DebugModule(this));

        IConfigRule IAdChannel.ConfigRule => Rules;

        // The debug panel's view of the channel: one placement per banner slot. It reads the slots' groups without
        // making them.
        private sealed class DebugModule : HDCChannelDiagnostics
        {
            private static readonly string[] SlotNames = Enum.GetNames(typeof(HDCBannerSlot));

            private readonly HDCBannerAds channel;

            internal DebugModule(HDCBannerAds channel)
                : base(channel.context)
            {
                this.channel = channel;
                Actions = new[]
                {
                    new HDCDebugAction("Init", "Init", true, selection =>
                    {
                        HDCBannerSlot slot = Slot(selection.Position);
                        channel.Initialize(slot);
                        return $"Banner.Initialize({slot})";
                    }),
                    new HDCDebugAction("Show", "Activate", true, selection =>
                    {
                        HDCBannerSlot slot = Slot(selection.Position);
                        return $"Banner.Show({slot}) -> {channel.Show(slot)}";
                    }),
                    new HDCDebugAction("Hide", "Hide", false, selection =>
                    {
                        HDCBannerSlot slot = Slot(selection.Position);
                        channel.Hide(slot);
                        return $"Banner.Hide({slot})";
                    }),
                };
            }

            public override IReadOnlyList<HDCDebugAction> Actions { get; }

            public override IReadOnlyList<(string Label, string Call)> Api { get; } = new[]
            {
                ("Init", "HDCAds.Banner.Initialize(slot)"),
                ("Show / Hide", "HDCAds.Banner.Show(slot), Hide(slot)"),
            };

            public override string PositionLabel => "Placement";

            public override string Hint => "Chọn placement: một trong sáu vị trí banner.";

            public override string PositionHint => "Chọn vị trí banner.";

            public override IReadOnlyList<string> Positions(string group) => SlotNames;

            public override List<HDCDebugGroup> UnitGroups(string group, string position, bool askNative) =>
                Slots.Select(slot => SlotGroup(slot, slot.ToString() == position)).ToList();

            public override HDCDebugInfo Describe(string group, string position)
            {
                HDCBannerSlot slot = Slot(position);
                HDCAdsConfig.BannerSlot config = channel.Channel.Slot(slot);
                HDCDebugInfo info = new HDCDebugInfo()
                    .Section("Configs")
                    .Needed("Channel Enabled", channel.Channel.isEnabled)
                    .Needed("Slot Enabled", config.isEnabled)
                    .Line("Auto Init", config.autoInit)
                    .Line("Auto Show On Load", config.autoShowOnLoad)
                    .Section("Runtime")
                    .Line("Can Show", channel.CanShow(slot))
                    .Section("Gates")
                    .Gate("Disabled", !channel.IsEnabled(slot))
                    .Gate("Ads Removed", Context.IsAdsRemoved)
                    .Section("Placement " + slot);
                if (channel.groups.TryGetValue(slot, out HDCRectGroup made))
                    made.DescribeTo(info);
                else
                    info.Add("State", "Not started", HDCDebugTone.Muted);
                return info;
            }

            public override void MapUnits(HDCDebugInfo map)
            {
                HDCAdsConfig.BannerChannel config = Context.Config.bannerChannel;
                foreach (HDCBannerSlot slot in Slots)
                {
                    HDCAdCoreConfig.FullscreenUnit unit = Context.CoreConfig.bannerUnit?.Slot(slot) ?? new HDCAdCoreConfig.FullscreenUnit();
                    bool enabled = (config?.isEnabled ?? false) && (config?.Slot(slot).isEnabled ?? false);
                    if (!enabled && string.IsNullOrEmpty(unit.admobUnit?.id) && string.IsNullOrEmpty(unit.androidUnit?.id))
                        continue;
                    map.Section("Banner · " + slot)
                        .Switch("Enabled", enabled)
                        .Line("Priority", FirstNetwork(unit.mediationPriority, unit.useBackup))
                        .Line("AdMob Unit", Dash(unit.admobUnit?.id));
                    if (slot == HDCBannerSlot.FullBottom)
                        map.Line("Native Units", Join(new[] { unit.androidUnit?.id }.Concat(unit.androidUnit?.ids ?? new string[0])));
                }
            }

            public override bool Owns(string instanceId) => instanceId.StartsWith(HDCAdNames.BannerPrefix, StringComparison.Ordinal);

            private static HDCBannerSlot Slot(string position) => Enum.TryParse(position, out HDCBannerSlot slot) ? slot : HDCBannerSlot.FullBottom;

            private HDCDebugGroup SlotGroup(HDCBannerSlot slot, bool selected)
            {
                HDCAdCoreConfig.FullscreenUnit unit = Context.CoreConfig.bannerUnit?.Slot(slot) ?? new HDCAdCoreConfig.FullscreenUnit();
                HDCAdsConfig.BannerSlot config = channel.Channel.Slot(slot);
                HDCDebugGroup group = RectGroup(slot.ToString(), channel.groups.TryGetValue(slot, out HDCRectGroup made) ? made : null, selected,
                    Context.Groups.BannerPlans(slot), details => details
                        .Needed("Enabled", channel.IsEnabled(slot))
                        .Line("Auto Init", config.autoInit)
                        .Line("Auto Show On Load", config.autoShowOnLoad)
                        .Line("Priority", Priority(unit.mediationPriority))
                        .Line("Backup", unit.useBackup));
                group.Kind = "Placement";
                return group;
            }
        }

        private sealed class ConfigRules : IConfigRule
        {
            public IEnumerable<HDCConfigFinding> Check(HDCAdsConfig ads, HDCAdCoreConfig core)
            {
                if (!(ads.bannerChannel?.isEnabled ?? false))
                    yield break;
                foreach (HDCBannerSlot slot in Slots)
                {
                    if (!ads.bannerChannel.Slot(slot).isEnabled)
                        continue;
                    HDCAdCoreConfig.FullscreenUnit unit = core.bannerUnit?.Slot(slot);
                    bool native = slot == HDCBannerSlot.FullBottom && unit?.androidUnit != null
                        && (!string.IsNullOrEmpty(unit.androidUnit.id) || (unit.androidUnit.ids?.Length ?? 0) > 0);
                    if (string.IsNullOrEmpty(unit?.admobUnit?.id) && !native)
                        yield return HDCConfigFinding.Error($"BN: slot {slot} đang bật nhưng bannerUnit không có ad unit cho slot này.");
                }
            }

            public IEnumerable<string> Positions(HDCAdsConfig ads) => new string[0];
        }
    }
}
