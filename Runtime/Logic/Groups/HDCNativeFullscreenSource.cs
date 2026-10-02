using HDC.Ads.Domain;
using HDC.Ads.Infrastructure;

namespace HDC.Ads.Logic
{
    /// <summary>A native full-screen ad; each show picks a layout from the unit's layout group.</summary>
    internal sealed class HDCNativeFullscreenSource : HDCFullscreenSource
    {
        private readonly string adUnitId;
        private readonly bool reloadAfterShow;
        private readonly HDCLayoutPicker layouts;
        private bool loaded;

        internal HDCNativeFullscreenSource(string id, string adUnitId, bool reloadAfterShow, HDCLayoutPicker layouts)
            : base(id, HDCAdFormat.Fullscreen)
        {
            this.adUnitId = adUnitId;
            this.reloadAfterShow = reloadAfterShow;
            this.layouts = layouts;
        }

        internal override string AdUnitId => adUnitId;

        // Tracked from events: asking the native side every frame would cost a thread hop on Android.
        internal override bool IsReady => loaded;

        internal override void Load() => HDCAdsSdk.LoadFullscreen(Id, new[] { adUnitId }, reloadAfterShow);

        protected override bool StartShow() => HDCAdsSdk.ShowFullscreen(Id, layouts.Next());

        protected override void DestroyAd()
        {
            loaded = false;
            HDCAdsSdk.DestroyFullscreen(Id);
        }

        protected override void OnOwnEvent(HDCAdEvent adEvent) =>
            loaded = adEvent.type == HDCAdEventType.Loaded || (loaded && adEvent.type != HDCAdEventType.Shown
                && adEvent.type != HDCAdEventType.ShowFailed && adEvent.type != HDCAdEventType.LoadFailed);
    }
}
