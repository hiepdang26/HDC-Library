using System;

namespace HDC.Ads.Domain
{
    [Serializable]
    internal sealed class HDCPopupOptions
    {
        public string layout = "popup_single_manual_01";

        public int timeShow = 5;

        public int timeReload;

        public float x = 16;
        public float y = 96;
        public float width = 320;
        public float height = 280;

        public bool autoClose;

        public bool enableCtrOverlay;

        public SourceLayout[] adSourceLayouts = new SourceLayout[0];

        [Serializable]
        internal sealed class SourceLayout
        {
            public string[] adSources = new string[0];
            public string layout = "";
        }
    }
}
