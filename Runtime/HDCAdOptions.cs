using System;

namespace HDC.Ads
{
    /// <summary>
    /// How a fullscreen native ad shows. Field names match the ads config JSON; defaults match the native
    /// library's.
    /// </summary>
    [Serializable]
    public sealed class HDCFullscreenOptions
    {
        /// <summary>
        /// SINGLE (when empty), TRANSPARENT, CTR, MULTIPLE, SEQUENCE, OVERLAY, CLS, NAV, ... CLS, NAV,
        /// progress and loop layouts pick their own mode.
        /// </summary>
        public string mode = "";

        /// <summary>Layouts to show, such as fs_single_cls_01. Empty uses the default layout.</summary>
        public string[] layoutNames = new string[0];

        /// <summary>Countdown seconds before the ad can be closed, or closes with <see cref="autoClose"/>.</summary>
        public double duration = 5;

        /// <summary>One duration per layout, for layouts shown in sequence.</summary>
        public double[] durations = new double[0];

        /// <summary>auto, portrait or landscape.</summary>
        public string orientation = "auto";

        public bool autoClose;
        public bool pauseGameplay = true;

        /// <summary>Keeps the ad after the player returns from a click, instead of closing it.</summary>
        public bool enableAdComeback = true;

        /// <summary>Shows the countdown, or the progress bar of progress layouts.</summary>
        public bool showTCD = true;

        /// <summary>Extra seconds after the countdown before the ad can be closed.</summary>
        public double delay;

        /// <summary>Progress layouts: seconds before the store button shows, added ahead of the countdown.</summary>
        public int timeUpC;

        /// <summary>Which assets react to a tap.</summary>
        public HDCNativeAssets clickAssets = new HDCNativeAssets();

        /// <summary>Which assets are visible.</summary>
        public HDCNativeAssets assetVisibility = new HDCNativeAssets();
    }

    /// <summary>A switch per native ad asset. Price, store and star rating only apply to visibility.</summary>
    [Serializable]
    public sealed class HDCNativeAssets
    {
        public bool cta = true;
        public bool headline = true;
        public bool body = true;
        public bool description = true;
        public bool icon = true;
        public bool advertiser = true;
        public bool media = true;
        public bool mediaImage = true;
        public bool mediaVideo = true;
        public bool price = true;
        public bool store = true;
        public bool starRating = true;
    }

    /// <summary>
    /// A native popup over the game. Sizes are dp. When <see cref="x"/> and <see cref="y"/> are both in 0..1
    /// they are the popup's center relative to the screen, with y measured from the bottom; otherwise they
    /// are its top-left corner in dp.
    /// </summary>
    [Serializable]
    public sealed class HDCPopupOptions
    {
        /// <summary>Layout such as popup_single_manual_01.</summary>
        public string layout = "popup_single_manual_01";

        /// <summary>Seconds before the popup can be closed.</summary>
        public int timeShow = 5;

        /// <summary>Seconds between refreshes while shown; 0 keeps the same ad.</summary>
        public int timeReload;

        public float x = 16;
        public float y = 96;
        public float width = 320;
        public float height = 280;

        /// <summary>Closes the popup when <see cref="timeShow"/> runs out.</summary>
        public bool autoClose;

        /// <summary>Makes the whole popup open the ad on tap.</summary>
        public bool enableCtrOverlay;
    }

    /// <summary>The native banner along the bottom of the screen.</summary>
    [Serializable]
    public sealed class HDCBannerOptions
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
