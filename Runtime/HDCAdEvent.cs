using System;

namespace HDC.Ads
{
    /// <summary>
    /// One event from the native ads SDK. Field names match the JSON the native side sends; fields an
    /// event does not use keep their defaults.
    /// </summary>
    [Serializable]
    internal sealed class HDCAdEvent
    {
        /// <summary>Instance id the ad was loaded with; empty for SDK events.</summary>
        public string id;

        /// <summary>Ad format, see <see cref="HDCAdFormat"/>.</summary>
        public string format;

        /// <summary>What happened, see <see cref="HDCAdEventType"/>.</summary>
        public string type;

        public string adUnitId;

        /// <summary>Mediation adapter class that served the ad.</summary>
        public string adapter;

        /// <summary>Ad source (network) that served the ad.</summary>
        public string adSource;

        public string responseId;

        /// <summary>Layout a fullscreen native ad was shown with.</summary>
        public string layout;

        /// <summary>Error code of a failed load or show: the ad SDK's code, or -1 for an error without one.</summary>
        public int code = -1;

        /// <summary>Error message of a failed load or show.</summary>
        public string message;

        /// <summary>Revenue of a <see cref="HDCAdEventType.Paid"/> event, in micros of <see cref="currency"/>.</summary>
        public long valueMicros;

        public string currency;

        /// <summary>Precision type of the paid value, as Google Mobile Ads reports it.</summary>
        public int precision;

        /// <summary>Reward type of a <see cref="HDCAdEventType.Rewarded"/> event.</summary>
        public string rewardType;

        /// <summary>Reward amount of a <see cref="HDCAdEventType.Rewarded"/> event.</summary>
        public double rewardAmount;

        /// <summary>Revenue of a paid event in <see cref="currency"/> units.</summary>
        public double Revenue => valueMicros / 1000000d;

        public override string ToString()
        {
            string text = $"{format}/{id} {type}";
            if (!string.IsNullOrEmpty(adUnitId))
                text += $" unit={adUnitId}";
            if (!string.IsNullOrEmpty(adSource))
                text += $" source={adSource}";
            if (type == HDCAdEventType.Paid)
                text += $" value={Revenue} {currency}";
            if (type == HDCAdEventType.Rewarded)
                text += $" reward={rewardAmount} {rewardType}";
            if (!string.IsNullOrEmpty(message))
                text += $" error={code} {message}";
            return text;
        }
    }

    /// <summary>Values of <see cref="HDCAdEvent.type"/>.</summary>
    internal static class HDCAdEventType
    {
        public const string Initialized = "Initialized";
        public const string Loaded = "Loaded";
        public const string LoadFailed = "LoadFailed";
        public const string Shown = "Shown";
        public const string ShowFailed = "ShowFailed";
        public const string Impression = "Impression";
        public const string Clicked = "Clicked";
        public const string Paid = "Paid";

        /// <summary>The player earned a rewarded ad's reward.</summary>
        public const string Rewarded = "Rewarded";

        /// <summary>A full-screen ad or popup closed, or a banner was hidden.</summary>
        public const string Closed = "Closed";
    }
}
