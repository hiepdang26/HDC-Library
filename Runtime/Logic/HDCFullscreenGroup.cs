using System;
using System.Collections.Generic;
using HDC.Ads.Internal;
using UnityEngine;

namespace HDC.Ads
{
    /// <summary>
    /// A full-screen ad slot backed by one or more ad units in priority order. Only the first unit loads at
    /// first; when the newest loaded unit fails to load or to show and backups are allowed, the next one
    /// starts. A show takes the first ready unit.
    /// </summary>
    internal sealed class HDCFullscreenGroup
    {
        private readonly List<HDCFullscreenSource> sources;
        private readonly bool useBackup;
        private int started;
        private int remainingShows;

        internal HDCFullscreenGroup(string name, List<HDCFullscreenSource> sources, bool useBackup, int maxShowCount)
        {
            Name = name;
            this.sources = sources;
            this.useBackup = useBackup;
            remainingShows = maxShowCount > 0 ? maxShowCount : -1;
            foreach (HDCFullscreenSource source in sources)
                source.Failed = OnSourceFailed;
        }

        internal string Name { get; }
        internal bool IsEmpty => sources.Count == 0;
        internal bool IsStopped => remainingShows == 0;
        internal bool IsReady => !IsStopped && ReadySource() != null;
        internal bool IsShowing => sources.Exists(source => source.IsShowing);

        internal void Initialize()
        {
            // A channel can be on with no unit configured for it.
            if (started == 0 && !IsStopped && sources.Count > 0)
                StartNext();
        }

        /// <summary>
        /// Shows the first ready unit. <paramref name="onBeforeShow"/> runs right before the ad starts,
        /// <paramref name="onDisplayed"/> once it is on screen and <paramref name="onClosed"/> once it closes or
        /// fails, with whether a reward was earned.
        /// </summary>
        internal bool Show(Action onBeforeShow, Action onDisplayed, Action<bool> onClosed)
        {
            if (IsStopped || sources.Count == 0)
                return false;

            HDCFullscreenSource source = ReadySource();
            if (source == null)
            {
                // A single unit shows anyway: its own not-ready handling reports the failure and loads again.
                if (useBackup || started == 0)
                    return false;
                source = sources[0];
            }

            onBeforeShow?.Invoke();
            HDCAds.NotifyFullscreenOpening();
            bool displayed = false;
            return source.Show(
                () =>
                {
                    displayed = true;
                    onDisplayed?.Invoke();
                },
                rewarded =>
                {
                    // Only shows that reached the screen use up the group's show count.
                    if (displayed && remainingShows > 0 && --remainingShows == 0)
                        Destroy();
                    onClosed?.Invoke(rewarded);
                });
        }

        internal void Destroy()
        {
            foreach (HDCFullscreenSource source in sources)
                source.Destroy();
            started = sources.Count;
        }

        private HDCFullscreenSource ReadySource()
        {
            for (int i = 0; i < started && i < sources.Count; i++)
            {
                if (sources[i].IsReady)
                    return sources[i];
            }

            return null;
        }

        private void OnSourceFailed(HDCFullscreenSource source)
        {
            if (useBackup && sources.IndexOf(source) == started - 1 && started < sources.Count && !IsStopped)
                StartNext();
        }

        private void StartNext() => sources[started++].Load();
    }

    /// <summary>One ad unit of a full-screen group. Its state comes from the SDK's events for its instance id.</summary>
    internal abstract class HDCFullscreenSource
    {
        private Action onDisplayed;
        private Action<bool> onClosed;
        private bool showing;
        private bool starting;
        private bool failedWhileStarting;
        private bool rewardEarned;
        private bool destroyed;

        protected HDCFullscreenSource(string id, string format)
        {
            Id = id;
            Format = format;
            HDCAdsSdk.AdEvent += OnAdEvent;
        }

        internal string Id { get; }
        internal string Format { get; }
        internal Action<HDCFullscreenSource> Failed { get; set; }
        internal abstract bool IsReady { get; }
        internal bool IsShowing => showing;

        /// <summary>Native full-screen ads served as rewarded ads reward the player when they close.</summary>
        internal bool RewardsOnClose { get; set; }

        internal abstract void Load();

        internal bool Show(Action displayed, Action<bool> closed)
        {
            if (showing || destroyed)
                return false;

            showing = true;
            starting = true;
            failedWhileStarting = false;
            rewardEarned = false;
            onDisplayed = displayed;
            onClosed = closed;
            bool started;
            try
            {
                started = StartShow();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                started = false;
            }
            finally
            {
                starting = false;
            }

            if (!started)
            {
                showing = false;
                onDisplayed = null;
                onClosed = null;
                return false;
            }

            if (failedWhileStarting)
                Finish(false);
            return true;
        }

        internal void Destroy()
        {
            if (destroyed)
                return;
            destroyed = true;
            HDCAdsSdk.AdEvent -= OnAdEvent;
            DestroyAd();
            if (showing)
                Finish(false);
        }

        protected abstract bool StartShow();
        protected abstract void DestroyAd();
        protected virtual void OnOwnEvent(HDCAdEvent adEvent) { }

