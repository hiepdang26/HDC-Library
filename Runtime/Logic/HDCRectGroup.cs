using System;
using System.Collections.Generic;
using System.Linq;

namespace HDC.Ads
{
    /// <summary>
    /// A banner or MREC slot backed by ad units in priority order. The first unit loads at first; if it fails
    /// before ever loading and backups are allowed, the next one starts. A shown slot displays the first
    /// loaded unit, or the first unit until one loads.
    /// </summary>
    internal sealed class HDCRectGroup
    {
        private readonly List<HDCRectSource> sources;
        private readonly bool useBackup;
        private int started;
        private bool showing;
        private HDCRectSource visible;

        internal HDCRectGroup(List<HDCRectSource> sources, bool useBackup)
        {
            this.sources = sources;
            this.useBackup = useBackup;
            foreach (HDCRectSource source in sources)
            {
                source.Failed = OnSourceFailed;
                source.LoadedAd = OnSourceLoaded;
            }
        }

        /// <summary>Raised whenever a unit loads an ad.</summary>
        internal event Action Loaded;

        internal bool IsEmpty => sources.Count == 0;
        internal bool IsLoaded => LoadedSource() != null;

        internal void Initialize()
        {
            if (started == 0 && sources.Count > 0)
                StartNext();
        }

        internal void Show()
        {
            if (sources.Count == 0)
                return;
            Initialize();
            showing = true;
            Display(LoadedSource() ?? sources[started - 1]);
        }

        internal void Hide()
        {
            showing = false;
            visible?.Hide();
            visible = null;
        }

        internal bool Expand(bool enableClick) => LoadedSource()?.Expand(enableClick) ?? false;

        internal HDCRectSource LoadedSource()
        {
            for (int i = 0; i < started; i++)
            {
                if (sources[i].IsLoaded)
                    return sources[i];
            }

            return null;
        }

        private void Display(HDCRectSource source)
        {
            if (visible == source)
                return;
            visible?.Hide();
            visible = source;
            source.Show();
        }

        private void OnSourceFailed(HDCRectSource source)
        {
            if (!useBackup || source.IsLoaded || sources.IndexOf(source) != started - 1 || started >= sources.Count)
                return;
            StartNext();
            if (showing && LoadedSource() == null)
                Display(sources[started - 1]);
        }

        private void OnSourceLoaded(HDCRectSource source)
        {
            // A higher priority unit that loads takes the place of the one on screen.
            if (showing)
                Display(LoadedSource() ?? source);
            HDCAdsLog.Run(Loaded);
        }

        private void StartNext() => sources[started++].Load();
    }

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

    /// <summary>A banner or MREC view from the Google Mobile Ads plugin.</summary>
    internal sealed class HDCPluginRectSource : HDCRectSource
    {
        private readonly string adUnitId;
        private readonly HDCBannerViewPlacement placement;

        internal HDCPluginRectSource(string id, string adUnitId, HDCBannerViewPlacement placement)
            : base(id, placement == HDCBannerViewPlacement.Mrec ? HDCAdFormat.Mrec : HDCAdFormat.BannerView)
        {
            this.adUnitId = adUnitId;
            this.placement = placement;
        }

        internal override void Load() => HDCAdsSdk.LoadBannerView(Id, adUnitId, placement);

        internal override void Show() => HDCAdsSdk.ShowBannerView(Id);

        internal override void Hide() => HDCAdsSdk.HideBannerView(Id);
    }

    /// <summary>The native banner, which can expand.</summary>
    internal sealed class HDCNativeBannerSource : HDCRectSource
    {
        private readonly string[] adUnitIds;
        private readonly HDCBannerOptions options;

        internal HDCNativeBannerSource(string id, HDCAdCoreConfig.NativeUnit unit) : base(id, HDCAdFormat.Banner)
        {
            var ids = new List<string>();
            foreach (string adUnitId in new[] { unit.id }.Concat(unit.ids ?? new string[0]))
            {
                if (!string.IsNullOrWhiteSpace(adUnitId) && !ids.Contains(adUnitId.Trim()))
                    ids.Add(adUnitId.Trim());
            }

            adUnitIds = ids.ToArray();
            options = new HDCBannerOptions { layoutNames = unit.layouts ?? new string[0], timeReload = unit.reloadTime };
        }

        internal override void Load() => HDCAdsSdk.LoadBanner(Id, adUnitIds, options);

        internal override void Show() => HDCAdsSdk.ShowBanner(Id);

        internal override void Hide() => HDCAdsSdk.HideBanner(Id);

        internal override bool Expand(bool enableClick) => HDCAdsSdk.ExpandBanner(Id, enableClick);
    }
}
