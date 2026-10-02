using System;
using HDC.Ads.Domain;

namespace HDC.Ads.Infrastructure
{
    /// <summary>An ad of <see cref="HDCAdsSdk"/>: it passes on the SDK's events that carry its instance id and format.</summary>
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

        /// <summary>Stops passing on events, before the ad is destroyed.</summary>
        protected void StopEvents() => HDCAdsSdk.AdEvent -= OnAdEvent;

        /// <summary>Lets the ad keep its own state before the event goes on.</summary>
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
