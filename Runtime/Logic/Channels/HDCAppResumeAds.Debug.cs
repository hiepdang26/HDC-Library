using System.Collections.Generic;
using HDC.Ads.Diagnostics;
using HDC.Ads.Domain;
using HDC.Ads.Ports;

namespace HDC.Ads.Logic
{
    internal sealed partial class HDCAppResumeAds
    {
        private static readonly IConfigRule Rules = new ConfigRules();
        private DebugModule debug;

        IChannelDiagnostics IAdChannel.Diagnostics => debug ?? (debug = new DebugModule(this));

        IConfigRule IAdChannel.ConfigRule => Rules;

        // The debug panel's view of the channel: one native full-screen ad.
        private sealed class DebugModule : HDCChannelDiagnostics
        {
            private readonly HDCAppResumeAds channel;

            internal DebugModule(HDCAppResumeAds channel)
                : base(channel.context)
            {
                this.channel = channel;
                Actions = new[]
                {
                    new HDCDebugAction("Init", "Init", true, _ =>
                    {
                        channel.Initialize();
                        return "AppResume.Initialize()";
                    }),
                };
            }

            public override IReadOnlyList<HDCDebugAction> Actions { get; }

            public override IReadOnlyList<(string Label, string Call)> Api { get; } = new[]
            {
                ("Init", "HDCAds.AppResume.Initialize()"),
                ("Skip Next", "HDCAds.AppResume.Block()"),
            };

            public override List<HDCDebugGroup> UnitGroups(string group, string position, bool askNative)
            {
                var resume = new HDCDebugGroup { Kind = "Channel", Name = "app_resume", Selected = true };
                resume.Details
                    .Line("Ad", "Native full-screen ad of appResumeChannel")
                    .Line("Layout Group", Context.Config.appResumeChannel?.layoutGroup);
                HDCAdPlan plan = Context.Groups.ResumePlan();
                string id = plan?.InstanceId ?? HDCAdNames.NativeAppResume;
                string format = plan?.Format ?? HDCAdFormat.Fullscreen;
                HDCAdRecord record = HDCAdsTracker.Find(format, id);
                resume.Units.Add(new HDCDebugUnit
                {
                    Index = 1,
                    Name = UnitName(plan?.Network.Name, format),
                    Format = format,
                    Id = id,
                    AdUnitId = record?.AdUnitId ?? channel.Channel.adUnitId,
                    Created = channel.source != null,
                    Started = channel.source != null,
                    Record = record,
                });
                return new List<HDCDebugGroup> { resume };
            }

            public override HDCDebugInfo Describe(string group, string position) =>
                new HDCDebugInfo()
                    .Section("Configs")
                    .Needed("Enabled", channel.Channel.isEnabled)
                    .Line("Auto Init", channel.Channel.autoInit)
                    .Line("Ad Unit ID", channel.Channel.adUnitId)
                    .Line("Layout Group", channel.Channel.layoutGroup)
                    .Section("Runtime")
                    .Line("Started", channel.IsInitialized)
                    .Line("Blocked For Next Resume", channel.blocked)
                    .Line("Showing", channel.showing)
                    .Line("Shows Once Loaded", channel.showWhenLoaded)
                    .Line("Ignore Ads", channel.IgnoreAds)
                    .Section("Gates")
                    .Gate("Disabled", channel.IsDisabled)
                    .Gate("Ads Removed", Context.IsAdsRemoved);

            public override void MapUnits(HDCDebugInfo map) =>
                map.Section("App Resume · AR")
                    .Switch("Enabled", Context.Config.appResumeChannel?.isEnabled ?? false)
                    .Line("Native Unit", Dash(Context.Config.appResumeChannel?.adUnitId))
                    .Line("Layout Group", Dash(Context.Config.appResumeChannel?.layoutGroup));

            public override bool Owns(string instanceId) => instanceId == HDCAdNames.NativeAppResume;
        }

        private sealed class ConfigRules : IConfigRule
        {
            public IEnumerable<HDCConfigFinding> Check(HDCAdsConfig ads, HDCAdCoreConfig core)
            {
                HDCAdsConfig.AppResumeChannel resume = ads.appResumeChannel ?? new HDCAdsConfig.AppResumeChannel();
                if (!resume.isEnabled)
                    yield break;
                if (string.IsNullOrEmpty(resume.adUnitId))
                    yield return HDCConfigFinding.Error("AR: appResumeChannel.adUnitId trống.");
                if (!string.IsNullOrEmpty(resume.layoutGroup) && core.LayoutGroupNamed(resume.layoutGroup) == null)
                    yield return HDCConfigFinding.Warning($"AR: layoutGroup '{resume.layoutGroup}' không có trong forceAdLayoutConfig.");
            }

            public IEnumerable<string> Positions(HDCAdsConfig ads) => new string[0];
        }
    }
}
