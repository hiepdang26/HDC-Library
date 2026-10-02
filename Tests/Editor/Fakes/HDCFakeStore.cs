using System.Collections.Generic;
using HDC.Ads.Ports;

namespace HDC.Ads.Tests
{
    /// <summary>A store in memory, which a test can hand to the next session.</summary>
    internal sealed class HDCFakeStore : IKeyValueStore
    {
        internal Dictionary<string, int> Values { get; } = new Dictionary<string, int>();

        internal int Saves { get; private set; }

        public bool TryGetInt(string key, out int value)
        {
            Values.TryGetValue(key, out value);
            return true;
        }

        public void SetInt(string key, int value) => Values[key] = value;

        public void Save() => Saves++;
    }
}
