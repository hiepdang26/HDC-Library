using System;
using System.Collections.Generic;
using HDC.Ads.Diagnostics;

namespace HDC.Ads.Logic
{
    /// <summary>
    /// A full-screen ad slot backed by one or more ad units in priority order. Only the first unit loads at
    /// first; when the newest loaded unit fails to load or to show and backups are allowed, the next one
    /// starts. A show takes the first ready unit.
    /// </summary>
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

        /// <summary>Units started so far, in order: a unit starts once the ones before it failed.</summary>
        internal int StartedCount => started;

        /// <summary>Shows left before the group stops; below zero for no limit.</summary>
        internal int ShowsLeft => remainingShows;

        internal bool UsesBackup => useBackup;
        internal bool IsStopped => remainingShows == 0;
        internal bool IsReady => !IsStopped && ReadySource() != null;
        internal bool IsShowing => sources.Exists(source => source.IsShowing);

        internal void Initialize()
        {
            // A channel can be on with no unit configured for it.
            if (started == 0 && !IsStopped && sources.Count > 0)
                StartNext();
        }

        /// <summary>
        /// Shows the first ready unit for a channel at a position, which its revenue reports.
        /// <paramref name="onBeforeShow"/> runs right before the ad starts, <paramref name="onDisplayed"/> once it
        /// is on screen and <paramref name="onClosed"/> once it closes or fails, with whether a reward was earned.
        /// </summary>
        internal bool Show(HDCAdChannel channel, string position, Action onBeforeShow, Action onDisplayed, Action<bool> onClosed)
        {
            if (IsStopped || sources.Count == 0)
                return false;

            HDCFullscreenSource source = ReadySource();
            if (source == null)
            {
                // A single unit shows anyway: its own not-ready handling reports the failure and loads again.
                if (useBackup || started == 0)
                    return false;
                source = sources[0];
            }

            onBeforeShow?.Invoke();
            context.NotifyFullscreenOpening();
            context.Placements.Record(source.Id, channel, position);
            bool displayed = false;
            return source.Show(
                () =>
                {
                    displayed = true;
                    onDisplayed?.Invoke();
                },
                rewarded =>
                {
                    // Only shows that reached the screen use up the group's show count.
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

        /// <summary>The group's state, for the debug panel, which shows its units on their own.</summary>
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
