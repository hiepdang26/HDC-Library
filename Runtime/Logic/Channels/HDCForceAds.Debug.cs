using System;
using System.Collections.Generic;
using System.Linq;
using HDC.Ads.Diagnostics;
using HDC.Ads.Domain;

namespace HDC.Ads.Logic
{
    internal sealed partial class HDCForceAds
    {
        private static readonly IConfigRule Rules = new ConfigRules();
        private DebugModule debug;

        IChannelDiagnostics IAdChannel.Diagnostics => debug ?? (debug = new DebugModule(this));

        IConfigRule IAdChannel.ConfigRule => Rules;

        // The debug panel's view of the channel: its groups, and the positions of each. It reads the groups without
        // making them.
        private sealed class DebugModule : HDCChannelDiagnostics
        {
            private readonly HDCForceAds channel;

            internal DebugModule(HDCForceAds channel)
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
                        return $"ForceAd.Initialize(\"{selection.Group}\")";
                    }),
                    new HDCDebugAction("Show", "Show", true, selection =>
                    {
                        string at = selection.Position;
                        if (at.Length == 0)
                            return null;
                        bool shown = channel.Show(at, () => selection.Record($"ForceAd \"{at}\": done"));
                        return $"ForceAd.Show(\"{at}\") -> {shown}";
                    }),
                };
            }

            public override IReadOnlyList<HDCDebugAction> Actions { get; }

            public override IReadOnlyList<(string Label, string Call)> Api { get; } = new[]
            {
                ("Init", "HDCAds.ForceAd.Initialize(group)"),
                ("Show", "HDCAds.ForceAd.Show(position, onDone)"),
                ("Ready", "HDCAds.ForceAd.CanShow(position)"),
            };

            public override string Hint => "Chọn group, rồi position của group đó.";

            private HDCAdCoreConfig.ForceAdGroup[] Configs => Context.CoreConfig.forceAdGroups ?? new HDCAdCoreConfig.ForceAdGroup[0];

            public override IReadOnlyList<string> Groups() =>
                Context.IsInitialized ? Distinct(Configs.Select(config => config?.groupName)) : new string[0];

            public override IReadOnlyList<string> Positions(string group) =>
                Context.IsInitialized
                    ? Distinct((channel.Channel.positionConfigs ?? new HDCAdsConfig.ForceAdPosition[0])
                        .Select(config => config?.positionName)
                        .Where(position => string.IsNullOrEmpty(group) || Context.CoreConfig.ForceAdGroupAt(position) == group))
                    : new string[0];

            public override List<HDCDebugGroup> UnitGroups(string group, string position, bool askNative)
            {
                var groups = new List<HDCDebugGroup>();
                foreach (HDCAdCoreConfig.ForceAdGroup config in Configs)
                {
                    if (config == null || string.IsNullOrEmpty(config.groupName))
                        continue;
                    groups.Add(FullscreenGroup(config.groupName, Context.Groups.ExistingForceAdGroup(config.groupName), config.groupName == group,
                        Context.Groups.ForceAdPlans(config.groupName), details => details
                            .Line("Positions", Join(config.positionNames))
                            .Line("Priority", Priority(config.mediationPriority))
                            .Line("Backup", config.useBackup)
                            .Line("Max Shows", config.maxShowCount > 0 ? config.maxShowCount.ToString() : "No limit")
                            .Line("Reload After Show", !config.disablePostInitReload)));
                }

                return groups;
            }

            public override HDCDebugInfo Describe(string group, string position)
            {
                HDCAdsConfig.ForceAdChannel config = channel.Channel;
                HDCDebugInfo info = new HDCDebugInfo()
                    .Section("Configs")
                    .Needed("Enabled", config.isEnabled)
                    .Line("Launch Capping (s)", config.launchCappingTime)
                    .Line("Minimum Capping (s)", config.minimumCappingTime)
                    .Line("Capping Decrease Per Impression (s)", config.cappingDecreasePerImpression)
                    .Line("Positions", config.positionConfigs?.Length ?? 0)
                    .Section("Runtime")
                    .Line("Ignore Ads", channel.IgnoreAds)
                    .Line("First Ad Of Session", channel.firstAd)
                    .Line("Total Impressions", channel.TotalImpressionCount)
                    .Line("Since Last Full-Screen Ad (s)", Context.Clock.RealTime - Context.LastFullscreenAdTime)
                    .Section("Gates")
                    .Gate("Disabled", channel.IsDisabled)
                    .Gate("Ads Removed", Context.IsAdsRemoved);

                HDCAdsConfig.ForceAdPosition positionConfig = channel.PositionConfig(position);
                if (positionConfig != null)
                {
                    bool allowed = channel.Allowed(position, false, out string reason);
                    info.Section("Position " + position)
                        .Line("Group", Context.CoreConfig.ForceAdGroupAt(position))
                        .Line("Can Show (Config)", positionConfig.canShow)
                        .Line("Auto Init", positionConfig.autoInit)
                        .Line("Capping Now (s)", channel.Capping(positionConfig))
                        .Line("Impressions Here", channel.ImpressionCount(position))
                        .Add("Can Show Now", allowed ? "Yes" : "No · " + reason, allowed ? HDCDebugTone.Good : HDCDebugTone.Bad);
                }

                info.Section("Group " + (string.IsNullOrEmpty(group) ? "-" : group));
                HDCFullscreenGroup made = Context.Groups.ExistingForceAdGroup(group);
                if (made != null)
                    made.DescribeTo(info);
                else
                    info.Add("State", "Not started: press Init", HDCDebugTone.Muted);
                return info;
            }

            public override void MapUnits(HDCDebugInfo map)
            {
                foreach (HDCAdCoreConfig.ForceAdGroup group in Configs)
                {
                    if (group == null)
                        continue;
                    bool interstitial = group.androidUnit?.androidInterstitials?.switchToInterstitialAndroid ?? false;
                    map.Section("Force Ad · " + group.groupName)
                        .Line("Positions", Join(group.positionNames))
                        .Line("Priority", FirstNetwork(group.mediationPriority, group.useBackup))
                        .Line("AdMob Unit", Dash(group.admobUnit?.id))
                        .Line(interstitial ? "Native Unit (Interstitial)" : "Native Unit", Dash(group.androidUnit?.id));
                    if (!interstitial && !string.IsNullOrEmpty(group.androidUnit?.id))
                        map.Line("Layout Group", Dash(group.androidUnit.layoutGroupName));
                }
            }

            public override bool Owns(string instanceId) => instanceId.StartsWith(HDCAdNames.ForceAdPrefix, StringComparison.Ordinal);
        }

        private sealed class ConfigRules : IConfigRule
        {
            public IEnumerable<HDCConfigFinding> Check(HDCAdsConfig ads, HDCAdCoreConfig core)
            {
                if (!(ads.forceAdChannel?.isEnabled ?? false))
                    yield break;

                HDCAdCoreConfig.ForceAdGroup[] groups = (core.forceAdGroups ?? new HDCAdCoreConfig.ForceAdGroup[0]).Where(group => group != null).ToArray();
                string[] positions = Positions(ads).ToArray();
                if (groups.Length == 0)
                    yield return HDCConfigFinding.Error("FA: bật forceAdChannel nhưng ad core config không có forceAdGroups.");
                foreach (string position in positions.Where(p => !string.IsNullOrEmpty(p) && string.IsNullOrEmpty(core.ForceAdGroupAt(p))))
                    yield return HDCConfigFinding.Error($"FA: position '{position}' không thuộc force ad group nào (thêm vào positionNames của một group).");
                foreach (HDCAdCoreConfig.ForceAdGroup group in groups)
                {
                    if (string.IsNullOrEmpty(group.admobUnit?.id) && string.IsNullOrEmpty(group.androidUnit?.id))
                        yield return HDCConfigFinding.Error($"FA: group '{group.groupName}' không có ad unit nào.");
                    bool nativeFullscreen = !string.IsNullOrEmpty(group.androidUnit?.id) && !(group.androidUnit.androidInterstitials?.switchToInterstitialAndroid ?? false);
                    if (nativeFullscreen && core.LayoutGroupNamed(group.androidUnit.layoutGroupName) == null)
                        yield return HDCConfigFinding.Error($"FA: group '{group.groupName}' dùng layout group '{group.androidUnit.layoutGroupName}' không có trong forceAdLayoutConfig.");
                    foreach (string position in (group.positionNames ?? new string[0]).Where(p => !string.IsNullOrEmpty(p) && !positions.Contains(p)))
                        yield return HDCConfigFinding.Warning($"FA: group '{group.groupName}' có position '{position}' không có trong forceAdChannel.positionConfigs.");
                }
            }

            public IEnumerable<string> Positions(HDCAdsConfig ads) =>
                (ads.forceAdChannel?.positionConfigs ?? new HDCAdsConfig.ForceAdPosition[0]).Where(config => config != null).Select(config => config.positionName);
        }
    }
}
