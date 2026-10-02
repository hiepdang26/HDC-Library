using System;
using UnityEngine;

namespace HDC.Ads.Domain
{
    [Serializable]
    internal sealed class HDCAdsConfig
    {
        public string selectedAdCoreName = "";

        public AppLaunchChannel appLaunchChannel = new AppLaunchChannel();
        public AppResumeChannel appResumeChannel = new AppResumeChannel();
        public ForceAdChannel forceAdChannel = new ForceAdChannel();
        public RewardedChannel rewardedChannel = new RewardedChannel();
        public BannerChannel bannerChannel = new BannerChannel();
        public MrecChannel mrecChannel = new MrecChannel();
        public PopupChannel popupChannel = new PopupChannel();

        public static HDCAdsConfig Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new HDCAdsConfig();
            try
            {
                return JsonUtility.FromJson<HDCAdsConfig>(json) ?? new HDCAdsConfig();
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[HDCAds] invalid ads config: " + exception.Message);
                return new HDCAdsConfig();
            }
        }

        [Serializable]
        public sealed class AppLaunchChannel
        {
            public bool isEnabled;
            public bool autoInit = true;

            public int minWaitSeconds;

            public int timeoutSeconds;
        }

        [Serializable]
        public sealed class AppResumeChannel
        {
            public bool isEnabled;
            public bool autoInit = true;
            public string adUnitId = "";

            public string layoutGroup = "";

            internal object UnitFor(string networkKey) =>
                networkKey == HDCAdUnitKeys.Native ? new HDCAdCoreConfig.NativeUnit { id = adUnitId, layoutGroupName = layoutGroup } : null;
        }

        [Serializable]
        public sealed class ForceAdChannel
        {
            public bool isEnabled;

            public float launchCappingTime;

            public float minimumCappingTime;

            public float cappingDecreasePerImpression;

            public ForceAdPosition[] positionConfigs = new ForceAdPosition[0];
            public BreakAd breakAdConfig = new BreakAd();
        }

        [Serializable]
        public sealed class ForceAdPosition
        {
            public string positionName = "";
            public bool canShow = true;
            public bool autoInit;

            public float cappingTime = 10;

            public float minimumCappingTime;

            public float cappingDecreasePerImpression;
        }

        [Serializable]
        public sealed class BreakAd
        {
            public bool isEnabled;
            public string positionName = "";

            public int notificationLeadTimeSeconds;
        }

        [Serializable]
        public sealed class RewardedChannel
        {
            public bool isEnabled;
            public bool autoInit = true;
        }

        [Serializable]
        public sealed class BannerChannel
        {
            public bool isEnabled;
            public BannerSlot fullBottom = new BannerSlot();
            public BannerSlot fullTop = new BannerSlot();
            public BannerSlot topLeft = new BannerSlot();
            public BannerSlot topRight = new BannerSlot();
            public BannerSlot bottomLeft = new BannerSlot();
            public BannerSlot bottomRight = new BannerSlot();

            internal BannerSlot Slot(HDCBannerSlot slot)
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
        public sealed class BannerSlot
        {
            public bool isEnabled;
            public bool autoInit = true;

            public bool autoShowOnLoad;
        }

        [Serializable]
        public sealed class MrecChannel
        {
            public bool isEnabled;
            public bool autoInit = true;
        }

        [Serializable]
        public sealed class PopupChannel
        {
            public bool isEnabled;
            public PopupPosition[] positionConfigs = new PopupPosition[0];
        }

        [Serializable]
        public sealed class PopupPosition
        {
            public bool isEnabled;
            public bool autoInit;
            public string positionName = "";
        }
    }
}
