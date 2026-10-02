using System;
using System.Collections.Generic;
using HDC.Ads.Domain;

namespace HDC.Ads.Logic
{
    internal sealed partial class HDCBannerAds : IBannerAds, IAdChannel
    {
        private static readonly HDCBannerSlot[] Slots = (HDCBannerSlot[])Enum.GetValues(typeof(HDCBannerSlot));

        private readonly HDCAdsContext context;
        private readonly Dictionary<HDCBannerSlot, HDCRectGroup> groups = new Dictionary<HDCBannerSlot, HDCRectGroup>();
        private readonly HashSet<HDCBannerSlot> autoShown = new HashSet<HDCBannerSlot>();

        internal HDCBannerAds(HDCAdsContext context)
        {
            this.context = context;
        }

        private HDCAdsConfig.BannerChannel Channel => context.Config.bannerChannel ?? new HDCAdsConfig.BannerChannel();

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

        public bool Expand(HDCBannerSlot slot = HDCBannerSlot.FullBottom, bool enableClick = true) =>
            EnabledGroup(slot)?.Expand(enableClick) ?? false;

        public bool CanShow(HDCBannerSlot slot = HDCBannerSlot.FullBottom) => EnabledGroup(slot)?.IsLoaded ?? false;

        public void Initialize(HDCBannerSlot slot = HDCBannerSlot.FullBottom)
        {
            if (!Channel.Slot(slot).autoInit)
                EnabledGroup(slot)?.Initialize();
        }

        string IAdChannel.Key => "BN";

        string IAdChannel.Title => "Banner";

        void IAdChannel.OnSdkInitialized()
        {
            foreach (HDCBannerSlot slot in Slots)
            {
                if (Channel.Slot(slot).autoInit)
                    EnabledGroup(slot)?.Initialize();
            }
        }

        void IAdChannel.InitializeAll()
        {
            foreach (HDCBannerSlot slot in Slots)
                Initialize(slot);
        }

        void IAdChannel.OnAdsRemoved()
        {
            foreach (HDCRectGroup group in groups.Values)
                group.Hide();
        }

        private bool IsEnabled(HDCBannerSlot slot) => Channel.isEnabled && Channel.Slot(slot).isEnabled && !context.IsAdsRemoved;

        private HDCRectGroup EnabledGroup(HDCBannerSlot slot)
        {
            if (!IsEnabled(slot))
                return null;
            if (groups.TryGetValue(slot, out HDCRectGroup group))
                return group;

            HDCAdCoreConfig.FullscreenUnit unit = context.CoreConfig.bannerUnit?.Slot(slot) ?? new HDCAdCoreConfig.FullscreenUnit();
            List<HDCRectSource> sources = HDCAdGroups.ViewSources(context.Groups.BannerPlans(slot));
            foreach (HDCRectSource source in sources)
                context.Placements.Record(source.Id, HDCAdChannel.Banner, slot.ToString(), source.Network.RevenueNetwork);
            group = new HDCRectGroup(sources, unit.useBackup);
            group.Loaded += () =>
            {
                if (Channel.Slot(slot).autoShowOnLoad && IsEnabled(slot) && autoShown.Add(slot))
                    group.Show();
            };
            groups[slot] = group;
            return group;
        }
    }
}
