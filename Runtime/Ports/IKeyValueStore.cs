namespace HDC.Ads.Ports
{
    internal interface IKeyValueStore
    {
        bool TryGetInt(string key, out int value);

        void SetInt(string key, int value);

        void Save();
    }
}
