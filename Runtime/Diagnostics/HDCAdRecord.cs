using System;

namespace HDC.Ads.Diagnostics
{
    /// <summary>What one ad instance did so far.</summary>
    internal sealed class HDCAdRecord
    {
        internal HDCAdRecord(string format, string id)
        {
            Format = format;
            Id = id;
        }

        internal string Format { get; }
        internal string Id { get; }
        internal HDCAdState State { get; set; } = HDCAdState.Idle;
        internal string AdUnitId { get; set; }
        internal string AdSource { get; set; }
        internal string Adapter { get; set; }
        internal string Layout { get; set; }
        internal int Requests { get; set; }
        internal int Loads { get; set; }
        internal int LoadFailures { get; set; }
        internal int Shows { get; set; }
        internal int ShowFailures { get; set; }
        internal int Impressions { get; set; }
        internal int Clicks { get; set; }
        internal double Revenue { get; set; }
        internal string Currency { get; set; }

        /// <summary>Seconds the last requested load took to succeed or fail; below zero until one did.</summary>
        internal float LoadSeconds { get; set; } = -1f;

        /// <summary>Real time of the scheduled retry of a failed load; below zero when none waits.</summary>
        internal float RetryAt { get; set; } = -1f;

        internal int RetryAttempt { get; set; }
        internal HDCAdError LastLoadError { get; set; }
        internal HDCAdError LastShowError { get; set; }
        internal DateTime UpdatedClock { get; set; }

        internal float RequestedAt { get; set; } = -1f;
        internal bool AwaitingResult { get; set; }
    }
}
