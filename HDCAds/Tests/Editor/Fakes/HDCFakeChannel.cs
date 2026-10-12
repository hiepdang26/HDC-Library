using System;
using System.Collections.Generic;
using HDC.Ads.Diagnostics;
using HDC.Ads.Domain;
using HDC.Ads.Logic;

namespace HDC.Ads.Tests
{
    internal sealed class HDCFakeChannel : IAdChannel
    {
        internal const string InstanceId = "fake_unit";

        internal const string SharedPosition = "gameplay";

        private readonly Module module;

        internal HDCFakeChannel(HDCAdsContext context)
        {
            module = new Module(this, context);
        }

        public string Key => "FAKE";
        public string Title => "Fake";
        public IChannelDiagnostics Diagnostics => module;
        public IConfigRule ConfigRule { get; } = new Rule();

        internal int Starts { get; private set; }
        internal int Pings { get; private set; }
        internal int Removals { get; private set; }

        public void OnSdkInitialized() => Starts++;

        public void InitializeAll()
        {
        }

        public void OnAdsRemoved() => Removals++;

        private sealed class Module : HDCChannelDiagnostics
        {
            private readonly HDCFakeChannel channel;

            internal Module(HDCFakeChannel channel, HDCAdsContext context)
                : base(context)
            {
                this.channel = channel;
                Actions = new[]
                {
                    new HDCDebugAction("Ping", "Ping", true, selection =>
                    {
                        channel.Pings++;
                        return $"Fake.Ping(\"{selection.Position}\")";
                    }),
                };
            }

            public override IReadOnlyList<HDCDebugAction> Actions { get; }

            public override IReadOnlyList<(string Label, string Call)> Api { get; } = new[] { ("Ping", "Fake.Ping(position)") };

            public override IReadOnlyList<string> Groups() => new[] { "fake_group" };

            public override IReadOnlyList<string> Positions(string group) => new[] { "fake_spot" };

            public override List<HDCDebugGroup> UnitGroups(string group, string position, bool askNative)
            {
                var fake = new HDCDebugGroup { Name = "fake_group", Selected = group == "fake_group" };
                fake.Details.Line("Pings", channel.Pings);
                fake.Units.Add(new HDCDebugUnit
                {
                    Index = 1,
                    Name = UnitName("Fake", HDCAdFormat.Banner),
                    Format = HDCAdFormat.Banner,
                    Id = InstanceId,
                    AdUnitId = "fake-ad-unit",
                    Created = true,
                    Started = true,
                    Ready = true,
                });
                return new List<HDCDebugGroup> { fake };
            }

            public override HDCDebugInfo Describe(string group, string position) =>
                new HDCDebugInfo().Section("Fake State").Line("Pings", channel.Pings);

            public override void MapUnits(HDCDebugInfo map) => map.Section("Fake Channel").Line("Fake Unit", "fake-ad-unit");

            public override bool Owns(string instanceId) => instanceId.StartsWith("fake_", StringComparison.Ordinal);
        }

        private sealed class Rule : IConfigRule
        {
            public IEnumerable<HDCConfigFinding> Check(HDCAdsConfig ads, HDCAdCoreConfig core)
            {
                yield return HDCConfigFinding.Warning("FAKE: kênh giả luôn có một cảnh báo.");
            }

            public IEnumerable<string> Positions(HDCAdsConfig ads) => new[] { SharedPosition };
        }
    }
}
