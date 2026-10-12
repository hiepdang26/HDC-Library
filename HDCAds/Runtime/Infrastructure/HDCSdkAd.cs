using System;
using HDC.Ads.Domain;

namespace HDC.Ads.Infrastructure
{
    internal abstract class HDCSdkAd
    {
        protected HDCSdkAd(string id, string format)
        {
            Id = id;
            Format = format;
            HDCAdsSdk.AdEvent += OnAdEvent;
        }

        public string Id { get; }
        public string Format { get; }

        public event Action<HDCAdEvent> Event;

        protected void StopEvents() => HDCAdsSdk.AdEvent -= OnAdEvent;

        protected virtual void OnOwnEvent(HDCAdEvent adEvent)
        {
        }

        private void OnAdEvent(HDCAdEvent adEvent)
        {
            if (adEvent.id != Id || adEvent.format != Format)
                return;
            OnOwnEvent(adEvent);
            Event?.Invoke(adEvent);
        }
    }
}
