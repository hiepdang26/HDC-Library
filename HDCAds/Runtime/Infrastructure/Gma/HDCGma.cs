using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using GoogleMobileAds.Api;
using HDC.Ads.Diagnostics;
using HDC.Ads.Domain;
using UnityEngine;

namespace HDC.Ads.Infrastructure
{
    internal static class HDCGma
    {
        private static bool initializeCalled;

        internal static void Initialize()
        {
            if (initializeCalled)
                return;
            initializeCalled = true;

            MobileAds.SetiOSAppPauseOnBackground(true);
            MobileAds.Initialize(status => HDCMainThread.Post(() => HDCMediationReport.AdaptersStarted(Adapters(status))));
        }

        private static IEnumerable<HDCAdapterStatus> Adapters(InitializationStatus status)
        {
            Dictionary<string, AdapterStatus> map = status?.getAdapterStatusMap();
            if (map == null)
                yield break;
            foreach (KeyValuePair<string, AdapterStatus> pair in map)
            {
                if (pair.Value != null)
                    yield return new HDCAdapterStatus(pair.Key, pair.Value.InitializationState == AdapterState.Ready, pair.Value.Description, pair.Value.Latency);
            }
        }

        internal static void ResetStatics() => initializeCalled = false;

        internal static void OpenAdInspector(Action<string> onClosed) =>
            MobileAds.OpenAdInspector(error =>
            {
                string problem = error == null ? null : $"{error.GetCode()} · {error.GetMessage()}";
                HDCMainThread.Post(() => onClosed?.Invoke(problem));
            });

        internal static void EnableTestDevice()
        {
            RequestConfiguration configuration = MobileAds.GetRequestConfiguration() ?? new RequestConfiguration();
            var ids = new HashSet<string>(configuration.TestDeviceIds ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
            foreach (string id in TestDeviceIds())
                ids.Add(id);
            configuration.TestDeviceIds = new List<string>(ids);
            MobileAds.SetRequestConfiguration(configuration);
        }

        internal static IEnumerable<string> TestDeviceIds()
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
#if UNITY_IOS && !UNITY_EDITOR
            string advertisingId = UnityEngine.iOS.Device.advertisingIdentifier;
            if (IsRealAdvertisingId(advertisingId))
            {
                yield return Md5(advertisingId);
                yield return Md5(advertisingId).ToUpperInvariant();
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

        internal static bool IsRealAdvertisingId(string advertisingId) =>
            !string.IsNullOrWhiteSpace(advertisingId) && advertisingId.Trim().Trim('0', '-').Length > 0;

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

        internal static HDCAdEvent Event(string id, string format, string type, string adUnitId, HDCGmaResponse response)
        {
            var adEvent = new HDCAdEvent { id = id, format = format, type = type, adUnitId = adUnitId };
            if (response == null)
                return adEvent;

            adEvent.responseId = response.ResponseId;
            adEvent.adapter = response.Adapter;
            adEvent.adSource = response.AdSource;
            return adEvent;
        }

        internal static HDCAdEvent WithError(this HDCAdEvent adEvent, HDCGmaFailure failure)
        {
            adEvent.code = failure?.Code ?? -1;
            adEvent.message = failure?.Message;
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
