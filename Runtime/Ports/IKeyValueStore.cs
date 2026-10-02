namespace HDC.Ads.Ports
{
    /// <summary>Numbers kept across sessions: impression counts and the ad removal flag.</summary>
    internal interface IKeyValueStore
    {
        /// <summary>False when the store cannot be read now; the value is then 0.</summary>
        bool TryGetInt(string key, out int value);

        void SetInt(string key, int value);

        /// <summary>Writes the values to disk now rather than when the app quits.</summary>
        void Save();
    }
}
