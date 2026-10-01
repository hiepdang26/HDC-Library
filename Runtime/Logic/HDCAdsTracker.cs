using System;
using System.Collections.Generic;
using UnityEngine;

namespace HDC.Ads
{
    /// <summary>The state of one ad instance, as its load commands and events show it.</summary>
    internal enum HDCAdState
    {
        Idle,
        Loading,
        Loaded,
        LoadFailed,
        Showing,
        ShowFailed,
        Closed,
        Destroyed,
    }

    /// <summary>A failed load or show: the ad SDK's error code, or -1 for an error without one.</summary>
    internal sealed class HDCAdError
    {
        internal int Code;
        internal string Message;
        internal string AdUnitId;
        internal DateTime Clock;
    }

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

    /// <summary>One ad event and when it came.</summary>
    internal sealed class HDCTrackedEvent
    {
        internal HDCTrackedEvent(HDCAdEvent adEvent, DateTime clock)
        {
            Event = adEvent;
            Clock = clock;
        }

        internal HDCAdEvent Event { get; }
        internal DateTime Clock { get; }
    }

    /// <summary>
    /// Keeps what every ad instance did: load requests, results with their error codes, shows, retries and
    /// revenue, and the latest events. HDCAdsSdk reports every command and event to it; the debug panel reads it.
    /// </summary>
    internal static class HDCAdsTracker
    {
        internal const int EventCapacity = 300;

        private static readonly Dictionary<string, HDCAdRecord> records = new Dictionary<string, HDCAdRecord>();
        private static readonly List<HDCTrackedEvent> events = new List<HDCTrackedEvent>(EventCapacity);

        /// <summary>The latest events, oldest first.</summary>
        internal static IReadOnlyList<HDCTrackedEvent> Events => events;

        internal static IEnumerable<HDCAdRecord> Records => records.Values;

        private static float Now => Time.realtimeSinceStartup;

        internal static HDCAdRecord Find(string format, string id) =>
            records.TryGetValue(Key(format, id), out HDCAdRecord record) ? record : null;

        internal static void Requested(string format, string id, string adUnitId)
        {
            HDCAdRecord record = Get(format, id);
            if (record == null)
                return;
            record.Requests++;
            record.State = HDCAdState.Loading;
            record.RequestedAt = Now;
            record.AwaitingResult = true;
            record.RetryAt = -1f;
            record.UpdatedClock = DateTime.Now;
            if (!string.IsNullOrEmpty(adUnitId))
                record.AdUnitId = adUnitId;
        }

        internal static void RetryScheduled(string format, string id, float delaySeconds, int attempt)
        {
            HDCAdRecord record = Get(format, id);
            if (record == null)
                return;
            record.RetryAt = Now + delaySeconds;
            record.RetryAttempt = attempt;
        }

        internal static void Destroyed(string format, string id)
        {
            HDCAdRecord record = Find(format, id);
            if (record == null)
                return;
            record.State = HDCAdState.Destroyed;
            record.RetryAt = -1f;
            record.AwaitingResult = false;
            record.UpdatedClock = DateTime.Now;
        }

        internal static void Record(HDCAdEvent adEvent)
        {
            DateTime clock = DateTime.Now;
            if (events.Count == EventCapacity)
                events.RemoveAt(0);
            events.Add(new HDCTrackedEvent(adEvent, clock));

            HDCAdRecord record = adEvent.format == HDCAdFormat.Sdk ? null : Get(adEvent.format, adEvent.id);
            if (record == null)
                return;

            record.UpdatedClock = clock;
            if (!string.IsNullOrEmpty(adEvent.adUnitId))
                record.AdUnitId = adEvent.adUnitId;
            if (!string.IsNullOrEmpty(adEvent.adSource))
                record.AdSource = adEvent.adSource;
            if (!string.IsNullOrEmpty(adEvent.adapter))
                record.Adapter = adEvent.adapter;
            if (!string.IsNullOrEmpty(adEvent.layout))
                record.Layout = adEvent.layout;

            switch (adEvent.type)
            {
                case HDCAdEventType.Loaded:
                    record.State = HDCAdState.Loaded;
                    record.Loads++;
                    record.RetryAt = -1f;
                    record.RetryAttempt = 0;
                    TakeLoadTime(record);
                    break;
                case HDCAdEventType.LoadFailed:
                    record.State = HDCAdState.LoadFailed;
                    record.LoadFailures++;
                    record.LastLoadError = Error(adEvent, clock);
                    TakeLoadTime(record);
                    break;
                case HDCAdEventType.Shown:
                    record.State = HDCAdState.Showing;
                    record.Shows++;
                    break;
                case HDCAdEventType.ShowFailed:
                    record.State = HDCAdState.ShowFailed;
                    record.ShowFailures++;
                    record.LastShowError = Error(adEvent, clock);
                    break;
                case HDCAdEventType.Impression:
                    record.Impressions++;
                    break;
                case HDCAdEventType.Clicked:
                    record.Clicks++;
                    break;
                case HDCAdEventType.Paid:
                    record.Revenue += adEvent.Revenue;
                    record.Currency = adEvent.currency;
                    break;
                case HDCAdEventType.Closed:
                    // A hidden banner keeps its ad; a closed full-screen ad or popup is used up.
                    record.State = IsView(record.Format) ? HDCAdState.Loaded : HDCAdState.Closed;
                    break;
            }
        }

        internal static void Reset()
        {
            records.Clear();
            events.Clear();
        }

        private static bool IsView(string format) =>
            format == HDCAdFormat.Banner || format == HDCAdFormat.BannerView || format == HDCAdFormat.Mrec;

        // The time from the load request to its result. Loads the native side starts on its own have no request.
        private static void TakeLoadTime(HDCAdRecord record)
        {
            if (!record.AwaitingResult)
                return;
            record.AwaitingResult = false;
            record.LoadSeconds = Now - record.RequestedAt;
        }

        private static HDCAdError Error(HDCAdEvent adEvent, DateTime clock) => new HDCAdError
        {
            Code = adEvent.code,
            Message = adEvent.message ?? string.Empty,
            AdUnitId = adEvent.adUnitId,
            Clock = clock,
        };

        private static HDCAdRecord Get(string format, string id)
        {
            if (string.IsNullOrEmpty(format) || string.IsNullOrEmpty(id))
                return null;
            string key = Key(format, id);
            if (!records.TryGetValue(key, out HDCAdRecord record))
                records[key] = record = new HDCAdRecord(format, id);
            return record;
        }

        private static string Key(string format, string id) => format + "/" + id;
    }
}
