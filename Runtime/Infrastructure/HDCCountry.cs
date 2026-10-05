using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HDC.Ads.Diagnostics;
using HDC.Ads.Domain;
using UnityEngine;
#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace HDC.Ads.Infrastructure
{
    internal static class HDCCountry
    {
        internal const string DevicesKey = "devices";

        private static string deviceId;

        internal static HDCCountryResult Check(string rulesJson, IEnumerable<string> remoteDebugDevices = null)
        {
            HDCCountryRules rules = HDCCountryRules.Parse(rulesJson);
            if (!rules.enabled)
                return HDCCountryResult.Off("Country check is off");
            HDCDeviceRegion region = Application.isEditor && !rules.simulateInEditor ? new HDCDeviceRegion() : ReadRegion();
            HDCCountryResult result = Decide(rules, DeviceId(), remoteDebugDevices, region, Application.isEditor);
            if (HDCAdsSdk.DebugLog)
                Debug.Log($"[HDCAds] country mode {(result.IsOn ? "on" : "off")}: {result.Reason} (device {result.DeviceId})");
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

        internal static string DeviceId()
        {
            if (deviceId != null)
                return deviceId;
#if UNITY_ANDROID && !UNITY_EDITOR
            deviceId = AndroidId();
#endif
            if (string.IsNullOrEmpty(deviceId))
                deviceId = SystemInfo.deviceUniqueIdentifier ?? "";
            return deviceId;
        }

        internal static List<string> RemoteDebugDevices(string devicesJson)
        {
            if (string.IsNullOrWhiteSpace(devicesJson))
                return new List<string>();
            try
            {
                return (JsonUtility.FromJson<DevicesValue>(devicesJson)?.debugDevices ?? new string[0])
                    .Where(device => !string.IsNullOrWhiteSpace(device)).Select(device => device.Trim()).ToList();
            }
            catch (ArgumentException)
            {
                return new List<string>();
            }
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

        private static HDCDeviceRegion ReadRegion()
        {
            var languages = new List<string>();
            var regions = new List<string>();
            AddCulture(CultureInfo.CurrentCulture, languages, regions);
            AddCulture(CultureInfo.CurrentUICulture, languages, regions);
            TimeZoneInfo local = TimeZoneInfo.Local;
            var region = new HDCDeviceRegion
            {
                SystemLanguage = Application.systemLanguage.ToString(),
                TimezoneName = local.Id ?? "",
                UtcOffsetHours = local.BaseUtcOffset.TotalHours,
            };
#if UNITY_ANDROID && !UNITY_EDITOR
            ReadAndroid(region, languages, regions);
#elif UNITY_IOS && !UNITY_EDITOR
            ReadIos(region, languages, regions);
#endif
            region.LanguageCodes = languages.Where(code => code.Length > 0).Distinct().ToArray();
            region.Regions = regions.Where(code => code.Length > 0).Distinct().ToArray();
            return region;
        }

        private static void AddCulture(CultureInfo culture, List<string> languages, List<string> regions)
        {
            if (culture == null || string.IsNullOrEmpty(culture.Name))
                return;
            string[] parts = culture.Name.Split('-', '_');
            languages.Add(parts[0].ToLowerInvariant());
            if (parts.Length > 1)
                regions.Add(parts[parts.Length - 1].ToLowerInvariant());
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private static void ReadAndroid(HDCDeviceRegion region, List<string> languages, List<string> regions)
        {
            try
            {
                using (var locale = new AndroidJavaClass("java.util.Locale"))
                using (AndroidJavaObject current = locale.CallStatic<AndroidJavaObject>("getDefault"))
                {
                    languages.Add((current.Call<string>("getLanguage") ?? "").ToLowerInvariant());
                    regions.Add((current.Call<string>("getCountry") ?? "").ToLowerInvariant());
                }

                using (var timeZone = new AndroidJavaClass("java.util.TimeZone"))
                using (AndroidJavaObject current = timeZone.CallStatic<AndroidJavaObject>("getDefault"))
                    region.TimezoneName = current.Call<string>("getID") ?? region.TimezoneName;

                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (AndroidJavaObject activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                using (AndroidJavaObject context = activity.Call<AndroidJavaObject>("getApplicationContext"))
                using (AndroidJavaObject telephony = context.Call<AndroidJavaObject>("getSystemService", "phone"))
                {
                    if (telephony != null)
                    {
                        region.SimCountry = (telephony.Call<string>("getSimCountryIso") ?? "").ToLowerInvariant();
                        region.NetworkCountry = (telephony.Call<string>("getNetworkCountryIso") ?? "").ToLowerInvariant();
                    }
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[HDCAds] cannot read the device region: " + exception.Message);
            }
        }

        private static string AndroidId()
        {
            try
            {
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (AndroidJavaObject activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                using (AndroidJavaObject context = activity.Call<AndroidJavaObject>("getApplicationContext"))
                using (AndroidJavaObject resolver = context.Call<AndroidJavaObject>("getContentResolver"))
                using (var secure = new AndroidJavaClass("android.provider.Settings$Secure"))
                    return secure.CallStatic<string>("getString", resolver, "android_id") ?? "";
            }
            catch (Exception)
            {
                return "";
            }
        }
#elif UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern string HDCAds_LocaleInfo();

        private static void ReadIos(HDCDeviceRegion region, List<string> languages, List<string> regions)
        {
            try
            {
                string[] parts = (HDCAds_LocaleInfo() ?? "").Split('|');
                if (parts.Length < 4)
                    return;
                languages.Add(parts[0].ToLowerInvariant());
                languages.Add(parts[1].Split('-', '_')[0].ToLowerInvariant());
                regions.Add(parts[2].ToLowerInvariant());
                if (parts[3].Length > 0)
                    region.TimezoneName = parts[3];
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[HDCAds] cannot read the device region: " + exception.Message);
            }
        }
#endif

#pragma warning disable 0649
        [Serializable]
        private sealed class DevicesValue
        {
            public string[] debugDevices;
        }
#pragma warning restore 0649
    }
}
