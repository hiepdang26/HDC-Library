using System;
using HDC.Ads.Domain;
using HDC.Ads.Infrastructure;

namespace HDC.Ads.Logic
{
    /// <summary>One ad unit of a banner or MREC slot, tracked through the SDK's events for its instance id.</summary>
    internal abstract class HDCRectSource
    {
        private readonly string format;

        protected HDCRectSource(string id, string format)
        {
            Id = id;
            this.format = format;
            HDCAdsSdk.AdEvent += OnAdEvent;
        }

        internal string Id { get; }
        internal string Format => format;
        internal abstract string AdUnitId { get; }
        internal bool IsLoaded { get; private set; }
        internal Action<HDCRectSource> Failed { get; set; }
        internal Action<HDCRectSource> LoadedAd { get; set; }

        internal abstract void Load();
        internal abstract void Show();
        internal abstract void Hide();
        internal virtual bool Expand(bool enableClick) => false;

        private void OnAdEvent(HDCAdEvent adEvent)
        {
            if (adEvent.id != Id || adEvent.format != format)
                return;

            if (adEvent.type == HDCAdEventType.Loaded)
            {
                IsLoaded = true;
                LoadedAd?.Invoke(this);
            }
            else if (adEvent.type == HDCAdEventType.LoadFailed)
            {
                Failed?.Invoke(this);
            }
        }
    }
}
