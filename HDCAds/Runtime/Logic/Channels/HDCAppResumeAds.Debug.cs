using System;
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

        internal static string AdTypeText(int resumeAdType)
        {
            switch ((HDCFullscreenAdType)resumeAdType)
            {
                case HDCFullscreenAdType.Interstitial:
                    return "0 · Interstitial (AdMob)";
                case HDCFullscreenAdType.Native:
                    return "1 · Native full-screen";
                case HDCFullscreenAdType.AppOpen:
                    return "2 · App open (AdMob)";
                default:
                    return resumeAdType + " · Unknown";
            }
        }

        internal static string UnitPath(HDCFullscreenAdType type)
        {
            switch (type)
            {
                case HDCFullscreenAdType.Interstitial:
                    return "forceAdUnit.admobUnit.id";
                case HDCFullscreenAdType.Native:
                    return "forceAdUnit.nativeAdUnit.id";
                default:
                    return "appOpenUnit.admobUnit.id";
            }
        }

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
                HDCAdCoreConfig.AppResumeChannel config = channel.Channel;
                var resume = new HDCDebugGroup { Kind = "Channel", Name = "app_resume", Selected = true };
                resume.Details.Line("Ad", AdTypeText(config.resumeAdType));
                if (config.AdType == HDCFullscreenAdType.Native)
                    resume.Details.Line("Layout Group", config.forceAdUnit?.nativeAdUnit?.layoutGroupName);
                HDCAdPlan plan = Context.Groups.ResumePlan();
                string id = plan?.InstanceId ?? DefaultId(config.AdType);
                string format = plan?.Format ?? DefaultFormat(config.AdType);
                HDCAdRecord record = HDCAdsTracker.Find(format, id);
                resume.Units.Add(new HDCDebugUnit
                {
                    Index = 1,
                    Name = UnitName(plan?.Network.Name, format),
                    Format = format,
                    Id = id,
                    AdUnitId = record?.AdUnitId ?? config.AdUnitId,
                    Created = channel.source != null,
                    Started = channel.source != null,
                    Record = record,
                });
                return new List<HDCDebugGroup> { resume };
            }

            public override HDCDebugInfo Describe(string group, string position)
            {
                HDCAdCoreConfig.AppResumeChannel config = channel.Channel;
                HDCDebugInfo info = new HDCDebugInfo()
                    .Section("Configs")
                    .Needed("Enabled", config.isEnabled)
                    .Line("Auto Init", config.autoInit)
                    .Line("Ad Type", AdTypeText(config.resumeAdType))
                    .Line("Ad Unit ID", config.AdUnitId);
                if (config.AdType == HDCFullscreenAdType.Native)
                    info.Line("Layout Group", config.forceAdUnit?.nativeAdUnit?.layoutGroupName);
                return info
                    .Section("Runtime")
                    .Line("Started", channel.IsInitialized)
                    .Line("Blocked For Next Resume", channel.blocked)
                    .Line("Showing", channel.showing)
                    .Line("Shows Once Loaded", channel.showWhenLoaded)
                    .Line("Ignore Ads", channel.IgnoreAds)
                    .Section("Gates")
                    .Gate("Disabled", channel.IsDisabled)
                    .Gate("Ads Removed", Context.IsAdsRemoved);
            }

            public override void MapUnits(HDCDebugInfo map)
            {
                HDCAdCoreConfig.AppResumeChannel config = channel.Channel;
                map.Section("App Resume · AR")
                    .Switch("Enabled", config.isEnabled)
                    .Line("Ad Type", AdTypeText(config.resumeAdType))
                    .Line("Ad Unit", Dash(config.AdUnitId));
                if (config.AdType == HDCFullscreenAdType.Native)
                    map.Line("Layout Group", Dash(config.forceAdUnit?.nativeAdUnit?.layoutGroupName));
            }

            public override bool Owns(string instanceId) =>
                instanceId == HDCAdNames.NativeAppResume || instanceId == HDCAdNames.AdMobAppResume || instanceId == HDCAdNames.AdMobAppOpen;

            private static string DefaultId(HDCFullscreenAdType type) =>
                type == HDCFullscreenAdType.Native ? HDCAdNames.NativeAppResume
                : type == HDCFullscreenAdType.AppOpen ? HDCAdNames.AdMobAppOpen
                : HDCAdNames.AdMobAppResume;

            private static string DefaultFormat(HDCFullscreenAdType type) =>
                type == HDCFullscreenAdType.Native ? HDCAdFormat.Fullscreen
                : type == HDCFullscreenAdType.AppOpen ? HDCAdFormat.AppOpen
                : HDCAdFormat.Interstitial;
        }

        private sealed class ConfigRules : IConfigRule
        {
            public IEnumerable<HDCConfigFinding> Check(HDCAdsConfig ads, HDCAdCoreConfig core)
            {
                HDCAdCoreConfig.AppResumeChannel resume = core.appResumeChannel ?? new HDCAdCoreConfig.AppResumeChannel();
                if (!resume.isEnabled)
                    yield break;
                if (!Enum.IsDefined(typeof(HDCFullscreenAdType), resume.resumeAdType))
                {
                    yield return HDCConfigFinding.Error(
                        $"AR: appResumeChannel.resumeAdType {resume.resumeAdType} không hợp lệ (0 = inter, 1 = native, 2 = app open).");
                    yield break;
                }

                if (string.IsNullOrWhiteSpace(resume.AdUnitId))
                    yield return HDCConfigFinding.Error($"AR: appResumeChannel.{UnitPath(resume.AdType)} trống.");
                string layoutGroup = resume.forceAdUnit?.nativeAdUnit?.layoutGroupName;
                if (resume.AdType == HDCFullscreenAdType.Native && !string.IsNullOrEmpty(layoutGroup) && core.LayoutGroupNamed(layoutGroup) == null)
                    yield return HDCConfigFinding.Warning($"AR: layoutGroupName '{layoutGroup}' không có trong forceAdLayoutConfig.");
            }

            public IEnumerable<string> Positions(HDCAdsConfig ads) => new string[0];
        }
    }
}
