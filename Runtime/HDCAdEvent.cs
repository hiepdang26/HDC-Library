using System;

namespace HDC.Ads
{
    /// <summary>
    /// One event from the native ads SDK. Field names match the JSON the native side sends; fields an
    /// event does not use keep their defaults.
    /// </summary>
    [Serializable]
    public sealed class HDCAdEvent
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

        /// <summary>Error code of a failed load or show.</summary>
        public int code;

        /// <summary>Error message of a failed load or show.</summary>
        public string message;

        /// <summary>Revenue of a <see cref="HDCAdEventType.Paid"/> event, in micros of <see cref="currency"/>.</summary>
        public long valueMicros;

        public string currency;

        /// <summary>Precision type of the paid value, as Google Mobile Ads reports it.</summary>
        public int precision;

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
            if (!string.IsNullOrEmpty(message))
                text += $" error={code} {message}";
            return text;
        }
    }

    /// <summary>Values of <see cref="HDCAdEvent.type"/>.</summary>
    public static class HDCAdEventType
    {
        public const string Initialized = "Initialized";
        public const string Loaded = "Loaded";
        public const string LoadFailed = "LoadFailed";
        public const string Shown = "Shown";
        public const string ShowFailed = "ShowFailed";
        public const string Impression = "Impression";
        public const string Clicked = "Clicked";
        public const string Paid = "Paid";
        public const string Closed = "Closed";
    }

    /// <summary>Values of <see cref="HDCAdEvent.format"/>.</summary>
    public static class HDCAdFormat
    {
        public const string Sdk = "sdk";
        public const string Interstitial = "interstitial";
    }
}
