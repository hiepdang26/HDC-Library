using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HDC.Ads.Domain;
using HDC.Ads.Ports;
using UnityEngine;
#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace HDC.Ads.Infrastructure
{
    internal sealed class HDCDeviceRegionReader : IDeviceRegionSource
    {
        private static string deviceId;

        public bool IsEditor => Application.isEditor;

        public string DeviceId
        {
            get
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
        }

        public HDCDeviceRegion Read()
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
    }
}
