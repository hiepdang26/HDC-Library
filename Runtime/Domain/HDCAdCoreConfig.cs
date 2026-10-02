using System;
using UnityEngine;

namespace HDC.Ads.Domain
{
    /// <summary>
    /// Which ad units serve each channel: the Remote Config value named by
    /// <see cref="HDCAdsConfig.selectedAdCoreName"/>. Field names are the JSON keys. "admobUnit" units are
    /// served by the Google Mobile Ads plugin, "androidUnit" units by the native library on both platforms.
    /// </summary>
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

        /// <summary>Which ad the launch and resume comeback slots use; ad types are 0 = force ad group, 1 = app open.</summary>
        [Serializable]
        public sealed class Comeback
        {
            public int launchAdType;
            public string launchForceAdGroupName = "";
            public int resumeAdType;
            public string resumeForceAdGroupName = "";
        }

        /// <summary>Which native ad assets show.</summary>
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

        /// <summary>Layouts a native full-screen ad shows with, one picked at random per show.</summary>
        [Serializable]
        public sealed class LayoutGroup
        {
            public string groupName = "";
            public Layout[] layouts = new Layout[0];
        }

        [Serializable]
        public sealed class Layout
        {
            public string layout = "";
            public string assetConfigName = "";

            /// <summary>Countdown seconds before the ad can be closed.</summary>
            public float layoutTime = 5;

            /// <summary>Extra seconds after the countdown before the ad can be closed.</summary>
            public float delay;

            public int timeUpC;
            public bool pauseGameplay;
            public bool showTCD = true;
            public bool disableAdComeback;
        }

        /// <summary>A force ad group serving positions, with its ad units in priority order.</summary>
        [Serializable]
        public sealed class ForceAdGroup
        {
            /// <summary>0 = Google Mobile Ads unit first, 1 = native unit first.</summary>
            public int mediationPriority;

            /// <summary>Falls back to the other unit when the first one fails.</summary>
            public bool useBackup;

            public string groupName = "";
            public string[] positionNames = new string[0];

            /// <summary>Shows per session before the group stops; 0 is unlimited.</summary>
            public int maxShowCount;

            /// <summary>Loads once: no reload after a show or a failed load.</summary>
            public bool disablePostInitReload;

            public AdmobUnit admobUnit = new AdmobUnit();
            public NativeUnit androidUnit = new NativeUnit();
        }

        /// <summary>Rewarded, app open or MREC ad units in priority order, as in <see cref="ForceAdGroup"/>.</summary>
        [Serializable]
        public sealed class FullscreenUnit
        {
            public int mediationPriority;
            public bool useBackup;
            public AdmobUnit admobUnit = new AdmobUnit();
            public NativeUnit androidUnit = new NativeUnit();
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
        }

        /// <summary>A Google Mobile Ads plugin unit. Preloading applies to full-screen formats.</summary>
        [Serializable]
        public sealed class AdmobUnit
        {
            public string id = "";
            public bool preloadAd;
            public int adBufferSize;
        }

        /// <summary>
        /// A native library unit. Full-screen units use <see cref="layoutGroupName"/>; the banner uses
        /// <see cref="ids"/>, <see cref="layouts"/> and <see cref="reloadTime"/>; popups use
        /// <see cref="layout"/>, <see cref="timeShow"/> and <see cref="reloadTime"/>.
        /// </summary>
        [Serializable]
        public sealed class NativeUnit
        {
            public string id = "";
            public string layoutGroupName = "";
            public Interstitials androidInterstitials = new Interstitials();
            public string[] ids = new string[0];
            public string layout = "";
            public string[] layouts = new string[0];
            public int reloadTime;
            public int timeShow;
        }

        /// <summary>Serves a force ad group's native unit as an interstitial instead of a native ad.</summary>
        [Serializable]
        public sealed class Interstitials
        {
            public bool switchToInterstitialAndroid;
            public bool isPreloadAd;
            public int bufferSize;
        }
    }
}
