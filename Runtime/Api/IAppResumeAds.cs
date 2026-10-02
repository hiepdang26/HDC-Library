namespace HDC.Ads
{
    public interface IAppResumeAds
    {
        bool IgnoreAds { get; set; }

        bool IsInitialized { get; }

        void Initialize();

        void Block();
    }
}
