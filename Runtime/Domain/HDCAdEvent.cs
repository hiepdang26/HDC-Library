using System;

namespace HDC.Ads.Domain
{
    [Serializable]
    internal sealed class HDCAdEvent
    {
        public string id;

        public string format;

        public string type;

        public string adUnitId;

        public string adapter;

        public string adSource;

        public string responseId;

        public string layout;

        public int code = -1;

        public string message;

        public long valueMicros;

        public string currency;

        public int precision;

        public string rewardType;

        public double rewardAmount;

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
}
