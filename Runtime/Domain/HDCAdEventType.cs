namespace HDC.Ads.Domain
{
    /// <summary>Values of <see cref="HDCAdEvent.type"/>.</summary>
    internal static class HDCAdEventType
    {
        public const string Initialized = "Initialized";
        public const string Loaded = "Loaded";
        public const string LoadFailed = "LoadFailed";
        public const string Shown = "Shown";
        public const string ShowFailed = "ShowFailed";
        public const string Impression = "Impression";
        public const string Clicked = "Clicked";
        public const string Paid = "Paid";

        /// <summary>The player earned a rewarded ad's reward.</summary>
        public const string Rewarded = "Rewarded";

        /// <summary>A full-screen ad or popup closed, or a banner was hidden.</summary>
        public const string Closed = "Closed";
    }
}
