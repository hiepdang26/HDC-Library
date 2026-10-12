using System;
using HDC.Ads.Domain;

namespace HDC.Onboarding
{
    [Serializable]
    public sealed class HDCSplashLaunchChannel
    {
        public int launchAdType;
        public ForceAdUnit forceAdUnit = new ForceAdUnit();
        public AppOpenUnit appOpenUnit = new AppOpenUnit();

        internal HDCFullscreenAdType AdType => (HDCFullscreenAdType)launchAdType;

        internal bool IsKnownType => Enum.IsDefined(typeof(HDCFullscreenAdType), launchAdType);

        internal string AdUnitId
        {
            get
            {
                switch (AdType)
                {
                    case HDCFullscreenAdType.Interstitial:
                        return (forceAdUnit?.admobUnit?.id ?? "").Trim();
                    case HDCFullscreenAdType.Native:
                        return (forceAdUnit?.nativeAdUnit?.id ?? "").Trim();
                    case HDCFullscreenAdType.AppOpen:
                        return (appOpenUnit?.admobUnit?.id ?? "").Trim();
                    default:
                        return "";
                }
            }
        }

        internal string LayoutGroup => forceAdUnit?.nativeAdUnit?.layoutGroupName ?? "";

        [Serializable]
        public sealed class ForceAdUnit
        {
            public AdmobUnit admobUnit = new AdmobUnit();
            public NativeAdUnit nativeAdUnit = new NativeAdUnit();
        }

        [Serializable]
        public sealed class AppOpenUnit
        {
            public AdmobUnit admobUnit = new AdmobUnit();
        }

        [Serializable]
        public sealed class AdmobUnit
        {
            public string id = "";
        }

        [Serializable]
        public sealed class NativeAdUnit
        {
            public string id = "";
            public string layoutGroupName = "";
        }
    }
}
