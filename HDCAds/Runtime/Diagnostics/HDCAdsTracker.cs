using System;
using System.Collections.Generic;
using HDC.Ads.Domain;
using UnityEngine;

namespace HDC.Ads.Diagnostics
{
    internal static class HDCAdsTracker
    {
        internal const int EventCapacity = 300;

        private static readonly Dictionary<string, HDCAdRecord> records = new Dictionary<string, HDCAdRecord>();
        private static readonly List<HDCTrackedEvent> events = new List<HDCTrackedEvent>(EventCapacity);

        internal static IReadOnlyList<HDCTrackedEvent> Events => events;

        internal static long TotalEvents { get; private set; }

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
            TotalEvents++;

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
                    record.State = IsView(record.Format) ? HDCAdState.Loaded : HDCAdState.Closed;
                    break;
            }
        }

        internal static void ClearEvents()
        {
            events.Clear();
            TotalEvents = 0;
        }

        internal static void Reset()
        {
            records.Clear();
            ClearEvents();
        }

        private static bool IsView(string format) =>
            format == HDCAdFormat.Banner || format == HDCAdFormat.BannerView || format == HDCAdFormat.Mrec;

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
