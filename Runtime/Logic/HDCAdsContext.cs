using System;
using HDC.Ads.Diagnostics;
using HDC.Ads.Domain;
using HDC.Ads.Ports;
using UnityEngine;

namespace HDC.Ads.Logic
{
    /// <summary>
    /// What the channels share: the configs, the ad removal flag, the full-screen bookkeeping, the groups made so
    /// far and the ports to the world outside. <see cref="HDCAds"/> hands the game's calls to it.
    /// </summary>
    internal sealed class HDCAdsContext
    {
        private const string RemovedAdsKey = "REMOVEADS";

        private bool initializeCalled;
        private bool? adsRemoved;

        internal HDCAdsContext(IClock clock, IKeyValueStore store, IMainThread mainThread, IAdsLog log, IAdsSdk sdk)
        {
            Clock = clock;
            Store = store;
            MainThread = mainThread;
            Log = log;
            Sdk = sdk;
            Groups = new HDCAdGroups(this);
        }

        internal IClock Clock { get; }
        internal IKeyValueStore Store { get; }
        internal IMainThread MainThread { get; }
        internal IAdsLog Log { get; }
        internal IAdsSdk Sdk { get; }

        internal HDCAdsConfig Config { get; private set; } = new HDCAdsConfig();
        internal HDCAdCoreConfig CoreConfig { get; private set; } = new HDCAdCoreConfig();
        internal HDCAdGroups Groups { get; }
        internal HDCAdPlacements Placements { get; } = new HDCAdPlacements();

        /// <summary>True once the SDK is ready and the channels have started.</summary>
        internal bool IsInitialized { get; private set; }

        /// <summary>Real time, in seconds since startup, when the last full-screen ad closed.</summary>
        internal float LastFullscreenAdTime { get; private set; }

        /// <summary>Once the SDK is ready, after the channels started.</summary>
        internal event Action Initialized;

        internal event Action<HDCAdRevenue> Revenue;

        /// <summary>The SDK is ready: the channels start, before <see cref="Initialized"/>.</summary>
        internal event Action SdkReady;

        /// <summary>The player bought ad removal: the channels it covers hide their ads.</summary>
        internal event Action AdsRemoved;

        /// <summary>Right before any channel shows a full-screen ad.</summary>
        internal event Action FullscreenOpening;

        /// <summary>The player tapped a banner, which may take them out of the app.</summary>
        internal event Action BannerClicked;

        /// <summary>True after the player bought ad removal. Rewarded ads still show.</summary>
        internal bool IsAdsRemoved
        {
            get
            {
                // The store cannot be read from constructors and field initializers: read it next time then.
                if (adsRemoved == null && Store.TryGetInt(RemovedAdsKey, out int removed))
                    adsRemoved = removed == 1;
                return adsRemoved ?? false;
            }
        }

        /// <summary>Applies both configs and starts the SDK. Only the first call counts.</summary>
        internal void Initialize(string adsConfigJson, string coreConfigJson, Action onInitialized)
        {
            if (initializeCalled)
            {
                Debug.LogWarning("[HDCAds] HDCAds.Initialize was already called");
                if (onInitialized != null)
                {
                    if (IsInitialized)
                        onInitialized();
                    else
                        Initialized += onInitialized;
                }

                return;
            }

            initializeCalled = true;
            HDCConfigReport.Applied(adsConfigJson, coreConfigJson);
            Config = HDCAdsConfig.Parse(adsConfigJson);
            CoreConfig = HDCAdCoreConfig.Parse(coreConfigJson);
            if (onInitialized != null)
                Initialized += onInitialized;
            Sdk.AdEvent += OnAdEvent;
            Sdk.Initialize(OnSdkReady);
        }

        /// <summary>Records the ad removal purchase, or undoes it, and hides the ads it covers.</summary>
        internal void SetAdsRemoved(bool removed)
        {
            adsRemoved = removed;
            Store.SetInt(RemovedAdsKey, removed ? 1 : 0);
            Store.Save();
            if (removed)
                AdsRemoved?.Invoke();
        }

        internal void NotifyFullscreenOpening() => FullscreenOpening?.Invoke();

        /// <summary>A count kept across sessions; 0 when there is none yet or the store cannot be read.</summary>
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
                HDCCallbacks.Run(callback);
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

            (HDCAdChannel channel, string position) = Placements.Find(adEvent.id);
            var revenue = new HDCAdRevenue(channel, position, adEvent.format, HDCAdRevenue.AdMob, adEvent.adSource,
                adEvent.adUnitId, adEvent.Revenue, adEvent.currency, adEvent.precision);
            // One handler's exception must not keep the revenue from the others.
            foreach (Action<HDCAdRevenue> handler in handlers.GetInvocationList())
                HDCCallbacks.Run(() => handler(revenue));
        }
    }
}
