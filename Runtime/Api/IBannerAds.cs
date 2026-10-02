namespace HDC.Ads
{
    public interface IBannerAds
    {
        bool Show(HDCBannerSlot slot = HDCBannerSlot.FullBottom);

        void Hide(HDCBannerSlot slot = HDCBannerSlot.FullBottom);

        bool Expand(HDCBannerSlot slot = HDCBannerSlot.FullBottom, bool enableClick = true);

        bool CanShow(HDCBannerSlot slot = HDCBannerSlot.FullBottom);

        void Initialize(HDCBannerSlot slot = HDCBannerSlot.FullBottom);
    }
}
