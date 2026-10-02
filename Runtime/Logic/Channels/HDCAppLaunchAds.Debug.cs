using System;
using System.Collections.Generic;
using HDC.Ads.Diagnostics;
using HDC.Ads.Domain;
using HDC.Ads.Ports;

namespace HDC.Ads.Logic
{
    internal sealed partial class HDCAppLaunchAds
    {
        private static readonly IConfigRule Rules = new ConfigRules();
        private DebugModule debug;

        IChannelDiagnostics IAdChannel.Diagnostics => debug ?? (debug = new DebugModule(this));

        IConfigRule IAdChannel.ConfigRule => Rules;

        private sealed class DebugModule : HDCChannelDiagnostics
        {
            private readonly HDCAppLaunchAds channel;

            internal DebugModule(HDCAppLaunchAds channel)
                : base(channel.context)
            {
                this.channel = channel;
                Actions = new[]
                {
                    new HDCDebugAction("Init", "Init", true, _ =>
                    {
                        channel.Initialize();
                        return "AppLaunch.Initialize()";
                    }),
                };
            }

            public override IReadOnlyList<HDCDebugAction> Actions { get; }

            public override IReadOnlyList<(string Label, string Call)> Api { get; } = new[]
            {
                ("Init", "HDCAds.AppLaunch.Initialize()"),
                ("Done", "HDCAds.AppLaunch.Completed, IsCompleted"),
            };

            public override List<HDCDebugGroup> UnitGroups(string group, string position, bool askNative)
            {
                HDCAdCoreConfig.Comeback comeback = Context.CoreConfig.comebackChannel ?? new HDCAdCoreConfig.Comeback();
                bool forceAd = comeback.launchAdType == 0;
                HDCAdCoreConfig.ForceAdGroup config = forceAd ? Context.CoreConfig.ForceAdGroupNamed(comeback.launchForceAdGroupName) : null;
                IEnumerable<HDCAdPlan> plans = forceAd
                    ? config != null ? Context.Groups.ForceAdPlans(config.groupName) : new HDCAdPlan[0]
                    : Context.Groups.AppOpenPlans();
                HDCDebugGroup launch = FullscreenGroup(forceAd ? comeback.launchForceAdGroupName : "app_open", channel.group, true, plans,
                    details => details.Add("Launch Ad",
                        forceAd ? "Force ad group " + comeback.launchForceAdGroupName + (config == null ? " (missing in the ad core config)" : string.Empty) : "App open",
                        forceAd && config == null ? HDCDebugTone.Bad : HDCDebugTone.Normal));
                launch.Kind = forceAd ? "Group" : "Channel";
                return new List<HDCDebugGroup> { launch };
            }

            public override HDCDebugInfo Describe(string group, string position)
            {
                HDCAdCoreConfig.Comeback comeback = Context.CoreConfig.comebackChannel ?? new HDCAdCoreConfig.Comeback();
                HDCDebugInfo info = new HDCDebugInfo()
                    .Section("Configs")
                    .Needed("Enabled", channel.Channel.isEnabled)
                    .Line("Auto Init", channel.Channel.autoInit)
                    .Line("Minimum Wait (s)", channel.MinimumWait)
                    .Line("Timeout (s)", channel.Timeout)
                    .Line("Launch Ad", comeback.launchAdType == 0 ? "Force ad group " + comeback.launchForceAdGroupName : "App open")
                    .Section("Runtime")
                    .Line("Clock Started", channel.clockStarted)
                    .Line("Elapsed (s)", channel.Elapsed)
                    .Line("Showing", channel.showing)
                    .Line("Completed", channel.IsCompleted)
                    .Line("Before Show Raised", channel.IsBeforeShowRaised)
                    .Line("Ignore Ads", channel.IgnoreAds)
                    .Section("Gates")
                    .Gate("Disabled", channel.IsDisabled)
                    .Gate("Ads Removed", Context.IsAdsRemoved)
                    .Section("Group");
                if (channel.group != null)
                    channel.group.DescribeTo(info);
                else
                    info.Add("State", "Not started", HDCDebugTone.Muted);
                return info;
            }

            public override void MapUnits(HDCDebugInfo map)
            {
                HDCAdCoreConfig.Comeback comeback = Context.CoreConfig.comebackChannel ?? new HDCAdCoreConfig.Comeback();
                map.Section("App Launch · AL")
                    .Switch("Enabled", Context.Config.appLaunchChannel?.isEnabled ?? false)
                    .Line("Launch Ad", comeback.launchAdType == 0 ? "Force ad group " + Dash(comeback.launchForceAdGroupName) : "App open");
                if (comeback.launchAdType != 0)
                    map.Line("App Open Unit", Dash(Context.CoreConfig.appOpenUnit?.admobUnit?.id));
            }

            public override bool Owns(string instanceId) => instanceId.StartsWith(HDCAdNames.AppOpenPrefix, StringComparison.Ordinal);
        }

        private sealed class ConfigRules : IConfigRule
        {
            public IEnumerable<HDCConfigFinding> Check(HDCAdsConfig ads, HDCAdCoreConfig core)
            {
                if (!(ads.appLaunchChannel?.isEnabled ?? false))
                    yield break;
                HDCAdCoreConfig.Comeback comeback = core.comebackChannel ?? new HDCAdCoreConfig.Comeback();
                if (comeback.launchAdType == 0 && core.ForceAdGroupNamed(comeback.launchForceAdGroupName) == null)
                    yield return HDCConfigFinding.Error($"AL: comebackChannel.launchForceAdGroupName '{comeback.launchForceAdGroupName}' không có trong forceAdGroups.");
                else if (comeback.launchAdType != 0 && string.IsNullOrEmpty(core.appOpenUnit?.admobUnit?.id))
                    yield return HDCConfigFinding.Error("AL: launch dùng app open nhưng appOpenUnit.admobUnit.id trống.");
            }

            public IEnumerable<string> Positions(HDCAdsConfig ads) => new string[0];
        }
    }
}
