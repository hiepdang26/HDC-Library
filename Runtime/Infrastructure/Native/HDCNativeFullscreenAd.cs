using HDC.Ads.Domain;
using HDC.Ads.Ports;

namespace HDC.Ads.Infrastructure
{
    internal sealed class HDCNativeFullscreenAd : HDCSdkAd, IFullscreenAd, IShowWithLeader
    {
        private readonly bool reloadAfterShow;
        private readonly HDCLayoutPicker layouts;
        private bool loaded;

        internal HDCNativeFullscreenAd(string id, string adUnitId, bool reloadAfterShow, HDCLayoutPicker layouts)
            : base(id, HDCAdFormat.Fullscreen)
        {
            AdUnitId = adUnitId;
            this.reloadAfterShow = reloadAfterShow;
            this.layouts = layouts;
        }

        public string AdUnitId { get; }

        public bool IsReady => loaded;

        internal string AdSourceId { get; private set; } = "";

        public void Load() => HDCAdsSdk.LoadFullscreen(Id, new[] { AdUnitId }, reloadAfterShow);

        public bool Show() => HDCAdsSdk.ShowFullscreen(Id, layouts.Next(AdSourceId));

        public void ShowWithLeader(string leaderId) => HDCAdsSdk.ShowFullscreenWithInterstitial(leaderId, Id, layouts.Next(AdSourceId));

        public void CancelShowWithLeader(string leaderId) => HDCAdsSdk.CancelShowWithInterstitial(leaderId);

        public bool ShownWithLeader(string leaderId) => HDCAdsSdk.TakeShownWithInterstitial(leaderId);

        public void Destroy()
        {
            StopEvents();
            loaded = false;
            HDCAdsSdk.DestroyFullscreen(Id);
        }

        protected override void OnOwnEvent(HDCAdEvent adEvent)
        {
            if (adEvent.type == HDCAdEventType.Loaded)
                AdSourceId = adEvent.adSourceId ?? "";
            loaded = adEvent.type == HDCAdEventType.Loaded || (loaded && adEvent.type != HDCAdEventType.Shown
                && adEvent.type != HDCAdEventType.ShowFailed && adEvent.type != HDCAdEventType.LoadFailed);
        }
    }
}
