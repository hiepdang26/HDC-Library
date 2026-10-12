namespace HDC.Ads.Diagnostics
{
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
