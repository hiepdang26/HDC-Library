using System.Collections.Generic;
using HDC.Ads.Ports;

namespace HDC.Ads.Logic
{
    internal sealed class HDCConfigInputs
    {
        internal HDCConfigMode Mode { get; set; }

        internal IRemoteConfigValues Remote { get; set; }

        internal string RemoteProblem { get; set; } = "";

        internal string DefaultAds { get; set; } = "";

        internal IDictionary<string, string> DefaultCores { get; set; } = new Dictionary<string, string>();

        internal string DefaultCore { get; set; } = "";

        internal IReadOnlyDictionary<string, string> CustomDefaults { get; set; } = new Dictionary<string, string>();

        internal string CountryRules { get; set; } = "";

        internal string CountryAds { get; set; } = "";

        internal string CountryCore { get; set; } = "";

        internal IReadOnlyDictionary<string, string> CustomCountry { get; set; } = new Dictionary<string, string>();
    }
}
