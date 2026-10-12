using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HDC.Ads.Diagnostics;
using HDC.Ads.Domain;
using HDC.Ads.Ports;

namespace HDC.Ads.Logic
{
    internal sealed class HDCCountryMode
    {
        internal const string DevicesKey = "devices";

        private readonly IDeviceRegionSource device;
        private readonly IAdsLog log;

        internal HDCCountryMode(IDeviceRegionSource device, IAdsLog log)
        {
            this.device = device;
            this.log = log;
        }

        internal HDCCountryResult Check(string rulesJson, IEnumerable<string> remoteDebugDevices = null)
        {
            HDCCountryRules rules = HDCCountryRules.Parse(rulesJson);
            if (!rules.enabled)
                return HDCCountryResult.Off("Country check is off");
            HDCDeviceRegion region = device.IsEditor && !rules.simulateInEditor ? new HDCDeviceRegion() : device.Read();
            HDCCountryResult result = Decide(rules, device.DeviceId, remoteDebugDevices, region, device.IsEditor);
            log.Info($"country mode {(result.IsOn ? "on" : "off")}: {result.Reason} (device {result.DeviceId})");
            return result;
        }

        internal static HDCCountryResult Decide(HDCCountryRules rules, string device, IEnumerable<string> remoteDebugDevices,
            HDCDeviceRegion region, bool editor)
        {
            if (rules == null || !rules.enabled)
                return HDCCountryResult.Off("Country check is off", device);
            string target = (rules.targetCountry ?? "").Trim().ToLowerInvariant();
            if (target.Length == 0)
                return HDCCountryResult.Off("No target country", device);
            if (editor && !rules.simulateInEditor)
                return HDCCountryResult.Off("The Editor never gets country mode", device);
            if (!string.IsNullOrEmpty(device) && (rules.debugDevices ?? new string[0]).Concat(remoteDebugDevices ?? new string[0])
                    .Any(listed => string.Equals(listed?.Trim(), device, StringComparison.OrdinalIgnoreCase)))
                return HDCCountryResult.Off("Debug device", device);
            if (editor)
                return new HDCCountryResult(true, "Simulated in the Editor", device, new[] { "Editor: simulateInEditor" });

            List<string> signals = Signals(rules, target, region ?? new HDCDeviceRegion());
            return signals.Count > 0
                ? new HDCCountryResult(true, signals[0], device, signals)
                : new HDCCountryResult(false, "No signal of " + target.ToUpperInvariant(), device, signals);
        }

        private static List<string> Signals(HDCCountryRules rules, string target, HDCDeviceRegion region)
        {
            var signals = new List<string>();
            if (Contains(rules.systemLanguages, region.SystemLanguage))
                signals.Add("System language " + region.SystemLanguage);
            string language = (region.LanguageCodes ?? new string[0]).FirstOrDefault(code => Contains(rules.languageCodes, code));
            if (language != null)
                signals.Add("Locale language " + language);
            if (rules.matchRegion && (region.Regions ?? new string[0]).Any(code => string.Equals(code, target, StringComparison.OrdinalIgnoreCase)))
                signals.Add("Locale region " + target.ToUpperInvariant());
            string zone = (rules.timezoneNames ?? new string[0]).FirstOrDefault(name =>
                !string.IsNullOrWhiteSpace(name) && (region.TimezoneName ?? "").IndexOf(name.Trim(), StringComparison.OrdinalIgnoreCase) >= 0);
            if (zone != null)
                signals.Add("Time zone " + region.TimezoneName);
            if ((rules.utcOffsets ?? new float[0]).Any(offset => Math.Abs(offset - region.UtcOffsetHours) < 0.01))
                signals.Add("UTC offset " + region.UtcOffsetHours.ToString("+0.##;-0.##", CultureInfo.InvariantCulture));
            if (rules.matchSimCountry && string.Equals(region.SimCountry, target, StringComparison.OrdinalIgnoreCase))
                signals.Add("SIM country " + target.ToUpperInvariant());
            if (rules.matchNetworkCountry && string.Equals(region.NetworkCountry, target, StringComparison.OrdinalIgnoreCase))
                signals.Add("Network country " + target.ToUpperInvariant());
            return signals;
        }

        private static bool Contains(string[] values, string value) =>
            !string.IsNullOrEmpty(value) && (values ?? new string[0]).Any(listed => string.Equals(listed?.Trim(), value, StringComparison.OrdinalIgnoreCase));
    }
}
