using System;

namespace HDC.Ads.Domain
{
    /// <summary>
    /// A native popup over the game. Sizes are dp. When <see cref="x"/> and <see cref="y"/> are both in 0..1
    /// they are the popup's center relative to the screen, with y measured from the bottom; otherwise they
    /// are its top-left corner in dp.
    /// </summary>
    [Serializable]
    internal sealed class HDCPopupOptions
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
}
