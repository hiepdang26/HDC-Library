namespace HDC.Ads.Domain
{
    internal sealed class HDCAdUnitSpec
    {
        internal HDCAdUnitSpec(string slotName, object unit, bool reloadAfterShow, HDCAdCoreConfig coreConfig,
            HDCBannerSlot bannerSlot = HDCBannerSlot.FullBottom)
        {
            SlotName = slotName ?? "";
            Unit = unit;
            ReloadAfterShow = reloadAfterShow;
            CoreConfig = coreConfig;
            BannerSlot = bannerSlot;
        }

        internal string SlotName { get; }

        internal object Unit { get; }

        internal bool ReloadAfterShow { get; }

        internal HDCAdCoreConfig CoreConfig { get; }

        internal HDCBannerSlot BannerSlot { get; }
    }
}
