namespace HDC.Ads.Diagnostics
{
    /// <summary>One Remote Config key the ads read: the value each source had, and the one used.</summary>
    internal sealed class HDCConfigEntry
    {
        internal HDCConfigEntry(string key) => Key = key;

        internal string Key { get; }

        /// <summary>Remote Config's active value; empty when it has none.</summary>
        internal string Remote { get; set; } = "";

        /// <summary>Where Remote Config's value came from, as Firebase reports it, or why there is none.</summary>
        internal string RemoteOrigin { get; set; } = "";

        /// <summary>The value the last run saved on the device, read before this run saved its own.</summary>
        internal string Saved { get; set; } = "";

        /// <summary>The project's default, from HDC > Edit configs.</summary>
        internal string Default { get; set; } = "";

        internal string Used { get; set; } = "";
        internal HDCConfigSource Source { get; set; }
    }
}
