using System;
using UnityEngine;

namespace HDC.Ads.Domain
{
    /// <summary>
    /// How each ad channel behaves: the "ads_config" Remote Config value. Field names are the JSON keys;
    /// missing keys keep the defaults below.
    /// </summary>
    [Serializable]
    internal sealed class HDCAdsConfig
    {
        /// <summary>Remote Config key that holds the <see cref="HDCAdCoreConfig"/>.</summary>
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

        /// <summary>The ad shown while the app starts: a force ad group or app open, see <see cref="HDCAdCoreConfig.comebackChannel"/>.</summary>
        [Serializable]
        public sealed class AppLaunchChannel
        {
            public bool isEnabled;
            public bool autoInit = true;

            /// <summary>Seconds the launch waits at least before an ad can show; 0 means 5.</summary>
            public int minWaitSeconds;

            /// <summary>Seconds after which the launch goes on without an ad; 0 or less than the minimum wait means minimum + 5.</summary>
            public int timeoutSeconds;
        }

        /// <summary>A native full-screen ad loaded when the app goes to the background and shown on return.</summary>
        [Serializable]
        public sealed class AppResumeChannel
        {
            public bool isEnabled;
            public bool autoInit = true;
            public string adUnitId = "";

            /// <summary>Layout group, from <see cref="HDCAdCoreConfig.forceAdLayoutConfig"/>, the ad shows with.</summary>
            public string layoutGroup = "";
        }

        [Serializable]
        public sealed class ForceAdChannel
        {
            public bool isEnabled;

            /// <summary>Minimum seconds before the first force ad of a session.</summary>
            public float launchCappingTime;

            /// <summary>Lowest capping once impressions have reduced it; positions can override it.</summary>
            public float minimumCappingTime;

            /// <summary>Seconds of capping removed per impression at a position; positions can override it.</summary>
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

            /// <summary>Seconds since the last full-screen ad before one can show here.</summary>
            public float cappingTime = 10;

            /// <summary>Overrides the channel's value when not 0.</summary>
            public float minimumCappingTime;

            /// <summary>Overrides the channel's value when not 0.</summary>
            public float cappingDecreasePerImpression;
        }

        /// <summary>A force ad shown on a timer at one position while the break ad runs.</summary>
        [Serializable]
        public sealed class BreakAd
        {
            public bool isEnabled;
            public string positionName = "";

            /// <summary>Seconds before the ad at which the notice event fires; 0 sends none.</summary>
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

            /// <summary>Shows the banner as soon as its first ad loads.</summary>
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
