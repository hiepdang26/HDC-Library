using System;
using System.Collections.Generic;
using HDC.Ads.Diagnostics;
using HDC.Ads.Domain;

namespace HDC.Ads.Logic
{
    internal sealed partial class HDCRewardedAds
    {
        private static readonly IConfigRule Rules = new ConfigRules();
        private DebugModule debug;

        IChannelDiagnostics IAdChannel.Diagnostics => debug ?? (debug = new DebugModule(this));

        IConfigRule IAdChannel.ConfigRule => Rules;

        // The debug panel's view of the channel. It reads the rewarded group without making it.
        private sealed class DebugModule : HDCChannelDiagnostics
        {
            // The position the panel's Show button reports.
            private const string DebugPosition = "debug_rw";

            private readonly HDCRewardedAds channel;

            internal DebugModule(HDCRewardedAds channel)
                : base(channel.context)
            {
                this.channel = channel;
                Actions = new[]
                {
                    new HDCDebugAction("Init", "Init", true, _ =>
                    {
                        channel.Initialize();
                        return "Rewarded.Initialize()";
                    }),
                    new HDCDebugAction("Show", "Show", true, selection =>
                    {
                        string at = selection.Position.Length > 0 ? selection.Position : DebugPosition;
                        bool shown = channel.Show(at, () => selection.Record("Rewarded: reward earned"), () => selection.Record("Rewarded: closed"));
                        return $"Rewarded.Show(\"{at}\") -> {shown}";
                    }),
                };
            }

            public override IReadOnlyList<HDCDebugAction> Actions { get; }

            public override IReadOnlyList<(string Label, string Call)> Api { get; } = new[]
            {
                ("Init", "HDCAds.Rewarded.Initialize()"),
                ("Show", "HDCAds.Rewarded.Show(position, onRewarded, onClosed)"),
                ("Ready", "HDCAds.Rewarded.CanShow"),
            };

            public override string Hint => $"Show dùng position {DebugPosition}.";

            public override IReadOnlyList<string> Positions(string group) => new[] { DebugPosition };

            public override List<HDCDebugGroup> UnitGroups(string group, string position, bool askNative)
            {
                HDCAdCoreConfig.FullscreenUnit config = Context.CoreConfig.rewardedUnit ?? new HDCAdCoreConfig.FullscreenUnit();
                HDCDebugGroup rewarded = FullscreenGroup("rewarded", Context.Groups.ExistingRewardedGroup, true, Context.Groups.RewardedPlans(), details => details
                    .Line("Priority", Priority(config.mediationPriority))
                    .Line("Backup", config.useBackup));
                rewarded.Kind = "Channel";
                // Ads of other formats serve as rewarded too, such as native full-screen ones.
                foreach (HDCDebugUnit unit in rewarded.Units)
                {
                    if (unit.Format != HDCAdFormat.Rewarded)
                        unit.Name += " (Rewarded)";
                }

                return new List<HDCDebugGroup> { rewarded };
            }

            public override HDCDebugInfo Describe(string group, string position)
            {
                HDCFullscreenGroup made = Context.Groups.ExistingRewardedGroup;
                HDCDebugInfo info = new HDCDebugInfo()
                    .Section("Configs")
                    .Needed("Enabled", channel.IsEnabled)
                    .Line("Auto Init", channel.AutoInit)
                    .Section("Runtime")
                    .Line("Ignore Ads", channel.IgnoreAds)
                    .Line("Can Show", channel.IsEnabled && made != null && made.IsReady)
                    .Line("Impressions", channel.ImpressionCount)
                    .Section("Gates")
                    .Gate("Disabled", !channel.IsEnabled)
                    .Section("Group");
                if (made != null)
                    made.DescribeTo(info);
                else
                    info.Add("State", "Not started", HDCDebugTone.Muted);
                return info;
            }

            public override void MapUnits(HDCDebugInfo map)
            {
                HDCAdCoreConfig.FullscreenUnit unit = Context.CoreConfig.rewardedUnit ?? new HDCAdCoreConfig.FullscreenUnit();
                map.Section("Rewarded · RW")
                    .Switch("Enabled", Context.Config.rewardedChannel?.isEnabled ?? false)
                    .Line("Priority", FirstNetwork(unit.mediationPriority, unit.useBackup))
                    .Line("AdMob Unit", Dash(unit.admobUnit?.id))
                    .Line("Native Unit", Dash(unit.androidUnit?.id));
            }

            public override bool Owns(string instanceId) => instanceId.StartsWith(HDCAdNames.RewardedPrefix, StringComparison.Ordinal);
        }

        private sealed class ConfigRules : IConfigRule
        {
            public IEnumerable<HDCConfigFinding> Check(HDCAdsConfig ads, HDCAdCoreConfig core)
            {
                HDCAdCoreConfig.FullscreenUnit unit = core.rewardedUnit;
                bool hasUnit = unit != null && (!string.IsNullOrEmpty(unit.admobUnit?.id) || !string.IsNullOrEmpty(unit.androidUnit?.id));
                if ((ads.rewardedChannel?.isEnabled ?? false) && !hasUnit)
                    yield return HDCConfigFinding.Error("RW: rewardedUnit không có ad unit nào (admobUnit.id và androidUnit.id đều trống).");
            }

            public IEnumerable<string> Positions(HDCAdsConfig ads) => new string[0];
        }
    }
}
