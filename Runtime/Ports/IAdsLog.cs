using System;

namespace HDC.Ads.Ports
{
    internal interface IAdsLog
    {
        void Info(string message);

        void Warning(string message);

        void Exception(Exception exception);
    }
}
