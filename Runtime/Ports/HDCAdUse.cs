namespace HDC.Ads.Ports
{
    /// <summary>What a channel wants an ad for, which decides the kind of ad a network makes.</summary>
    internal enum HDCAdUse
    {
        ForceAd,
        Rewarded,
        AppOpen,
        AppResume,
        Banner,
        Mrec,
        Popup,
    }
}
