using System;
using System.Collections.Generic;
using System.Linq;
using HDC.Ads.Diagnostics;

namespace HDC.Ads.Logic
{
    /// <summary>
    /// A banner or MREC slot backed by ad units in priority order. The first unit loads at first; if it fails
    /// before ever loading and backups are allowed, the next one starts. A shown slot displays the first
    /// loaded unit, or the first unit until one loads.
    /// </summary>
    internal sealed class HDCRectGroup
    {
        private readonly List<HDCRectSource> sources;
        private readonly bool useBackup;
        private int started;
        private bool showing;
        private HDCRectSource visible;

        internal HDCRectGroup(List<HDCRectSource> sources, bool useBackup)
        {
            this.sources = sources;
            this.useBackup = useBackup;
            foreach (HDCRectSource source in sources)
            {
                source.Failed = OnSourceFailed;
                source.LoadedAd = OnSourceLoaded;
            }
        }

        /// <summary>Raised whenever a unit loads an ad.</summary>
        internal event Action Loaded;

        internal bool IsEmpty => sources.Count == 0;
        internal bool IsLoaded => LoadedSource() != null;
        internal IReadOnlyList<HDCRectSource> Sources => sources;

        /// <summary>Units started so far, in order: a unit starts once the ones before it failed.</summary>
        internal int StartedCount => started;

        internal bool IsShowing => showing;

        /// <summary>The unit on screen while the slot shows.</summary>
        internal HDCRectSource Visible => visible;

        internal bool UsesBackup => useBackup;

        internal void Initialize()
        {
            if (started == 0 && sources.Count > 0)
                StartNext();
        }

        internal void Show()
        {
            if (sources.Count == 0)
                return;
            Initialize();
            showing = true;
            Display(LoadedSource() ?? sources[started - 1]);
        }

        internal void Hide()
        {
            showing = false;
            visible?.Hide();
            visible = null;
        }

        internal bool Expand(bool enableClick) => LoadedSource()?.Expand(enableClick) ?? false;

        internal HDCRectSource LoadedSource()
        {
            for (int i = 0; i < started; i++)
            {
                if (sources[i].IsLoaded)
                    return sources[i];
            }

            return null;
        }

        /// <summary>The slot's state, for the debug panel, which shows its units on their own.</summary>
        internal void DescribeTo(HDCDebugInfo info) =>
            info.Line("Loaded", IsLoaded)
                .Line("Showing", showing)
                .Line("On Screen", visible?.Id ?? "-")
                .Line("Backup", useBackup)
                .Line("Units Started", started + " / " + sources.Count);

        private void Display(HDCRectSource source)
        {
            if (visible == source)
                return;
            visible?.Hide();
            visible = source;
            source.Show();
        }

        private void OnSourceFailed(HDCRectSource source)
        {
            if (!useBackup || source.IsLoaded || sources.IndexOf(source) != started - 1 || started >= sources.Count)
                return;
            StartNext();
            if (showing && LoadedSource() == null)
                Display(sources[started - 1]);
        }

        private void OnSourceLoaded(HDCRectSource source)
        {
            // A higher priority unit that loads takes the place of the one on screen.
            if (showing)
                Display(LoadedSource() ?? source);
            HDCCallbacks.Run(Loaded);
        }

        private void StartNext() => sources[started++].Load();
    }
}
