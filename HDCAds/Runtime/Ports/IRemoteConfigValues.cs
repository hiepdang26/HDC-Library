namespace HDC.Ads.Ports
{
    internal interface IRemoteConfigValues
    {
        string Read(string key, out string origin);
    }
}
