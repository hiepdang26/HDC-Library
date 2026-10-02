using System.Collections.Generic;
using HDC.Ads.Diagnostics;
using HDC.Ads.Domain;

namespace HDC.Ads.Logic
{
    /// <summary>The banner channel behind <see cref="IBannerAds"/>.</summary>
    internal sealed class HDCBannerAds : IBannerAds
    {
        private readonly HDCAdsContext context;
        private readonly Dictionary<HDCBannerSlot, HDCRectGroup> groups = new Dictionary<HDCBannerSlot, HDCRectGroup>();
        private readonly HashSet<HDCBannerSlot> autoShown = new HashSet<HDCBannerSlot>();

        internal HDCBannerAds(HDCAdsContext context)
        {
            this.context = context;
        }

        private HDCAdsConfig.BannerChannel Channel => context.Config.bannerChannel ?? new HDCAdsConfig.BannerChannel();

        /// <summary>Shows the slot's banner now, or as soon as it loads. False when the slot is off.</summary>
        public bool Show(HDCBannerSlot slot = HDCBannerSlot.FullBottom)
        {
            HDCRectGroup group = EnabledGroup(slot);
            if (group == null)
                return false;
            group.Show();
            return true;
        }

        public void Hide(HDCBannerSlot slot = HDCBannerSlot.FullBottom)
        {
            if (groups.TryGetValue(slot, out HDCRectGroup group))
                group.Hide();
        }

        /// <summary>Expands the shown native banner. False for plugin banners or when it cannot expand now.</summary>
        public bool Expand(HDCBannerSlot slot = HDCBannerSlot.FullBottom, bool enableClick = true) =>
            EnabledGroup(slot)?.Expand(enableClick) ?? false;

        public bool CanShow(HDCBannerSlot slot = HDCBannerSlot.FullBottom) => EnabledGroup(slot)?.IsLoaded ?? false;

        /// <summary>Starts loading a slot that does not load on its own (autoInit off).</summary>
        public void Initialize(HDCBannerSlot slot = HDCBannerSlot.FullBottom)
        {
            if (!Channel.Slot(slot).autoInit)
                EnabledGroup(slot)?.Initialize();
        }

        /// <summary>A slot's configs and ad units, for the debug panel.</summary>
        internal HDCDebugInfo Describe(HDCBannerSlot slot)
        {
            HDCAdsConfig.BannerSlot config = Channel.Slot(slot);
            HDCDebugInfo info = new HDCDebugInfo()
                .Section("Configs")
                .Needed("Channel Enabled", Channel.isEnabled)
                .Needed("Slot Enabled", config.isEnabled)
                .Line("Auto Init", config.autoInit)
                .Line("Auto Show On Load", config.autoShowOnLoad)
                .Section("Runtime")
                .Line("Can Show", CanShow(slot))
                .Section("Gates")
                .Gate("Disabled", !IsEnabled(slot))
                .Gate("Ads Removed", context.IsAdsRemoved)
                .Section("Placement " + slot);
            if (groups.TryGetValue(slot, out HDCRectGroup group))
                group.DescribeTo(info);
            else
                info.Add("State", "Not started", HDCDebugTone.Muted);
            return info;
        }

        internal void OnSdkInitialized()
        {
            foreach (HDCBannerSlot slot in new[]
                     {
                         HDCBannerSlot.FullBottom, HDCBannerSlot.FullTop, HDCBannerSlot.TopLeft,
                         HDCBannerSlot.TopRight, HDCBannerSlot.BottomLeft, HDCBannerSlot.BottomRight,
                     })
            {
                if (Channel.Slot(slot).autoInit)
                    EnabledGroup(slot)?.Initialize();
            }
        }

        internal void HideAll()
        {
            foreach (HDCRectGroup group in groups.Values)
                group.Hide();
        }

        /// <summary>A slot's group once the channel made it, for the debug panel.</summary>
        internal HDCRectGroup ExistingGroup(HDCBannerSlot slot) => groups.TryGetValue(slot, out HDCRectGroup group) ? group : null;

        internal bool IsSlotEnabled(HDCBannerSlot slot) => IsEnabled(slot);

        private bool IsEnabled(HDCBannerSlot slot) => Channel.isEnabled && Channel.Slot(slot).isEnabled && !context.IsAdsRemoved;

        private HDCRectGroup EnabledGroup(HDCBannerSlot slot)
        {
            if (!IsEnabled(slot))
                return null;
            if (groups.TryGetValue(slot, out HDCRectGroup group))
                return group;

            HDCAdCoreConfig.FullscreenUnit unit = context.CoreConfig.bannerUnit?.Slot(slot) ?? new HDCAdCoreConfig.FullscreenUnit();
            var sources = new List<HDCRectSource>();
            // Only the bottom slot has a native unit; for the others priority 0 is the plugin and 1 a removed network.
            IEnumerable<int> order = slot == HDCBannerSlot.FullBottom
                ? HDCAdGroups.Order(unit.mediationPriority, unit.useBackup)
                : unit.mediationPriority == HDCAdGroups.PluginUnit || unit.useBackup ? new[] { HDCAdGroups.PluginUnit } : new int[0];
            foreach (int priority in order)
            {
                if (priority == HDCAdGroups.PluginUnit && !string.IsNullOrEmpty(unit.admobUnit?.id))
                    sources.Add(new HDCPluginRectSource("bn_plugin_" + slot, unit.admobUnit.id, Placement(slot)));
                else if (priority == HDCAdGroups.NativeUnit && slot == HDCBannerSlot.FullBottom && HasNativeUnit(unit.androidUnit))
                    sources.Add(new HDCNativeBannerSource("bn_native", unit.androidUnit));
            }

            foreach (HDCRectSource source in sources)
                context.Placements.Record(source.Id, HDCAdChannel.Banner, slot.ToString());
            group = new HDCRectGroup(sources, unit.useBackup);
            group.Loaded += () =>
            {
                if (Channel.Slot(slot).autoShowOnLoad && IsEnabled(slot) && autoShown.Add(slot))
                    group.Show();
            };
            groups[slot] = group;
            return group;
        }

        private static bool HasNativeUnit(HDCAdCoreConfig.NativeUnit unit) =>
            unit != null && (!string.IsNullOrEmpty(unit.id) || (unit.ids != null && unit.ids.Length > 0));

        private static HDCBannerViewPlacement Placement(HDCBannerSlot slot)
        {
            switch (slot)
            {
                case HDCBannerSlot.FullTop: return HDCBannerViewPlacement.FullTop;
                case HDCBannerSlot.TopLeft: return HDCBannerViewPlacement.TopLeft;
                case HDCBannerSlot.TopRight: return HDCBannerViewPlacement.TopRight;
                case HDCBannerSlot.BottomLeft: return HDCBannerViewPlacement.BottomLeft;
                case HDCBannerSlot.BottomRight: return HDCBannerViewPlacement.BottomRight;
                default: return HDCBannerViewPlacement.FullBottom;
            }
        }
    }
}
