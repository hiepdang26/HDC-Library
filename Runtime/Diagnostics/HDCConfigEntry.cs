namespace HDC.Ads.Diagnostics
{
    internal sealed class HDCConfigEntry
    {
        internal HDCConfigEntry(string key) => Key = key;

        internal string Key { get; }

        internal string Remote { get; set; } = "";

        internal string RemoteOrigin { get; set; } = "";

        internal string Saved { get; set; } = "";

        internal string Default { get; set; } = "";

        internal string Used { get; set; } = "";
        internal HDCConfigSource Source { get; set; }
    }
}
