using System;
using System.Collections.Generic;
using System.Linq;
using HDC.Ads.Diagnostics;
using HDC.Ads.Domain;
using HDC.Ads.Ports;

namespace HDC.Ads.Logic
{
    internal sealed class HDCAdsContext
    {
        private readonly HashSet<string> overlayAds = new HashSet<string>(StringComparer.Ordinal);
        private bool initializeCalled;
        private bool? adsRemoved;

        internal HDCAdsContext(IClock clock, IKeyValueStore store, IMainThread mainThread, IAdsLog log, IAdsSdk sdk,
            IReadOnlyList<IAdNetwork> networks, IUnitOrderPolicy order, HDCConsentGate consent)
        {
            Clock = clock;
            Store = store;
            MainThread = mainThread;
            Log = log;
            Sdk = sdk;
            Networks = networks;
            Order = order;
            Consent = consent;
            Groups = new HDCAdGroups(this);
        }

        internal IClock Clock { get; }
        internal IKeyValueStore Store { get; }
        internal IMainThread MainThread { get; }
        internal IAdsLog Log { get; }
        internal IAdsSdk Sdk { get; }

        internal IReadOnlyList<IAdNetwork> Networks { get; }

        internal IUnitOrderPolicy Order { get; }

        internal HDCConsentGate Consent { get; }

        internal HDCAdsConfig Config { get; private set; } = new HDCAdsConfig();
        internal HDCAdCoreConfig CoreConfig { get; private set; } = new HDCAdCoreConfig();
        internal HDCAdGroups Groups { get; }
        internal HDCAdPlacements Placements { get; } = new HDCAdPlacements();

        internal bool IsInitialized { get; private set; }

        internal float LastFullscreenAdTime { get; private set; }

        internal event Action Initialized;

        internal event Action<HDCAdRevenue> Revenue;

        internal event Action SdkReady;

        internal event Action AdsRemoved;

        internal event Action FullscreenOpening;

        internal event Action BannerClicked;

        internal event Action AdsNotAllowed;

        internal bool IsAdsRemoved
        {
            get
            {
                if (adsRemoved == null && Store.TryGetInt(HDCAdNames.AdsRemovedKey, out int removed))
                    adsRemoved = removed == 1;
                return adsRemoved ?? false;
            }
        }

        internal bool InitializeCalled => initializeCalled;

        internal void WhenInitialized(Action onInitialized)
        {
            if (onInitialized == null)
                return;
            if (IsInitialized)
                onInitialized();
            else
                Initialized += onInitialized;
        }

        internal void Initialize(string adsConfigJson, string coreConfigJson, Action onInitialized)
        {
            if (initializeCalled)
            {
                Log.Warning("HDCAds.Initialize was already called");
                WhenInitialized(onInitialized);
                return;
            }

            initializeCalled = true;
            HDCConfigReport.Applied(adsConfigJson, coreConfigJson);
            if (onInitialized != null)
                Initialized += onInitialized;
            Consent.UseConfig(HDCAdsConfig.Parse(adsConfigJson));
            Consent.Start();
            Consent.WhenDone(() => StartAds(adsConfigJson, coreConfigJson));
        }

        private void StartAds(string adsConfigJson, string coreConfigJson)
        {
            if (!Consent.CanRequestAds)
            {
                Log.Warning("consent does not allow ad requests, so no ad loads this session");
                HDCCallbacks.Run(AdsNotAllowed, Log);
                return;
            }

            Config = HDCAdsConfig.Parse(adsConfigJson, out string adsError);
            if (adsError != null)
                Log.Warning("invalid ads config: " + adsError);
            CoreConfig = HDCAdCoreConfig.Parse(coreConfigJson, out string coreError);
            if (coreError != null)
                Log.Warning("invalid ad core config: " + coreError);
            Sdk.AdEvent += OnAdEvent;
            Sdk.Initialize(OnSdkReady);
        }

        internal void SetAdsRemoved(bool removed)
        {
            adsRemoved = removed;
            Store.SetInt(HDCAdNames.AdsRemovedKey, removed ? 1 : 0);
            Store.Save();
            if (removed)
                AdsRemoved?.Invoke();
        }

        internal void NotifyFullscreenOpening() => FullscreenOpening?.Invoke();

        internal bool HasOverlayAd => overlayAds.Count > 0;

        internal void SetOverlayAd(string instanceId, bool onScreen)
        {
            if (string.IsNullOrEmpty(instanceId))
                return;
            if (onScreen)
                overlayAds.Add(instanceId);
            else
                overlayAds.Remove(instanceId);
        }

        internal IAdNetwork Network(string unitKey) =>
            unitKey == null ? null : Networks.FirstOrDefault(network => network.UnitKey == unitKey);

        internal int Count(string key) => Store.TryGetInt(key, out int value) ? value : 0;

        private void OnSdkReady()
        {
            IsInitialized = true;
            SdkReady?.Invoke();
            Action callbacks = Initialized;
            Initialized = null;
            if (callbacks == null)
                return;
            foreach (Action callback in callbacks.GetInvocationList())
                HDCCallbacks.Run(callback, Log);
        }

        private void OnAdEvent(HDCAdEvent adEvent)
        {
            if (adEvent.type == HDCAdEventType.Paid)
            {
                RaiseRevenue(adEvent);
                return;
            }

            bool fullscreen = adEvent.format == HDCAdFormat.Interstitial || adEvent.format == HDCAdFormat.Fullscreen
                || adEvent.format == HDCAdFormat.Rewarded || adEvent.format == HDCAdFormat.AppOpen;
            if (fullscreen && adEvent.type == HDCAdEventType.Closed)
                LastFullscreenAdTime = Clock.RealTime;
            else if (adEvent.type == HDCAdEventType.Clicked
                && (adEvent.format == HDCAdFormat.Banner || adEvent.format == HDCAdFormat.BannerView))
                BannerClicked?.Invoke();
        }

        private void RaiseRevenue(HDCAdEvent adEvent)
        {
            Action<HDCAdRevenue> handlers = Revenue;
            if (handlers == null)
                return;

            (HDCAdChannel channel, string position, string network, string group) = Placements.Find(adEvent.id);
            var revenue = new HDCAdRevenue(channel, position, adEvent.format, network ?? HDCAdRevenue.AdMob, adEvent.adSource,
                adEvent.adUnitId, adEvent.Revenue, adEvent.currency, adEvent.precision, group);
            foreach (Action<HDCAdRevenue> handler in handlers.GetInvocationList())
                HDCCallbacks.Run(() => handler(revenue), Log);
        }
    }
}
