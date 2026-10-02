using System;

namespace HDC.Ads.Infrastructure
{
    /// <summary>
    /// Retries failed loads after 2, 4, 8, 16, 32 and then every 64 seconds of real time. A successful load
    /// resets the delay.
    /// </summary>
    internal sealed class HDCRetry
    {
        private const int MaxExponent = 6;

        private int attempt;
        private int generation;

        internal bool IsWaiting { get; private set; }

        /// <summary>Failed loads in a row, which set the next delay.</summary>
        internal int Attempt => attempt;

        /// <summary>Seconds the scheduled retry waits.</summary>
        internal float Delay => 1 << attempt;

        /// <summary>Runs <paramref name="retry"/> after the next delay, replacing a pending retry.</summary>
        internal void Schedule(Action retry)
        {
            attempt = Math.Min(attempt + 1, MaxExponent);
            int scheduled = ++generation;
            IsWaiting = true;
            HDCMainThread.PostDelayed(1 << attempt, () =>
            {
                if (scheduled != generation)
                    return;
                IsWaiting = false;
                retry();
            });
        }

        /// <summary>Drops a pending retry; the delay keeps growing until <see cref="Reset"/>.</summary>
        internal void Cancel()
        {
            generation++;
            IsWaiting = false;
        }

        /// <summary>Drops a pending retry and starts the next one over at the shortest delay.</summary>
        internal void Reset()
        {
            Cancel();
            attempt = 0;
        }
    }
}
