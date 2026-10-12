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

        private sealed class DebugModule : HDCChannelDiagnostics
        {
            private readonly HDCBannerAds channel;

            internal DebugModule(HDCBannerAds channel)
                : base(channel.context)
            {
                this.channel = channel;
                Actions = new[]
                {
                    new HDCDebugAction("Init", "Init", true, _ =>
                    {
                        channel.Initialize();
                        return "Banner.Initialize()";
                    }),
                    new HDCDebugAction("Show", "Activate", true, _ => $"Banner.Show() -> {channel.Show()}"),
                    new HDCDebugAction("Hide", "Hide", false, _ =>
                    {
                        channel.Hide();
                        return "Banner.Hide()";
                    }),
                };
            }

            public override IReadOnlyList<HDCDebugAction> Actions { get; }

            public override IReadOnlyList<(string Label, string Call)> Api { get; } = new[]
            {
                ("Init", "HDCAds.Banner.Initialize()"),
                ("Show / Hide", "HDCAds.Banner.Show(), Hide()"),
            };

            public override string PositionLabel => "Placement";

            public override string Hint => "Banner chỉ có một vị trí: FullBottom.";

            public override List<HDCDebugGroup> UnitGroups(string group, string position, bool askNative)
            {
                HDCAdCoreConfig.FullscreenUnit unit = Unit();
                HDCAdsConfig.BannerSlot config = channel.Slot;
                HDCDebugGroup banner = RectGroup(Placement, channel.group, true, Context.Groups.BannerPlans(), details => details
                    .Needed("Enabled", channel.IsEnabled)
                    .Line("Auto Init", config.autoInit)
                    .Line("Auto Show On Load", config.autoShowOnLoad)
                    .Line("Priority", Priority(unit.mediationPriority))
                    .Line("Backup", unit.useBackup));
                banner.Kind = "Placement";
                return new List<HDCDebugGroup> { banner };
            }

            public override HDCDebugInfo Describe(string group, string position)
            {
                HDCAdsConfig.BannerSlot config = channel.Slot;
                HDCDebugInfo info = new HDCDebugInfo()
                    .Section("Configs")
                    .Needed("Channel Enabled", channel.Channel.isEnabled)
                    .Needed("FullBottom Enabled", config.isEnabled)
                    .Line("Auto Init", config.autoInit)
                    .Line("Auto Show On Load", config.autoShowOnLoad)
                    .Section("Runtime")
                    .Line("Can Show", channel.CanShow())
                    .Section("Gates")
                    .Gate("Disabled", !channel.IsActive)
                    .Gate("Ads Removed", Context.IsAdsRemoved)
                    .Section("Placement " + Placement);
                if (channel.group != null)
                    channel.group.DescribeTo(info);
                else
                    info.Add("State", "Not started", HDCDebugTone.Muted);
                return info;
            }

            public override void MapUnits(HDCDebugInfo map)
            {
                HDCAdCoreConfig.FullscreenUnit unit = Unit();
                bool enabled = channel.Channel.isEnabled && channel.Slot.isEnabled;
                if (!enabled && string.IsNullOrEmpty(unit.admobUnit?.id) && string.IsNullOrEmpty(unit.nativeAdUnit?.id))
                    return;
                map.Section("Banner · " + Placement)
                    .Switch("Enabled", enabled)
                    .Line("Priority", FirstNetwork(unit.mediationPriority, unit.useBackup))
                    .Line("AdMob Unit", Dash(unit.admobUnit?.id))
                    .Line("Native Units", Join(new[] { unit.nativeAdUnit?.id }.Concat(unit.nativeAdUnit?.ids ?? new string[0])));
            }

            public override bool Owns(string instanceId) => instanceId.StartsWith(HDCAdNames.BannerPrefix, StringComparison.Ordinal);

            private HDCAdCoreConfig.FullscreenUnit Unit() => Context.CoreConfig.bannerUnit?.fullBottom ?? new HDCAdCoreConfig.FullscreenUnit();
        }

        private sealed class ConfigRules : IConfigRule
        {
            public IEnumerable<HDCConfigFinding> Check(HDCAdsConfig ads, HDCAdCoreConfig core)
            {
                if (!(ads.bannerChannel?.isEnabled ?? false) || !(ads.bannerChannel.fullBottom?.isEnabled ?? false))
                    yield break;
                HDCAdCoreConfig.FullscreenUnit unit = core.bannerUnit?.fullBottom;
                bool native = unit?.nativeAdUnit != null
                    && (!string.IsNullOrEmpty(unit.nativeAdUnit.id) || (unit.nativeAdUnit.ids?.Length ?? 0) > 0);
                if (string.IsNullOrEmpty(unit?.admobUnit?.id) && !native)
                    yield return HDCConfigFinding.Error("BN: fullBottom đang bật nhưng bannerUnit.fullBottom không có ad unit.");
            }

            public IEnumerable<string> Positions(HDCAdsConfig ads) => new string[0];
        }
    }
}
