using HDC.Ads.Domain;

namespace HDC.Ads.Ports
{
    /// <summary>
    /// An ad a network would make for a slot: its instance id, format and ad unit, known before the ad exists so
    /// the debug panel can show what a slot will load.
    /// </summary>
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

        /// <summary>The id the ad loads with, which its events carry.</summary>
        internal string InstanceId { get; }

        /// <summary>One of the <see cref="HDCAdFormat"/> values.</summary>
        internal string Format { get; }

        /// <summary>The ad unit, or several joined by commas where the ad loads from a list.</summary>
        internal string AdUnitId { get; }

        internal HDCAdUnitSpec Spec { get; }
    }
}
