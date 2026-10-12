namespace HDC.Ads
{
    public interface IBannerAds
    {
        bool Show();

        void Hide();

        bool Expand(bool enableClick = true);

        bool CanShow();

        void Initialize();
    }
}
