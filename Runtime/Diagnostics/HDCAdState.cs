namespace HDC.Ads.Diagnostics
{
    /// <summary>The state of one ad instance, as its load commands and events show it.</summary>
    internal enum HDCAdState
    {
        Idle,
        Loading,
        Loaded,
        LoadFailed,
        Showing,
        ShowFailed,
        Closed,
        Destroyed,
    }
}
