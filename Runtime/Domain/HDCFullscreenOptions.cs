using System;

namespace HDC.Ads.Domain
{
    /// <summary>
    /// How a fullscreen native ad shows. Field names match the ads config JSON; defaults match the native
    /// library's.
    /// </summary>
    [Serializable]
    internal sealed class HDCFullscreenOptions
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
}
