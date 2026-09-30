using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using GoogleMobileAds.Api;
using UnityEngine;

namespace HDC.Ads.Internal
{
    /// <summary>Shared setup and event helpers for the formats served through the Google Mobile Ads Unity plugin.</summary>
    internal static class HDCGma
    {
        private static bool initializeCalled;

        internal static void Initialize()
        {
            if (initializeCalled)
                return;
            initializeCalled = true;

            // Pauses Unity while a full-screen ad covers it on iOS, like the native formats do.
            MobileAds.SetiOSAppPauseOnBackground(true);
            MobileAds.Initialize(_ => { });
        }

        /// <summary>Lets the next Play Mode session initialize the plugin again; see HDCAds.</summary>
        internal static void ResetStatics() => initializeCalled = false;

        /// <summary>Adds this device's ids to the SDK's test devices, so every format serves test ads here.</summary>
        internal static void EnableTestDevice()
        {
            RequestConfiguration configuration = MobileAds.GetRequestConfiguration() ?? new RequestConfiguration();
            var ids = new HashSet<string>(configuration.TestDeviceIds ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
            foreach (string id in DeviceIds())
                ids.Add(id);
            configuration.TestDeviceIds = new List<string>(ids);
            MobileAds.SetRequestConfiguration(configuration);
        }

        // Android hashes ANDROID_ID; iOS hashes the advertising id or, without one, the vendor id that Unity
        // reports as deviceUniqueIdentifier. Both cases are added because the platforms print them differently.
        private static IEnumerable<string> DeviceIds()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            string androidId = null;
            try
            {
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (AndroidJavaObject activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                using (AndroidJavaObject resolver = activity.Call<AndroidJavaObject>("getContentResolver"))
                using (var secure = new AndroidJavaClass("android.provider.Settings$Secure"))
                {
                    androidId = secure.CallStatic<string>("getString", resolver, "android_id");
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[HDCAds] cannot read ANDROID_ID: " + exception.Message);
            }

            if (!string.IsNullOrEmpty(androidId))
            {
                yield return Md5(androidId).ToUpperInvariant();
                yield return Md5(androidId);
            }
#endif
            string deviceId = SystemInfo.deviceUniqueIdentifier;
            if (!string.IsNullOrEmpty(deviceId) && deviceId != SystemInfo.unsupportedIdentifier)
            {
                yield return Md5(deviceId);
                yield return Md5(deviceId).ToUpperInvariant();
            }

            yield return AdRequest.TestDeviceSimulator;
        }

        private static string Md5(string value)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] hash = md5.ComputeHash(Encoding.ASCII.GetBytes(value));
                var hex = new StringBuilder(hash.Length * 2);
                foreach (byte b in hash)
                    hex.Append(b.ToString("x2"));
                return hex.ToString();
            }
        }

        internal static HDCAdEvent Event(string id, string format, string type, string adUnitId, ResponseInfo response)
        {
            var adEvent = new HDCAdEvent { id = id, format = format, type = type, adUnitId = adUnitId };
            if (response == null)
                return adEvent;

            try
            {
                adEvent.responseId = response.GetResponseId();
                adEvent.adapter = response.GetMediationAdapterClassName();
                adEvent.adSource = response.GetLoadedAdapterResponseInfo()?.AdSourceName;
            }
            catch (Exception exception)
            {
                // Mediated responses are read over JNI or from native objects; a missing field must not drop the event.
                Debug.LogWarning("[HDCAds] cannot read the ad response: " + exception.Message);
            }

            return adEvent;
        }

        internal static HDCAdEvent WithError(this HDCAdEvent adEvent, AdError error, string fallbackMessage)
        {
            adEvent.code = -1;
            adEvent.message = fallbackMessage;
            if (error == null)
                return adEvent;

            try
            {
                adEvent.code = error.GetCode();
                adEvent.message = error.GetMessage();
            }
            catch (Exception)
            {
                adEvent.message = error.ToString();
            }

            return adEvent;
        }

        internal static HDCAdEvent WithValue(this HDCAdEvent adEvent, AdValue value)
        {
            if (value == null)
                return adEvent;

            adEvent.valueMicros = value.Value;
            adEvent.currency = value.CurrencyCode;
            adEvent.precision = (int)value.Precision;
            return adEvent;
        }
    }
}
