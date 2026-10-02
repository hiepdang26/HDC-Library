namespace HDC.Ads
{
    /// <summary>The channels of <see cref="HDCAds"/>.</summary>
    public enum HDCAdChannel
    {
        /// <summary>An ad that no channel showed, so none can claim it.</summary>
        Unknown,
        ForceAd,
        Rewarded,
        AppLaunch,
        AppResume,
        Banner,
        Mrec,
        Popup,
    }
}
