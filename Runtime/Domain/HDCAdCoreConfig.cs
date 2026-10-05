using System;
using UnityEngine;

namespace HDC.Ads.Domain
{
    [Serializable]
    internal sealed class HDCAdCoreConfig
    {
        public Comeback comebackChannel = new Comeback();
        public AssetConfig[] assetConfigs = new AssetConfig[0];
        public LayoutConfigs forceAdLayoutConfig = new LayoutConfigs();
        public ForceAdGroup[] forceAdGroups = new ForceAdGroup[0];
        public FullscreenUnit rewardedUnit = new FullscreenUnit();
        public FullscreenUnit appOpenUnit = new FullscreenUnit();
        public BannerUnits bannerUnit = new BannerUnits();
        public FullscreenUnit mrecUnit = new FullscreenUnit();
        public PopupGroup[] popupGroups = new PopupGroup[0];

        public static HDCAdCoreConfig Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new HDCAdCoreConfig();
            try
            {
                return JsonUtility.FromJson<HDCAdCoreConfig>(json) ?? new HDCAdCoreConfig();
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[HDCAds] invalid ad core config: " + exception.Message);
                return new HDCAdCoreConfig();
            }
        }

        internal ForceAdGroup ForceAdGroupNamed(string groupName) =>
            Array.Find(forceAdGroups ?? new ForceAdGroup[0], g => g != null && g.groupName == groupName);

        internal string ForceAdGroupAt(string position) =>
            Array.Find(forceAdGroups ?? new ForceAdGroup[0], g => g != null && Contains(g.positionNames, position))?.groupName ?? "";

        internal PopupGroup PopupGroupNamed(string groupName) =>
            Array.Find(popupGroups ?? new PopupGroup[0], g => g != null && g.groupName == groupName);

        internal string PopupGroupAt(string position) =>
            Array.Find(popupGroups ?? new PopupGroup[0], g => g != null && Contains(g.positionNames, position))?.groupName ?? "";

        internal LayoutGroup LayoutGroupNamed(string groupName) =>
            string.IsNullOrEmpty(groupName)
                ? null
                : Array.Find(forceAdLayoutConfig?.layoutGroups ?? new LayoutGroup[0], g => g != null && g.groupName == groupName);

        internal AssetConfig AssetConfigNamed(string name) =>
            string.IsNullOrEmpty(name) ? null : Array.Find(assetConfigs ?? new AssetConfig[0], a => a != null && a.assetConfigName == name);

        private static bool Contains(string[] values, string value) =>
            !string.IsNullOrEmpty(value) && values != null && Array.IndexOf(values, value) >= 0;

        [Serializable]
        public sealed class Comeback
        {
            public int launchAdType;
            public string launchForceAdGroupName = "";
            public int resumeAdType;
            public string resumeForceAdGroupName = "";
        }

        [Serializable]
        public sealed class AssetConfig
        {
            public string assetConfigName = "";
            public bool show_cta = true;
            public bool show_headline = true;
            public bool show_body = true;
            public bool show_description = true;
            public bool show_icon = true;
            public bool show_advertiser = true;
            public bool show_media = true;
            public bool show_media_image = true;
            public bool show_media_video = true;
            public bool show_star_rating = true;
            public bool show_store = true;
            public bool show_price = true;
        }

        [Serializable]
        public sealed class LayoutConfigs
        {
            public LayoutGroup[] layoutGroups = new LayoutGroup[0];
        }

        [Serializable]
        public sealed class LayoutGroup
        {
            public string groupName = "";
            public Layout[] layouts = new Layout[0];
            public AdSourceGroup[] adSourceGroups = new AdSourceGroup[0];
        }

        [Serializable]
        public sealed class AdSourceGroup
        {
            public string[] adSourceIds = new string[0];
            public Layout[] layouts = new Layout[0];
        }

        [Serializable]
        public sealed class Layout
        {
            public string layout = "";
            public string assetConfigName = "";

            public float layoutTime = 5;

            public float delay;

            public int timeUpC;
            public bool pauseGameplay;
            public bool showTCD = true;
            public bool disableAdComeback;
        }

        [Serializable]
        public sealed class ForceAdGroup
        {
            public int mediationPriority;

            public bool useBackup;

            public string groupName = "";
            public string[] positionNames = new string[0];

            public int maxShowCount;

            public bool disablePostInitReload;

            public AdmobUnit admobUnit = new AdmobUnit();
            public NativeUnit androidUnit = new NativeUnit();

            internal object UnitFor(string networkKey) =>
                networkKey == HDCAdUnitKeys.AdMob ? admobUnit : networkKey == HDCAdUnitKeys.Native ? (object)androidUnit : null;
        }

        [Serializable]
        public sealed class FullscreenUnit
        {
            public int mediationPriority;
            public bool useBackup;
            public AdmobUnit admobUnit = new AdmobUnit();
            public NativeUnit androidUnit = new NativeUnit();

            internal object UnitFor(string networkKey) =>
                networkKey == HDCAdUnitKeys.AdMob ? admobUnit : networkKey == HDCAdUnitKeys.Native ? (object)androidUnit : null;
        }

        [Serializable]
        public sealed class BannerUnits
        {
            public FullscreenUnit fullBottom = new FullscreenUnit();
            public FullscreenUnit fullTop = new FullscreenUnit();
            public FullscreenUnit topLeft = new FullscreenUnit();
            public FullscreenUnit topRight = new FullscreenUnit();
            public FullscreenUnit bottomLeft = new FullscreenUnit();
            public FullscreenUnit bottomRight = new FullscreenUnit();

            internal FullscreenUnit Slot(HDCBannerSlot slot)
            {
                switch (slot)
                {
                    case HDCBannerSlot.FullTop: return fullTop;
                    case HDCBannerSlot.TopLeft: return topLeft;
                    case HDCBannerSlot.TopRight: return topRight;
                    case HDCBannerSlot.BottomLeft: return bottomLeft;
                    case HDCBannerSlot.BottomRight: return bottomRight;
                    default: return fullBottom;
                }
            }
        }

        [Serializable]
        public sealed class PopupGroup
        {
            public string groupName = "";
            public string[] positionNames = new string[0];
            public bool disablePostInitReload;
            public NativeUnit androidUnit = new NativeUnit();

            internal object UnitFor(string networkKey) => networkKey == HDCAdUnitKeys.Native ? androidUnit : null;
        }

        [Serializable]
        public sealed class AdmobUnit
        {
            public string id = "";
            public bool preloadAd;
            public int adBufferSize;
        }

        [Serializable]
        public sealed class NativeUnit
        {
            public string id = "";
            public string layoutGroupName = "";
            public Interstitials androidInterstitials = new Interstitials();
            public string[] ids = new string[0];
            public string layout = "";
            public string[] layouts = new string[0];
            public AdSourceLayout[] adSourceLayouts = new AdSourceLayout[0];
            public int reloadTime;
            public int timeShow;
        }

        [Serializable]
        public sealed class AdSourceLayout
        {
            public string[] adSources = new string[0];
            public string layout = "";
        }

        [Serializable]
        public sealed class Interstitials
        {
            public bool switchToInterstitialAndroid;
            public bool isPreloadAd;
            public int bufferSize;
            public bool useNativeAfterInterstitial;
            public string nativeAfterInterstitialId = "";
            public string nativeAfterInterstitialLayout = "";

            internal bool HasNativeAfterInterstitial =>
                switchToInterstitialAndroid && useNativeAfterInterstitial && !string.IsNullOrWhiteSpace(nativeAfterInterstitialId);
        }
    }
}
