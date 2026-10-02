using System;

namespace HDC.Ads.Domain
{
    /// <summary>A switch per native ad asset. Price, store and star rating only apply to visibility.</summary>
    [Serializable]
    internal sealed class HDCNativeAssets
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
}
