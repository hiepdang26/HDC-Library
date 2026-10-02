namespace HDC.Ads.Domain
{
    /// <summary>
    /// What a network needs to plan and make one ad of a slot: the slot, the network's own unit from the configs
    /// and how the slot reloads.
    /// </summary>
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

        /// <summary>The force ad or popup group; empty for the other channels.</summary>
        internal string SlotName { get; }

        /// <summary>The network's unit: an AdmobUnit under "admobUnit", a NativeUnit under "androidUnit".</summary>
        internal object Unit { get; }

        /// <summary>False when the slot loads once (disablePostInitReload).</summary>
        internal bool ReloadAfterShow { get; }

        /// <summary>The ad core config, whose layout groups native full-screen ads show with.</summary>
        internal HDCAdCoreConfig CoreConfig { get; }

        /// <summary>The slot of a banner unit.</summary>
        internal HDCBannerSlot BannerSlot { get; }
    }
}
