using System;

namespace HDC.Ads.Infrastructure
{
    internal sealed class HDCRetry
    {
        private const int MaxExponent = 6;

        private int attempt;
        private int generation;

        internal bool IsWaiting { get; private set; }

        internal int Attempt => attempt;

        internal float Delay => 1 << attempt;

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

        internal void Cancel()
        {
            generation++;
            IsWaiting = false;
        }

        internal void Reset()
        {
            Cancel();
            attempt = 0;
        }
    }
}
