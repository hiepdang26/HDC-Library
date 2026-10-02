namespace HDC.Ads.Diagnostics
{
    /// <summary>How one mediation adapter started, as Google Mobile Ads reported it.</summary>
    internal sealed class HDCAdapterStatus
    {
        internal HDCAdapterStatus(string adapterClass, bool ready, string description, int latencyMillis)
        {
            AdapterClass = adapterClass ?? "";
            Ready = ready;
            Description = description ?? "";
            LatencyMillis = latencyMillis;
        }

        internal string AdapterClass { get; }
        internal bool Ready { get; }
        internal string Description { get; }
        internal int LatencyMillis { get; }
    }
}
