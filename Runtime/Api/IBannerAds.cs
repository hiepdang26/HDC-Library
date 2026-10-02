namespace HDC.Ads
{
    /// <summary>
    /// Banners in six slots. The bottom slot can use the native banner, which can expand; the others use
    /// plugin banners: adaptive across the top, 320x50 in the corners.
    /// </summary>
    public interface IBannerAds
    {
        /// <summary>Shows the slot's banner now, or as soon as it loads. False when the slot is off.</summary>
        bool Show(HDCBannerSlot slot = HDCBannerSlot.FullBottom);

        void Hide(HDCBannerSlot slot = HDCBannerSlot.FullBottom);

        /// <summary>Expands the shown native banner. False for plugin banners or when it cannot expand now.</summary>
        bool Expand(HDCBannerSlot slot = HDCBannerSlot.FullBottom, bool enableClick = true);

        bool CanShow(HDCBannerSlot slot = HDCBannerSlot.FullBottom);

        /// <summary>Starts loading a slot that does not load on its own (autoInit off).</summary>
        void Initialize(HDCBannerSlot slot = HDCBannerSlot.FullBottom);
    }
}
