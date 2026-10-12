using System.Collections.Generic;
using HDC.Ads.Diagnostics;
using HDC.Ads.Domain;

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
                Actions = new HDCDebugAction[0];
            }

            public override IReadOnlyList<HDCDebugAction> Actions { get; }

            public override IReadOnlyList<(string Label, string Call)> Api { get; } = new[]
            {
                ("Done", "HDCAds.AppLaunch.Completed, IsCompleted"),
            };

            public override List<HDCDebugGroup> UnitGroups(string group, string position, bool askNative) => new List<HDCDebugGroup>();

            public override HDCDebugInfo Describe(string group, string position) =>
                new HDCDebugInfo()
                    .Section("Configs")
                    .Line("Launch Ad", "Shown by the splash from " + LaunchAdSource)
                    .Section("Runtime")
                    .Line("Completed", channel.IsCompleted)
                    .Line("Completion", channel.completion)
                    .Line("Before Show Raised", channel.IsBeforeShowRaised)
                    .Line("Ignore Ads", channel.IgnoreAds);

            public override void MapUnits(HDCDebugInfo map) =>
                map.Section("App Launch · AL")
                    .Line("Launch Ad", "Splash · " + LaunchAdSource);

            public override bool Owns(string instanceId) => false;
        }

        private sealed class ConfigRules : IConfigRule
        {
            public IEnumerable<HDCConfigFinding> Check(HDCAdsConfig ads, HDCAdCoreConfig core) => new HDCConfigFinding[0];

            public IEnumerable<string> Positions(HDCAdsConfig ads) => new string[0];
        }
    }
}
