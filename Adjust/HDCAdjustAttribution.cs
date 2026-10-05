using System;

namespace HDC.Ads
{
    public sealed class HDCAdjustAttribution
    {
        internal HDCAdjustAttribution(string trackerToken, string trackerName, string network, string campaign, string adgroup,
            string creative, string clickLabel, string costType, double? costAmount, string costCurrency)
        {
            TrackerToken = trackerToken ?? "";
            TrackerName = trackerName ?? "";
            Network = network ?? "";
            Campaign = campaign ?? "";
            Adgroup = adgroup ?? "";
            Creative = creative ?? "";
            ClickLabel = clickLabel ?? "";
            CostType = costType ?? "";
            CostAmount = costAmount;
            CostCurrency = costCurrency ?? "";
        }

        public string TrackerToken { get; }

        public string TrackerName { get; }

        public string Network { get; }

        public string Campaign { get; }

        public string Adgroup { get; }

        public string Creative { get; }

        public string ClickLabel { get; }

        public string CostType { get; }

        public double? CostAmount { get; }

        public string CostCurrency { get; }

        public override string ToString() =>
            Network.Length == 0 ? "(no network)" : Campaign.Length == 0 ? Network : Network + " / " + Campaign;

        internal bool IsEmpty => TrackerToken.Length == 0 && Network.Length == 0;

        internal bool SameAs(HDCAdjustAttribution other) =>
            other != null && TrackerToken == other.TrackerToken && TrackerName == other.TrackerName && Network == other.Network
            && Campaign == other.Campaign && Adgroup == other.Adgroup && Creative == other.Creative && ClickLabel == other.ClickLabel
            && CostType == other.CostType && Nullable.Equals(CostAmount, other.CostAmount) && CostCurrency == other.CostCurrency;
    }
}
