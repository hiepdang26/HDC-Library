using System.Collections.Generic;
using System.Globalization;
using HDC.Ads.Diagnostics;
using HDC.Ads.Domain;
using HDC.Ads.Ports;

namespace HDC.Ads.Logic
{
    internal sealed partial class HDCBreakAds
    {
        private static readonly IConfigRule Rules = new ConfigRules();
        private DebugModule debug;

        IChannelDiagnostics IAdChannel.Diagnostics => debug ?? (debug = new DebugModule(this));

        IConfigRule IAdChannel.ConfigRule => Rules;

        private static string Seconds(float value) => value.ToString("0.#", CultureInfo.InvariantCulture) + " s";

        private sealed class DebugModule : HDCChannelDiagnostics
        {
            private readonly HDCBreakAds channel;

            internal DebugModule(HDCBreakAds channel)
                : base(channel.context)
            {
                this.channel = channel;
                Actions = new[]
                {
                    new HDCDebugAction("Start", "Start", true, _ =>
                    {
                        channel.Start();
                        return "BreakAd.Start()";
                    }),
                    new HDCDebugAction("Break Now", "Break Now", true, _ =>
                    {
                        channel.BreakNow();
                        return "BreakAd: notice starts now";
                    }),
                    new HDCDebugAction("Stop", "Stop", false, _ =>
                    {
                        channel.Stop();
                        return "BreakAd.Stop()";
                    }),
                };
            }

            public override IReadOnlyList<HDCDebugAction> Actions { get; }

            public override IReadOnlyList<(string Label, string Call)> Api { get; } = new[]
            {
                ("Start", "HDCAds.BreakAd.Start()"),
                ("Stop", "HDCAds.BreakAd.Stop()"),
                ("Restart", "HDCAds.BreakAd.Restart()"),
                ("State", "HDCAds.BreakAd.State, StateChanged"),
            };

            public override string Hint => "Break ad chạy khi prefab HDCBreakAd có trong scene, hoặc khi game gọi Start. Break Now bắt đầu đếm ngược ngay.";

            public override List<HDCDebugGroup> UnitGroups(string group, string position, bool askNative)
            {
                HDCAdCoreConfig.BreakAds config = channel.Config;
                var breakAd = new HDCDebugGroup { Kind = "Channel", Name = Position, Selected = true };
                breakAd.Details
                    .Line("Ad", "Native full-screen ad of break_ads_config")
                    .Line("Layout Group", Dash(config.layoutGroup))
                    .Line("State", channel.State)
                    .Line("Last Result", Dash(channel.lastResult));
                HDCAdPlan plan = channel.source == null ? Context.Groups.BreakAdPlan() : null;
                string id = channel.source?.Id ?? plan?.InstanceId ?? HDCAdNames.NativeBreakAd;
                string format = channel.source?.Format ?? plan?.Format ?? HDCAdFormat.Fullscreen;
                HDCAdRecord record = HDCAdsTracker.Find(format, id);
                breakAd.Units.Add(new HDCDebugUnit
                {
                    Index = 1,
                    Name = UnitName((channel.source?.Network ?? plan?.Network)?.Name, format),
                    Format = format,
                    Id = id,
                    AdUnitId = record?.AdUnitId ?? config.adUnitId,
                    Created = channel.source != null,
                    Started = channel.source != null,
                    Ready = channel.source?.IsReady ?? false,
                    OnScreen = channel.State == HDCBreakAdState.Showing && channel.displayed,
                    Record = record,
                });
                return new List<HDCDebugGroup> { breakAd };
            }

            public override HDCDebugInfo Describe(string group, string position)
            {
                HDCAdCoreConfig.BreakAds config = channel.Config;
                return new HDCDebugInfo()
                    .Section("Configs")
                    .Needed("Configured", config.IsOn)
                    .Line("Ad Unit ID", Dash(config.adUnitId))
                    .Line("Layout Group", Dash(config.layoutGroup))
                    .Line("Show Break Ad Time", Seconds(config.showBreakAdTime))
                    .Line("Noti Break Ad Time", Seconds(config.notiBreakAdTime))
                    .Section("Runtime")
                    .Line("Running", channel.running)
                    .Line("State", channel.State)
                    .Line("Off Because", channel.State == HDCBreakAdState.Off ? channel.offReason : "-")
                    .Line("Counted", Seconds(channel.elapsed) + " / " + Seconds(config.NoticeAt))
                    .Line("Countdown Left", Seconds(channel.CountdownLeft))
                    .Line("Loading Timeout", Seconds(channel.loadingTimeout))
                    .Line("Ad Loading", channel.loading)
                    .Line("Ad Ready", channel.source?.IsReady ?? false)
                    .Line("Other Full-Screen Ads On Screen", channel.otherFullscreens.Count)
                    .Line("App Paused", channel.paused)
                    .Line("Last Result", Dash(channel.lastResult))
                    .Section("Gates")
                    .Gate("Not Started", !channel.running)
                    .Gate("Not Configured", !config.IsOn)
                    .Gate("Ads Removed", Context.IsAdsRemoved);
            }

            public override void MapUnits(HDCDebugInfo map)
            {
                HDCAdCoreConfig.BreakAds config = channel.Config;
                map.Section("Break Ad · BRK")
                    .Switch("Configured", config.IsOn)
                    .Line("Native Unit", Dash(config.adUnitId))
                    .Line("Layout Group", Dash(config.layoutGroup))
                    .Line("Every", Seconds(config.showBreakAdTime))
                    .Line("Notice", Seconds(config.notiBreakAdTime));
            }

            public override bool Owns(string instanceId) => instanceId == HDCAdNames.NativeBreakAd;
        }

        private sealed class ConfigRules : IConfigRule
        {
            public IEnumerable<HDCConfigFinding> Check(HDCAdsConfig ads, HDCAdCoreConfig core)
            {
                HDCAdCoreConfig.BreakAds config = core.break_ads_config ?? new HDCAdCoreConfig.BreakAds();
                if (string.IsNullOrWhiteSpace(config.adUnitId))
                    yield break;
                if (config.showBreakAdTime <= 0)
                {
                    yield return HDCConfigFinding.Info("BRK: break_ads_config.showBreakAdTime không lớn hơn 0 nên break ad tắt.");
                    yield break;
                }

                if (config.notiBreakAdTime < 0)
                    yield return HDCConfigFinding.Warning("BRK: break_ads_config.notiBreakAdTime âm: không đếm ngược, quảng cáo hiện ngay khi tới giờ.");
                else if (config.notiBreakAdTime >= config.showBreakAdTime)
                    yield return HDCConfigFinding.Warning("BRK: break_ads_config.notiBreakAdTime không nhỏ hơn showBreakAdTime: đếm ngược bắt đầu ngay đầu mỗi chu kỳ.");
                if (string.IsNullOrEmpty(config.layoutGroup))
                    yield return HDCConfigFinding.Warning("BRK: break_ads_config.layoutGroup trống: dùng layout mặc định.");
                else if (core.LayoutGroupNamed(config.layoutGroup) == null)
                    yield return HDCConfigFinding.Warning($"BRK: layoutGroup '{config.layoutGroup}' không có trong forceAdLayoutConfig: dùng layout mặc định.");
            }

            public IEnumerable<string> Positions(HDCAdsConfig ads) => new string[0];
        }
    }
}
