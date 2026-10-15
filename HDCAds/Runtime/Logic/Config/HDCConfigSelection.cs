using System.Collections.Generic;
using HDC.Ads.Diagnostics;
using HDC.Ads.Domain;
using HDC.Ads.Ports;

namespace HDC.Ads.Logic
{
    internal sealed class HDCConfigSelection
    {
        internal const string AdsConfigKey = "ads_config";

        private readonly HDCCountryMode country;
        private readonly IAdsLog log;

        internal HDCConfigSelection(HDCCountryMode country, IAdsLog log)
        {
            this.country = country;
            this.log = log;
        }

        internal HDCConfigChoice Select(HDCConfigInputs inputs)
        {
            var choice = new HDCConfigChoice();
            bool keyed = inputs.Mode != HDCConfigMode.NoRemoteConfig;
            if (keyed)
            {
                choice.Ads = Choose(AdsConfigKey, inputs.DefaultAds, inputs);
                choice.CoreKey = SelectedCore(choice.Ads) ?? "";
                choice.Core = choice.CoreKey.Length == 0 ? "" : Choose(choice.CoreKey, DefaultCore(inputs, choice.CoreKey), inputs);
            }
            else
            {
                choice.Ads = Record(AdsConfigKey, inputs.DefaultAds);
                choice.CoreKey = SelectedCore(choice.Ads) ?? "";
                choice.Core = choice.CoreKey.Length == 0 ? inputs.DefaultCore ?? "" : Record(choice.CoreKey, inputs.DefaultCore);
            }

            choice.Country = country.Check(inputs.CountryRules, RemoteDebugDevices(inputs));
            HDCConfigReport.CountryChecked(choice.Country);
            if (choice.Country.IsOn)
            {
                choice.Ads = UseCountry(AdsConfigKey, inputs.CountryAds, choice.Ads, true);
                string countryCoreKey = SelectedCore(choice.Ads);
                if (!string.IsNullOrEmpty(countryCoreKey))
                    choice.CoreKey = countryCoreKey;
                if (!keyed || choice.CoreKey.Length > 0)
                    choice.Core = UseCountry(choice.CoreKey, inputs.CountryCore, choice.Core, choice.CoreKey.Length > 0);
                foreach (KeyValuePair<string, string> pair in inputs.CustomCountry)
                {
                    if (inputs.CustomDefaults.ContainsKey(pair.Key))
                        choice.Custom[pair.Key] = (pair.Value, HDCConfigSource.Country);
                }
            }
            else
            {
                foreach (KeyValuePair<string, string> custom in inputs.CustomDefaults)
                    choice.Custom[custom.Key] = ChooseCustom(custom.Key, custom.Value, inputs);
            }

            return choice;
        }

        private string SelectedCore(string ads)
        {
            HDCAdsConfig config = HDCAdsConfig.Parse(ads, out string error);
            if (error != null)
                log.Warning("invalid ads config: " + error);
            return config.selectedAdCoreName;
        }

        private static string Choose(string key, string fallback, HDCConfigInputs inputs)
        {
            HDCConfigEntry entry = HDCConfigReport.Entry(key);
            entry.Default = fallback ?? "";
            if (UsesRemote(inputs))
            {
                entry.Remote = inputs.Remote.Read(key, out string origin) ?? "";
                entry.RemoteOrigin = origin ?? "";
            }
            else
            {
                entry.RemoteOrigin = NotFetched(inputs);
            }

            if (entry.Remote.Length > 0)
                Use(entry, entry.Remote, HDCConfigSource.Remote);
            else
                Use(entry, entry.Default, HDCConfigSource.Default);
            return entry.Used;
        }

        private static string Record(string key, string value)
        {
            HDCConfigEntry entry = HDCConfigReport.Entry(key);
            entry.Default = value ?? "";
            entry.RemoteOrigin = "Not in this build: no Firebase";
            Use(entry, entry.Default, HDCConfigSource.Default);
            return entry.Used;
        }

        private static (string Value, HDCConfigSource Source) ChooseCustom(string key, string fallback, HDCConfigInputs inputs)
        {
            if (UsesRemote(inputs))
            {
                string remote = inputs.Remote.Read(key, out _);
                if (!string.IsNullOrEmpty(remote))
                    return (remote, HDCConfigSource.Remote);
            }

            return (fallback ?? "", HDCConfigSource.Default);
        }

        private static bool UsesRemote(HDCConfigInputs inputs) => inputs.Mode == HDCConfigMode.RemoteConfig && inputs.Remote != null;

        private static string NotFetched(HDCConfigInputs inputs)
        {
            switch (inputs.Mode)
            {
                case HDCConfigMode.EditorDefaults:
                    return "Not fetched in the Editor";
                case HDCConfigMode.Defaults:
                    return "Not fetched: HDC Ads Settings uses Default";
                default:
                    return "Not used: " + (string.IsNullOrEmpty(inputs.RemoteProblem) ? "Firebase not ready" : inputs.RemoteProblem);
            }
        }

        private static string UseCountry(string key, string countryValue, string current, bool record)
        {
            if (string.IsNullOrWhiteSpace(countryValue))
                return current;
            if (record)
                Use(HDCConfigReport.Entry(key), countryValue, HDCConfigSource.Country);
            return countryValue;
        }

        private static IEnumerable<string> RemoteDebugDevices(HDCConfigInputs inputs) =>
            UsesRemote(inputs)
                ? HDCDebugDevices.Parse(inputs.Remote.Read(HDCCountryMode.DevicesKey, out _))
                : new List<string>();

        private static string DefaultCore(HDCConfigInputs inputs, string key) =>
            inputs.DefaultCores != null && inputs.DefaultCores.TryGetValue(key, out string fallback) ? fallback : "";

        private static void Use(HDCConfigEntry entry, string value, HDCConfigSource source)
        {
            entry.Used = value;
            entry.Source = source;
        }
    }
}
