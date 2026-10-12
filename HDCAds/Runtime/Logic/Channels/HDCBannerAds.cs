using System.Collections.Generic;
using HDC.Ads.Domain;

namespace HDC.Ads.Logic
{
    internal sealed partial class HDCBannerAds : IBannerAds, IAdChannel
    {
        internal const string Placement = "FullBottom";

        private readonly HDCAdsContext context;
        private HDCRectGroup group;
        private bool autoShown;

        internal HDCBannerAds(HDCAdsContext context)
        {
            this.context = context;
        }

        private HDCAdsConfig.BannerChannel Channel => context.Config.bannerChannel ?? new HDCAdsConfig.BannerChannel();

        private HDCAdsConfig.BannerSlot Slot => Channel.fullBottom ?? new HDCAdsConfig.BannerSlot();

        public bool Show()
        {
            HDCRectGroup banner = Group();
            if (banner == null)
                return false;
            banner.Show();
            return banner.IsLoaded;
        }

        public void Hide() => group?.Hide();

        public bool Expand(bool enableClick = true) => IsActive && group != null && group.Expand(enableClick);

        public bool CanShow() => IsActive && group != null && group.IsLoaded;

        public void Initialize()
        {
            if (!Slot.autoInit)
                EnabledGroup()?.Initialize();
        }

        string IAdChannel.Key => "BN";

        string IAdChannel.Title => "Banner";

        void IAdChannel.OnSdkInitialized()
        {
            if (Slot.autoInit)
                EnabledGroup()?.Initialize();
        }

        void IAdChannel.InitializeAll() => Initialize();

        void IAdChannel.OnAdsRemoved() => group?.Hide();

        private bool IsActive => Channel.isEnabled && !context.IsAdsRemoved;

        private bool IsEnabled => IsActive && Slot.isEnabled;

        private HDCRectGroup EnabledGroup() => IsEnabled ? Group() : null;

        private HDCRectGroup Group()
        {
            if (!IsActive)
                return null;
            if (group != null)
                return group;

            HDCAdCoreConfig.FullscreenUnit unit = context.CoreConfig.bannerUnit?.fullBottom ?? new HDCAdCoreConfig.FullscreenUnit();
            List<HDCRectSource> sources = HDCAdGroups.ViewSources(context.Groups.BannerPlans());
            foreach (HDCRectSource source in sources)
                context.Placements.Record(source.Id, HDCAdChannel.Banner, Placement, source.Network.RevenueNetwork);
            group = new HDCRectGroup(sources, unit.useBackup, context.Log, context.Clock, context.MainThread);
            group.Loaded += () =>
            {
                if (Slot.autoShowOnLoad && IsEnabled && !autoShown)
                {
                    autoShown = true;
                    group.Show();
                }
            };
            return group;
        }
    }
}
