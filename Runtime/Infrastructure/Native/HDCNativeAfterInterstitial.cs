using System;
using HDC.Ads.Domain;
using HDC.Ads.Ports;

namespace HDC.Ads.Infrastructure
{
    internal sealed class HDCNativeAfterInterstitial
    {
        private readonly HDCNativeFullscreenAd ad;
        private readonly HDCLayoutPicker layouts;
        private bool loading;
        private bool showWhenLoaded;
        private bool showing;
        private bool destroyWhenClosed;

        internal HDCNativeAfterInterstitial(string id, string adUnitId, HDCLayoutPicker layouts)
        {
            this.layouts = layouts;
            ad = new HDCNativeFullscreenAd(id, adUnitId, true, layouts);
            ad.Event += OnEvent;
        }

        internal IFullscreenAd Ad => ad;

        internal event Action Opening;

        internal void BeforeInterstitialShow(string interstitialId)
        {
            if (ad.IsReady)
                HDCAdsSdk.ShowFullscreenWithInterstitial(interstitialId, ad.Id, layouts.Next(ad.AdSourceId));
        }

        internal void InterstitialNotShown(string interstitialId) => HDCAdsSdk.CancelShowWithInterstitial(interstitialId);

        internal void InterstitialShown(string interstitialId)
        {
            if (HDCAdsSdk.TakeShownWithInterstitial(interstitialId))
                return;
            if (ad.IsReady)
            {
                ShowNow();
                return;
            }

            showWhenLoaded = true;
            if (loading)
                return;
            loading = true;
            ad.Load();
        }

        internal void Destroy()
        {
            showWhenLoaded = false;
            if (showing)
            {
                destroyWhenClosed = true;
                return;
            }

            ad.Event -= OnEvent;
            ad.Destroy();
        }

        private void ShowNow()
        {
            Opening?.Invoke();
            ad.Show();
        }

        private void OnEvent(HDCAdEvent adEvent)
        {
            switch (adEvent.type)
            {
                case HDCAdEventType.Loaded:
                    loading = false;
                    if (!showWhenLoaded)
                        break;
                    showWhenLoaded = false;
                    ShowNow();
                    break;
                case HDCAdEventType.LoadFailed:
                    loading = false;
                    showWhenLoaded = false;
                    break;
                case HDCAdEventType.Shown:
                    showing = true;
                    break;
                case HDCAdEventType.ShowFailed:
                    showing = false;
                    break;
                case HDCAdEventType.Closed:
                    showing = false;
                    loading = true;
                    if (destroyWhenClosed)
                        Destroy();
                    break;
            }
        }
    }
}
