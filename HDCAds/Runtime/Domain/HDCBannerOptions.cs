using System;

namespace HDC.Ads.Domain
{
    [Serializable]
    internal sealed class HDCBannerOptions
    {
        public string[] layoutNames = new string[0];

        public int timeReload;

        public int timeCountdown = 5;

        public int timeCollapse = 5;
    }
}
