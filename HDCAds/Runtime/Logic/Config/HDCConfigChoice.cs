using System;
using System.Collections.Generic;
using System.Linq;
using HDC.Ads.Diagnostics;

namespace HDC.Ads.Logic
{
    internal sealed class HDCConfigChoice
    {
        internal string Ads { get; set; } = "";

        internal string Core { get; set; } = "";

        internal string CoreKey { get; set; } = "";

        internal HDCCountryResult Country { get; set; }

        internal List<KeyValuePair<string, string>> Saves { get; } = new List<KeyValuePair<string, string>>();

        internal Dictionary<string, (string Value, HDCConfigSource Source)> Custom { get; } =
            new Dictionary<string, (string Value, HDCConfigSource Source)>(StringComparer.Ordinal);

        internal bool SaveCustom { get; set; }

        internal Dictionary<string, (string Value, string Source)> CustomValues() =>
            Custom.ToDictionary(pair => pair.Key, pair => (Value: pair.Value.Value, Source: pair.Value.Source.ToString()), StringComparer.Ordinal);
    }
}
