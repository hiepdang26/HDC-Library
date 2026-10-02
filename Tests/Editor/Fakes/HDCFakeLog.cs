using System.Collections.Generic;
using HDC.Ads.Ports;

namespace HDC.Ads.Tests
{
    /// <summary>Keeps the debug log's lines for the test to read.</summary>
    internal sealed class HDCFakeLog : IAdsLog
    {
        internal List<string> Lines { get; } = new List<string>();

        public void Info(string message) => Lines.Add(message);
    }
}
