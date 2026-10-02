using System;
using HDC.Ads.Domain;
using HDC.Ads.Ports;
using UnityEngine;

namespace HDC.Ads.Tests
{
    /// <summary>
    /// A full-screen ad or a view of <see cref="HDCFakeNetwork"/>. It counts what the channels ask of it, and the
    /// test plays the SDK: loaded, failed, shown, closed and paid go out through the fake SDK like real events.
    /// </summary>
    internal sealed class HDCFakeAd : IFullscreenAd, IViewAd
    {
        private readonly HDCFakeSdk sdk;

        internal HDCFakeAd(HDCAdPlan plan, HDCFakeSdk sdk)
        {
            Use = plan.Use;
            Id = plan.InstanceId;
            Format = plan.Format;
            AdUnitId = plan.AdUnitId;
            this.sdk = sdk;
            sdk.AdEvent += OnAdEvent;
        }

        public event Action<HDCAdEvent> Event;

        internal HDCAdUse Use { get; }

        public string Id { get; }

        public string Format { get; }

        public string AdUnitId { get; }

        public bool IsReady { get; private set; }

        public Vector2 SizeInPixels => Vector2.zero;

        internal int Loads { get; private set; }

        internal int Shows { get; private set; }

        internal int Hides { get; private set; }

        internal bool Destroyed { get; private set; }

        public void Load() => Loads++;

        public bool Show()
        {
            Shows++;
            return IsReady;
        }

        void IViewAd.Show() => Shows++;

        public void Hide() => Hides++;

        public bool Expand(bool enableClick) => false;

        public void Move(HDCAdPosition position)
        {
        }

        public void Move(Vector2 screenPoint)
        {
        }

        public void Destroy()
        {
            Destroyed = true;
            sdk.AdEvent -= OnAdEvent;
        }

        internal void Loaded() => Send(HDCAdEventType.Loaded);

        internal void FailedToLoad() => Send(HDCAdEventType.LoadFailed);

        internal void Displayed() => Send(HDCAdEventType.Shown);

        internal void Closed() => Send(HDCAdEventType.Closed);

        internal void Paid(long valueMicros) =>
            sdk.Emit(new HDCAdEvent { id = Id, format = Format, type = HDCAdEventType.Paid, valueMicros = valueMicros, currency = "USD" });

        private void Send(string type) => sdk.Emit(new HDCAdEvent { id = Id, format = Format, type = type });

        private void OnAdEvent(HDCAdEvent adEvent)
        {
            if (adEvent.id != Id || adEvent.format != Format)
                return;
            if (adEvent.type == HDCAdEventType.Loaded)
                IsReady = true;
            else if (adEvent.type == HDCAdEventType.Shown || adEvent.type == HDCAdEventType.LoadFailed)
                IsReady = false;
            Event?.Invoke(adEvent);
        }
    }
}
