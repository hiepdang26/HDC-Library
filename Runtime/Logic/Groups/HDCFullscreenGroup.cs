using System;
using System.Collections.Generic;
using HDC.Ads.Diagnostics;

namespace HDC.Ads.Logic
{
    internal sealed class HDCFullscreenGroup
    {
        private readonly HDCAdsContext context;
        private readonly List<HDCFullscreenSource> sources;
        private readonly bool useBackup;
        private int started;
        private int remainingShows;

        internal HDCFullscreenGroup(HDCAdsContext context, string name, List<HDCFullscreenSource> sources, bool useBackup, int maxShowCount)
        {
            this.context = context;
            Name = name;
            this.sources = sources;
            this.useBackup = useBackup;
            remainingShows = maxShowCount > 0 ? maxShowCount : -1;
            foreach (HDCFullscreenSource source in sources)
                source.Failed = OnSourceFailed;
        }

        internal string Name { get; }
        internal bool IsEmpty => sources.Count == 0;
        internal IReadOnlyList<HDCFullscreenSource> Sources => sources;

        internal int StartedCount => started;

        internal int ShowsLeft => remainingShows;

        internal bool UsesBackup => useBackup;
        internal bool IsStopped => remainingShows == 0;
        internal bool IsReady => !IsStopped && ReadySource() != null;
        internal bool IsShowing => sources.Exists(source => source.IsShowing);

        internal void Initialize()
        {
            if (started == 0 && !IsStopped && sources.Count > 0)
                StartNext();
        }

        internal bool Show(HDCAdChannel channel, string position, Action onBeforeShow, Action onDisplayed, Action<bool> onClosed)
        {
            if (IsStopped || sources.Count == 0)
                return false;

            HDCFullscreenSource source = ReadySource();
            if (source == null)
            {
                if (useBackup || started == 0)
                    return false;
                source = sources[0];
            }

            onBeforeShow?.Invoke();
            context.NotifyFullscreenOpening();
            context.Placements.Record(source.Id, channel, position, source.Network.RevenueNetwork);
            bool displayed = false;
            return source.Show(
                () =>
                {
                    displayed = true;
                    onDisplayed?.Invoke();
                },
                rewarded =>
                {
                    if (displayed && remainingShows > 0 && --remainingShows == 0)
                        Destroy();
                    onClosed?.Invoke(rewarded);
                });
        }

        internal void Destroy()
        {
            foreach (HDCFullscreenSource source in sources)
                source.Destroy();
            started = sources.Count;
        }

        private HDCFullscreenSource ReadySource()
        {
            for (int i = 0; i < started && i < sources.Count; i++)
            {
                if (sources[i].IsReady)
                    return sources[i];
            }

            return null;
        }

        internal void DescribeTo(HDCDebugInfo info) =>
            info.Line("Group", Name)
                .Line("Ready", IsReady)
                .Line("Showing", IsShowing)
                .Line("Shows Left", remainingShows < 0 ? "No limit" : remainingShows.ToString())
                .Line("Backup", useBackup)
                .Line("Units Started", started + " / " + sources.Count);

        private void OnSourceFailed(HDCFullscreenSource source)
        {
            if (useBackup && sources.IndexOf(source) == started - 1 && started < sources.Count && !IsStopped)
                StartNext();
        }

        private void StartNext() => sources[started++].Load();
    }
}
