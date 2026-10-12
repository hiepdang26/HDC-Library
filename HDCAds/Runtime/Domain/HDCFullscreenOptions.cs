using System;

namespace HDC.Ads.Domain
{
    [Serializable]
    internal sealed class HDCFullscreenOptions
    {
        public string mode = "";

        public string[] layoutNames = new string[0];

        public double duration = 5;

        public double[] durations = new double[0];

        public string orientation = "auto";

        public bool autoClose;
        public bool pauseGameplay = true;

        public bool enableAdComeback = true;

        public bool showTCD = true;

        public double delay;

        public int timeUpC;

        public HDCNativeAssets clickAssets = new HDCNativeAssets();

        public HDCNativeAssets assetVisibility = new HDCNativeAssets();
    }
}
