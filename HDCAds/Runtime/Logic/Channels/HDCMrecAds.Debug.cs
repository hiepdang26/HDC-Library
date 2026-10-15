using System;
using System.Collections.Generic;
using HDC.Ads.Diagnostics;
using HDC.Ads.Domain;
using UnityEngine;

namespace HDC.Ads.Logic
{
    internal sealed partial class HDCMrecAds
    {
        private static readonly IConfigRule Rules = new ConfigRules();
        private DebugModule debug;

        IChannelDiagnostics IAdChannel.Diagnostics => debug ?? (debug = new DebugModule(this));

        IConfigRule IAdChannel.ConfigRule => Rules;

        private sealed class DebugModule : HDCChannelDiagnostics
        {
            private static readonly string[] ScreenPositions = { "TopLeft", "Top", "TopRight", "Center", "BottomLeft", "Bottom", "BottomRight" };
            private static readonly Vector2 MrecPoints = new Vector2(300f, 250f);

            private readonly HDCMrecAds channel;

            internal DebugModule(HDCMrecAds channel)
                : base(channel.context)
            {
                this.channel = channel;
                Actions = new[]
                {
                    new HDCDebugAction("Init", "Init", true, _ =>
                    {
                        channel.Initialize();
                        return "Mrec.Initialize()";
                    }),
                    new HDCDebugAction("Show", "Activate", true, _ => $"Mrec.Show() -> {channel.Show()}"),
                    new HDCDebugAction("Hide", "Hide", false, _ =>
                    {
                        channel.Hide();
                        return "Mrec.Hide()";
                    }),
                    new HDCDebugAction("Update Position", "UpdatePos", false, selection =>
                    {
                        if (!Enum.TryParse(selection.Position, out HDCAdPosition position))
                            return null;
                        channel.Move(position);
                        return $"Mrec.Move({position})";
                    }),
                    new HDCDebugAction("Get Size", "GetSize", false, _ => $"Mrec.SizeInPixels -> {channel.SizeInPixels}"),
                };
            }

            public override IReadOnlyList<HDCDebugAction> Actions { get; }

            public override IReadOnlyList<(string Label, string Call)> Api { get; } = new[]
            {
                ("Init", "HDCAds.Mrec.Initialize()"),
                ("Show / Hide", "HDCAds.Mrec.Show(), Hide()"),
                ("Place", "HDCAds.Mrec.Move(position), SizeInPixels"),
            };

            public override string Hint => "Giữ khung MREC để kéo tới chỗ bất kỳ, hoặc chọn một vị trí có sẵn rồi bấm UpdatePos.";

            public override IReadOnlyList<string> Positions(string group) => ScreenPositions;

            public override HDCDebugArea Area(string group, string position)
            {
                var area = new HDCDebugArea("MREC", MrecPoints, false, rect =>
                {
                    channel.Move(rect.center);
                    return $"Mrec.Move(new Vector2({rect.center.x:0}, {rect.center.y:0}))";
                });
                Vector2 size = channel.SizeInPixels;
                if (size.x > 0f)
                    area.PixelsPerPoint = size.x / MrecPoints.x;
                switch (channel.moveMode)
                {
                    case MoveMode.Point:
                        area.Center = channel.movePoint;
                        break;
                    case MoveMode.Target when channel.moveTarget != null:
                        area.Center = channel.TargetPoint();
                        break;
                    case MoveMode.Position:
                        area.Preset = channel.movePosition;
                        break;
                    default:
                        area.Preset = HDCAdPosition.BottomRight;
                        break;
                }

                return area;
            }

            public override List<HDCDebugGroup> UnitGroups(string group, string position, bool askNative)
            {
                HDCDebugGroup mrec = RectGroup("mrec", channel.group, true, Context.Groups.MrecPlans(),
                    details => details.Line("Priority", Priority(Context.CoreConfig.mrecUnit?.mediationPriority ?? 0)));
                mrec.Kind = "Channel";
                return new List<HDCDebugGroup> { mrec };
            }

            public override HDCDebugInfo Describe(string group, string position)
            {
                Vector2 size = channel.SizeInPixels;
                HDCDebugInfo info = new HDCDebugInfo()
                    .Section("Configs")
                    .Needed("Enabled", channel.Channel.isEnabled)
                    .Line("Auto Init", channel.Channel.autoInit)
                    .Section("Runtime")
                    .Line("Can Show", channel.IsEnabled && channel.group != null && channel.group.IsLoaded)
                    .Line("Size In Pixels", size == Vector2.zero ? "-" : $"{size.x:0} x {size.y:0}")
                    .Section("Gates")
                    .Gate("Disabled", !channel.IsEnabled)
                    .Gate("Ads Removed", Context.IsAdsRemoved)
                    .Section("Group");
                if (channel.group != null)
                    channel.group.DescribeTo(info);
                else
                    info.Add("State", "Not started", HDCDebugTone.Muted);
                return info;
            }

            public override void MapUnits(HDCDebugInfo map) =>
                map.Section("MREC")
                    .Switch("Enabled", Context.Config.mrecChannel?.isEnabled ?? false)
                    .Line("AdMob Unit", Dash(Context.CoreConfig.mrecUnit?.admobUnit?.id));

            public override bool Owns(string instanceId) => instanceId.StartsWith(HDCAdNames.MrecPrefix, StringComparison.Ordinal);
        }

        private sealed class ConfigRules : IConfigRule
        {
            public IEnumerable<HDCConfigFinding> Check(HDCAdsConfig ads, HDCAdCoreConfig core)
            {
                if ((ads.mrecChannel?.isEnabled ?? false) && string.IsNullOrEmpty(core.mrecUnit?.admobUnit?.id))
                    yield return HDCConfigFinding.Error("MREC: mrecUnit.admobUnit.id trống.");
            }

            public IEnumerable<string> Positions(HDCAdsConfig ads) => new string[0];
        }
    }
}
