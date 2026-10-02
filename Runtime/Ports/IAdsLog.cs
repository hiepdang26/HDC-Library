namespace HDC.Ads.Ports
{
    /// <summary>The library's debug log, which only writes while debug logging is on.</summary>
    internal interface IAdsLog
    {
        void Info(string message);
    }
}
