using HDC.Ads.Domain;

namespace HDC.Ads.Ports
{
    internal sealed class HDCAdPlan
    {
        internal HDCAdPlan(IAdNetwork network, HDCAdUse use, string instanceId, string format, string adUnitId, HDCAdUnitSpec spec)
        {
            Network = network;
            Use = use;
            InstanceId = instanceId;
            Format = format;
            AdUnitId = adUnitId;
            Spec = spec;
        }

        internal IAdNetwork Network { get; }
        internal HDCAdUse Use { get; }

        internal string InstanceId { get; }

        internal string Format { get; }

        internal string AdUnitId { get; }

        internal HDCAdUnitSpec Spec { get; }
    }
}
