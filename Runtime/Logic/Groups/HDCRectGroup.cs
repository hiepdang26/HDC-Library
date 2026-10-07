using System;
using System.Collections.Generic;
using HDC.Ads.Diagnostics;
using HDC.Ads.Ports;

namespace HDC.Ads.Logic
{
    internal sealed class HDCRectGroup
    {
        internal const float FreshShownSeconds = 10f;
        internal const float SilentSeconds = 30f;
        internal const float RetryBaseSeconds = 20f;
        internal const float RetryStepSeconds = 5f;
        internal const float RetryMaxSeconds = 40f;
        private const float TickSeconds = 1f;

        private readonly List<HDCRectSource> sources;
        private readonly bool useBackup;
        private readonly IClock clock;
        private readonly IMainThread mainThread;
        private readonly IAdsLog log;
        private int started;
        private bool showing;
        private bool ticking;
        private float lastTick;
        private int swaps;
        private HDCRectSource visible;

        internal HDCRectGroup(List<HDCRectSource> sources, bool useBackup, IAdsLog log, IClock clock = null, IMainThread mainThread = null)
        {
            this.sources = sources;
            this.log = log;
            this.useBackup = useBackup;
            this.clock = clock;
            this.mainThread = mainThread;
            foreach (HDCRectSource source in sources)
            {
                source.Failed = OnSourceFailed;
                source.LoadedAd = OnSourceLoaded;
            }
        }

        internal event Action Loaded;

        internal bool IsEmpty => sources.Count == 0;
        internal bool IsLoaded => LoadedSource() != null;
        internal IReadOnlyList<HDCRectSource> Sources => sources;

        internal int StartedCount => started;

        internal bool IsShowing => showing;

        internal HDCRectSource Visible => visible;

        internal bool UsesBackup => useBackup;

        private float Now => clock?.RealTime ?? 0f;

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
            StartTicking();
            Display(Best() ?? sources[started - 1]);
        }

        internal void Hide()
        {
            showing = false;
            StopTicking();
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

        internal void DescribeTo(HDCDebugInfo info) =>
            info.Line("Loaded", IsLoaded)
                .Line("Showing", showing)
                .Line("On Screen", visible?.Id ?? "-")
                .Line("Refresh Fails On Screen", visible?.FailStreak ?? 0)
                .Line("Swaps", swaps)
                .Line("Backup", useBackup)
                .Line("Units Started", started + " / " + sources.Count);

        internal void Tick()
        {
            float now = Now;
            if (!showing || now - lastTick < TickSeconds)
                return;
            float elapsed = now - lastTick;
            lastTick = now;
            if (visible != null)
                visible.ShownSeconds += elapsed;

            if (visible != null && visible.IsLoaded && visible.RefreshSeconds > 0
                && now - visible.LastSignalAt > Math.Max(SilentSeconds, visible.RefreshSeconds + 5f))
            {
                visible.LastSignalAt = now;
                visible.FailStreak++;
                WakeBackup(visible);
                TrySwap();
            }

            for (int i = 0; i < started; i++)
            {
                HDCRectSource source = sources[i];
                if (source == visible || source.RetryAt < 0f || now < source.RetryAt)
                    continue;
                source.RetryAt = -1f;
                source.Reload();
            }
        }

        private void Display(HDCRectSource source)
        {
            if (visible == source)
                return;
            if (visible != null)
            {
                swaps++;
                if (useBackup && visible.FailStreak > 0)
                    ScheduleRetry(visible);
            }

            visible?.Hide();
            visible = source;
            source.LastSignalAt = Now;
            source.Show();
        }

        private HDCRectSource Best()
        {
            for (int i = 0; i < started; i++)
            {
                if (sources[i].IsLoaded && sources[i].FailStreak == 0)
                    return sources[i];
            }

            return LoadedSource();
        }

        private bool IsFresh(HDCRectSource source) => source.IsLoaded && source.FailStreak == 0 && source.ShownSeconds < FreshShownSeconds;

        private static int Threshold(HDCRectSource source) => source.RefreshSeconds < 0 || source.RefreshSeconds >= 20 ? 1 : 2;

        private bool CanReplaceVisible(HDCRectSource candidate) =>
            candidate != visible && IsFresh(candidate)
            && (sources.IndexOf(candidate) < sources.IndexOf(visible) || visible.FailStreak >= Threshold(visible));

        private void TrySwap()
        {
            if (!showing || visible == null)
                return;
            for (int i = 0; i < started; i++)
            {
                if (CanReplaceVisible(sources[i]))
                {
                    Display(sources[i]);
                    return;
                }
            }
        }

        private void WakeBackup(HDCRectSource current)
        {
            if (!useBackup)
                return;
            int index = sources.FindIndex(source => source != current);
            if (index < 0)
                return;
            if (index >= started)
            {
                while (started <= index)
                    StartNext();
                return;
            }

            HDCRectSource backup = sources[index];
            if (backup.Reloading || IsFresh(backup) || backup.RetryAt >= 0f)
                return;
            backup.Reload();
        }

        private void ScheduleRetry(HDCRectSource source)
        {
            source.RetryCount++;
            source.RetryAt = Now + Math.Min(RetryBaseSeconds + RetryStepSeconds * (source.RetryCount - 1), RetryMaxSeconds);
        }

        private void OnSourceFailed(HDCRectSource source)
        {
            source.LastSignalAt = Now;
            if (!source.IsLoaded)
            {
                if (!useBackup || sources.IndexOf(source) != started - 1 || started >= sources.Count)
                    return;
                StartNext();
                if (showing && LoadedSource() == null)
                    Display(sources[started - 1]);
                return;
            }

            source.FailStreak++;
            if (showing && source == visible)
            {
                WakeBackup(source);
                TrySwap();
            }
            else if (useBackup && showing)
            {
                ScheduleRetry(source);
            }
        }

        private void OnSourceLoaded(HDCRectSource source)
        {
            source.LastSignalAt = Now;
            if (showing)
            {
                if (visible == null || !visible.IsLoaded)
                    Display(Best() ?? source);
                else if (CanReplaceVisible(source))
                    Display(source);
            }

            HDCCallbacks.Run(Loaded, log);
        }

        private void StartNext() => sources[started++].Load();

        private void StartTicking()
        {
            lastTick = Now;
            if (ticking || mainThread == null)
                return;
            ticking = true;
            mainThread.Ticked += Tick;
        }

        private void StopTicking()
        {
            if (!ticking)
                return;
            ticking = false;
            mainThread.Ticked -= Tick;
        }
    }
}
