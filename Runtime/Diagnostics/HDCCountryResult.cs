using System.Collections.Generic;

namespace HDC.Ads.Diagnostics
{
    internal sealed class HDCCountryResult
    {
        internal HDCCountryResult(bool on, string reason, string deviceId, IReadOnlyList<string> signals)
        {
            IsOn = on;
            Reason = reason ?? "";
            DeviceId = deviceId ?? "";
            Signals = signals ?? new string[0];
        }

        internal bool IsOn { get; }

        internal string Reason { get; }

        internal string DeviceId { get; }

        internal IReadOnlyList<string> Signals { get; }

        internal static HDCCountryResult Off(string reason, string deviceId = "") => new HDCCountryResult(false, reason, deviceId, null);
    }
}
