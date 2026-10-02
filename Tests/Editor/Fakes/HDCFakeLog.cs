using System.Collections.Generic;
using HDC.Ads.Ports;

namespace HDC.Ads.Tests
{
    internal sealed class HDCFakeLog : IAdsLog
    {
        internal List<string> Lines { get; } = new List<string>();

        public void Info(string message) => Lines.Add(message);
    }
}
