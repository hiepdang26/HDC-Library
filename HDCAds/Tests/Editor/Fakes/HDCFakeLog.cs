using System;
using System.Collections.Generic;
using HDC.Ads.Ports;

namespace HDC.Ads.Tests
{
    internal sealed class HDCFakeLog : IAdsLog
    {
        internal List<string> Lines { get; } = new List<string>();

        internal List<string> Warnings { get; } = new List<string>();

        internal List<Exception> Exceptions { get; } = new List<Exception>();

        public void Info(string message) => Lines.Add(message);

        public void Warning(string message) => Warnings.Add(message);

        public void Exception(Exception exception) => Exceptions.Add(exception);
    }
}
