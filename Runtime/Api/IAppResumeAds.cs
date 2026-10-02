namespace HDC.Ads
{
    /// <summary>
    /// A native full-screen ad for players coming back to the app: it loads when the app goes to the
    /// background and shows once loaded. After a full-screen ad opened, a banner was tapped, or
    /// <see cref="Block"/>, the next trip to the background shows nothing: the player left because of an ad
    /// or of the game itself.
    /// </summary>
    public interface IAppResumeAds
    {
        bool IgnoreAds { get; set; }

        bool IsInitialized { get; }

        /// <summary>Starts the channel when it does not start on its own (autoInit off).</summary>
        void Initialize();

        /// <summary>Skips the next resume ad, before the game sends the player out of the app.</summary>
        void Block();
    }
}
