using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HDC.Ads.Diagnostics;
using HDC.Ads.Ports;

namespace HDC.Ads.Logic
{
    internal abstract class HDCChannelDiagnostics : IChannelDiagnostics
    {
        private static readonly string[] None = new string[0];

        protected HDCChannelDiagnostics(HDCAdsContext context)
        {
            Context = context;
        }

        public abstract IReadOnlyList<HDCDebugAction> Actions { get; }

        public abstract IReadOnlyList<(string Label, string Call)> Api { get; }

        public virtual string PositionLabel => "Position";

        public virtual string Hint => "Kênh này không cần chọn group hay position.";

        public virtual string PositionHint => "Chọn position để Show, Hide hay đặt vị trí.";

        public virtual bool UsesArea => false;

        protected HDCAdsContext Context { get; }

        public virtual IReadOnlyList<string> Groups() => None;

        public virtual IReadOnlyList<string> Positions(string group) => None;

        public abstract List<HDCDebugGroup> UnitGroups(string group, string position, bool askNative);

        public abstract HDCDebugInfo Describe(string group, string position);

        public abstract void MapUnits(HDCDebugInfo map);

        public abstract bool Owns(string instanceId);

        internal static string UnitName(string network, string format)
        {
            string kind;
            switch (format)
            {
                case HDCAdFormat.Interstitial: kind = "Interstitial"; break;
                case HDCAdFormat.Fullscreen: kind = "Full-Screen"; break;
                case HDCAdFormat.Rewarded: kind = "Rewarded"; break;
                case HDCAdFormat.AppOpen: kind = "App Open"; break;
                case HDCAdFormat.Banner:
                case HDCAdFormat.BannerView: kind = "Banner"; break;
                case HDCAdFormat.Mrec: kind = "MREC"; break;
                case HDCAdFormat.Popup: kind = "Popup"; break;
                default: kind = format; break;
            }

            return string.IsNullOrEmpty(network) ? kind : network + " " + kind;
        }

        protected static HDCDebugGroup FullscreenGroup(string name, HDCFullscreenGroup made, bool selected, IEnumerable<HDCAdPlan> plans,
            Action<HDCDebugInfo> details)
        {
            var group = new HDCDebugGroup { Name = name, Selected = selected };
            details(group.Details);
            if (made == null)
            {
                AddPlanned(group, plans);
                return group;
            }

            group.Details
                .Line("Ready", made.IsReady)
                .Line("Showing", made.IsShowing)
                .Line("Shows Left", made.ShowsLeft < 0 ? "No limit" : made.ShowsLeft.ToString())
                .Line("Units Started", made.StartedCount + " / " + made.Sources.Count);
            for (int i = 0; i < made.Sources.Count; i++)
            {
                HDCFullscreenSource source = made.Sources[i];
                HDCAdRecord record = HDCAdsTracker.Find(source.Format, source.Id);
                group.Units.Add(new HDCDebugUnit
                {
                    Index = i + 1,
                    Name = UnitName(source.Network.Name, source.Format),
                    Format = source.Format,
                    Id = source.Id,
                    AdUnitId = record?.AdUnitId ?? source.AdUnitId,
                    Created = true,
                    Started = i < made.StartedCount,
                    Ready = source.IsReady,
                    OnScreen = source.IsShowing,
                    Record = record,
                });
            }

            foreach (HDCFullscreenSource source in made.Sources)
            {
                IFullscreenAd companion = source.Companion;
                if (companion == null)
                    continue;
                HDCAdRecord record = HDCAdsTracker.Find(companion.Format, companion.Id);
                group.Units.Add(new HDCDebugUnit
                {
                    Index = group.Units.Count + 1,
                    Name = UnitName(source.Network.Name, companion.Format) + " · After Interstitial",
                    Format = companion.Format,
                    Id = companion.Id,
                    AdUnitId = record?.AdUnitId ?? companion.AdUnitId,
                    Created = true,
                    Started = true,
                    Ready = companion.IsReady,
                    OnScreen = source.CompanionShowing,
                    Record = record,
                });
            }

            return group;
        }

        protected static HDCDebugGroup RectGroup(string name, HDCRectGroup made, bool selected, IEnumerable<HDCAdPlan> plans,
            Action<HDCDebugInfo> details)
        {
            var group = new HDCDebugGroup { Name = name, Selected = selected };
            details(group.Details);
            if (made == null)
            {
                AddPlanned(group, plans);
                return group;
            }

            group.Details
                .Line("Loaded", made.IsLoaded)
                .Line("Showing", made.IsShowing)
                .Line("Units Started", made.StartedCount + " / " + made.Sources.Count);
            for (int i = 0; i < made.Sources.Count; i++)
            {
                HDCRectSource source = made.Sources[i];
                HDCAdRecord record = HDCAdsTracker.Find(source.Format, source.Id);
                group.Units.Add(new HDCDebugUnit
                {
                    Index = i + 1,
                    Name = UnitName(source.Network.Name, source.Format),
                    Format = source.Format,
                    Id = source.Id,
                    AdUnitId = record?.AdUnitId ?? source.AdUnitId,
                    Created = true,
                    Started = i < made.StartedCount,
                    Ready = source.IsLoaded,
                    OnScreen = made.IsShowing && made.Visible == source,
                    Record = record,
                });
            }

            return group;
        }

        protected string Priority(int priority)
        {
            IAdNetwork network = Context.Network(Context.Order.KeyFor(priority));
            return network != null ? $"{priority} · {network.Name} first" : priority.ToString();
        }

        protected string FirstNetwork(int priority, bool useBackup)
        {
            IAdNetwork network = Context.Network(Context.Order.KeyFor(priority));
            return (network != null ? network.Name + " first" : priority.ToString(CultureInfo.InvariantCulture))
                + (useBackup ? " · backup on" : string.Empty);
        }

        protected static string Join(IEnumerable<string> values)
        {
            string[] list = (values ?? None).Where(value => !string.IsNullOrEmpty(value)).ToArray();
            return list.Length == 0 ? "-" : string.Join(", ", list);
        }

        protected static string Dash(string value) => string.IsNullOrEmpty(value) ? "-" : value;

        protected static string[] Distinct(IEnumerable<string> values) =>
            values.Where(value => !string.IsNullOrEmpty(value)).Distinct().ToArray();

        private static void AddPlanned(HDCDebugGroup group, IEnumerable<HDCAdPlan> plans)
        {
            int index = 0;
            foreach (HDCAdPlan plan in plans)
            {
                group.Units.Add(new HDCDebugUnit
                {
                    Index = ++index,
                    Name = UnitName(plan.Network.Name, plan.Format),
                    Format = plan.Format,
                    Id = plan.InstanceId,
                    AdUnitId = plan.AdUnitId,
                    Record = HDCAdsTracker.Find(plan.Format, plan.InstanceId),
                });
            }

            if (group.Units.Count == 0)
                group.Details.Add("Ad Units", "None in the ad core config", HDCDebugTone.Bad);
        }
    }
}