        private void OnAdEvent(HDCAdEvent adEvent)
        {
            if (adEvent.id != Id || adEvent.format != Format)
                return;

            OnOwnEvent(adEvent);
            switch (adEvent.type)
            {
                case HDCAdEventType.LoadFailed:
                    Failed?.Invoke(this);
                    break;
                case HDCAdEventType.Shown:
                    if (showing)
                    {
                        Action displayed = onDisplayed;
                        onDisplayed = null;
                        displayed?.Invoke();
                    }

                    break;
                case HDCAdEventType.Rewarded:
                    rewardEarned = true;
                    break;
                case HDCAdEventType.Closed:
                    if (showing && !starting)
                        Finish(rewardEarned || RewardsOnClose);
                    break;
                case HDCAdEventType.ShowFailed:
                    if (starting)
                        failedWhileStarting = true;
                    else if (showing)
                        Finish(false);
                    Failed?.Invoke(this);
                    break;
            }
        }

        private void Finish(bool rewarded)
        {
            showing = false;
            onDisplayed = null;
            Action<bool> closed = onClosed;
            onClosed = null;
            try
            {
                closed?.Invoke(rewarded);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }
    }

    /// <summary>A rewarded, app open or interstitial ad from the Google Mobile Ads plugin.</summary>
    internal sealed class HDCPluginFullscreenSource : HDCFullscreenSource
    {
        private readonly HDCGmaFullscreenAd ad;

        internal HDCPluginFullscreenSource(string format, HDCGmaFullscreenAd ad) : base(ad.Id, format)
        {
            this.ad = ad;
        }

        internal override bool IsReady => ad.IsReady;

        internal override void Load() => ad.Load();

        protected override bool StartShow() => ad.Show(null);

        protected override void DestroyAd() => ad.Destroy();
    }

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

    /// <summary>A native unit served as an interstitial.</summary>
    internal sealed class HDCNativeInterstitialSource : HDCFullscreenSource
    {
        private readonly string adUnitId;
        private readonly int bufferSize;
        private readonly bool autoReload;

        internal HDCNativeInterstitialSource(string id, string adUnitId, int bufferSize, bool autoReload)
            : base(id, HDCAdFormat.Interstitial)
        {
            this.adUnitId = adUnitId;
            this.bufferSize = Math.Max(1, bufferSize);
            this.autoReload = autoReload;
        }

        internal override bool IsReady => HDCAdsSdk.IsInterstitialReady(Id);

        internal override void Load() => HDCAdsSdk.LoadInterstitial(Id, new[] { adUnitId }, bufferSize, autoReload);

        protected override bool StartShow() => HDCAdsSdk.ShowInterstitial(Id);

        protected override void DestroyAd() => HDCAdsSdk.DestroyInterstitial(Id);
    }

    /// <summary>
    /// Picks the layout of each native full-screen show: layouts come out of a shuffled bag, so every layout
    /// of the group shows once before any repeats.
    /// </summary>
    internal sealed class HDCLayoutPicker
    {
        private readonly HDCAdCoreConfig.LayoutGroup group;
        private readonly HDCAdCoreConfig config;
        private readonly List<HDCAdCoreConfig.Layout> bag = new List<HDCAdCoreConfig.Layout>();

        internal HDCLayoutPicker(HDCAdCoreConfig config, string groupName)
        {
            this.config = config;
            group = config?.LayoutGroupNamed(groupName);
        }

        internal HDCFullscreenOptions Next()
        {
            HDCAdCoreConfig.Layout[] layouts = group?.layouts;
            if (layouts == null || layouts.Length == 0)
                return new HDCFullscreenOptions();

            if (bag.Count == 0)
                bag.AddRange(Array.FindAll(layouts, l => l != null && !string.IsNullOrEmpty(l.layout)));
            if (bag.Count == 0)
                return new HDCFullscreenOptions();

            int index = UnityEngine.Random.Range(0, bag.Count);
            HDCAdCoreConfig.Layout layout = bag[index];
            bag.RemoveAt(index);
            return Options(layout, config.AssetConfigNamed(layout.assetConfigName));
        }

        private static HDCFullscreenOptions Options(HDCAdCoreConfig.Layout layout, HDCAdCoreConfig.AssetConfig assets)
        {
            var options = new HDCFullscreenOptions
            {
                layoutNames = new[] { layout.layout },
                duration = layout.layoutTime,
                delay = layout.delay,
                timeUpC = layout.timeUpC,
                pauseGameplay = layout.pauseGameplay,
                showTCD = layout.showTCD,
                enableAdComeback = !layout.disableAdComeback,
            };
            if (assets != null)
            {
                options.assetVisibility = new HDCNativeAssets
                {
                    cta = assets.show_cta,
                    headline = assets.show_headline,
                    body = assets.show_body,
                    description = assets.show_description,
                    icon = assets.show_icon,
                    advertiser = assets.show_advertiser,
                    media = assets.show_media,
                    mediaImage = assets.show_media && assets.show_media_image,
                    mediaVideo = assets.show_media && assets.show_media_video,
                    price = assets.show_price,
                    store = assets.show_store,
                    starRating = assets.show_star_rating,
                };
            }

            return options;
        }
    }
}
