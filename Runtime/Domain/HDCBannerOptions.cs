using System;

namespace HDC.Ads.Domain
{
    /// <summary>The native banner along the bottom of the screen.</summary>
    [Serializable]
    internal sealed class HDCBannerOptions
    {
        /// <summary>Layouts such as bn_single_transparent_01, used in turn. Empty uses all of them.</summary>
        public string[] layoutNames = new string[0];

        /// <summary>Seconds between refreshes; 0 keeps the same ad.</summary>
        public int timeReload;

        /// <summary>Countdown on the banner, in seconds, before it can be closed (layouts with a countdown).</summary>
        public int timeCountdown = 5;

        /// <summary>Seconds after <see cref="HDCAdsSdk.ExpandBanner"/> before the banner can be collapsed.</summary>
        public int timeCollapse = 5;
    }
}
