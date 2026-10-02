using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace HDC.Ads.DebugUI
{
    internal static class HDCAdjustProbe
    {
        private static readonly string[] TypeNames = { "AdjustSdk.Adjust", "com.adjust.sdk.Adjust" };
        private static readonly string[] AttributionFields =
        {
            "TrackerName", "Network", "Campaign", "Adgroup", "Creative", "ClickLabel", "TrackerToken", "CostType", "CostAmount", "CostCurrency",
        };

        private static readonly Dictionary<string, string> values = new Dictionary<string, string>();
        private const float AnswerSeconds = 3f;

        private static Type adjust;
        private static bool searched;
        private static string state = "Not checked";
        private static bool answered;
        private static float askedAt;

        internal static string State =>
            state == "OK" && !answered && Time.realtimeSinceStartup - askedAt > AnswerSeconds
                ? "Không có phản hồi: game chưa khởi tạo Adjust (Adjust.InitSdk)"
                : state;

        internal static bool Found => adjust != null;

        internal static IEnumerable<KeyValuePair<string, string>> Values => values;

        internal static void Ask()
        {
            if (!searched)
            {
                searched = true;
                adjust = FindAdjust();
            }

            if (adjust == null)
            {
                state = "Adjust SDK không có trong build";
                return;
            }

            if (Application.isEditor)
            {
                state = "Adjust chỉ chạy trên máy Android và iOS";
                return;
            }

            state = "OK";
            answered = false;
            askedAt = Time.realtimeSinceStartup;
            Text("SDK Version", "GetSdkVersion", "getSdkVersion");
            Flag("Enabled", "IsEnabled", "isEnabled");
            Text("Adjust ID (adid)", "GetAdid", "getAdid");
            Attribution();
        }

        private static Type FindAdjust()
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (string name in TypeNames)
                {
                    Type type = assembly.GetType(name, false);
                    if (type != null)
                        return type;
                }
            }

            return null;
        }

        private static void Text(string label, string asyncName, string syncName)
        {
            try
            {
                MethodInfo async = adjust.GetMethod(asyncName, BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(Action<string>) }, null);
                if (async != null)
                {
                    async.Invoke(null, new object[] { (Action<string>)(value => Set(label, value)) });
                    return;
                }

                MethodInfo sync = adjust.GetMethod(syncName, BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null);
                Set(label, sync != null ? sync.Invoke(null, null) as string : "(not in this SDK)");
            }
            catch (Exception exception)
            {
                Set(label, "Failed: " + exception.GetBaseException().Message);
            }
        }

        private static void Flag(string label, string asyncName, string syncName)
        {
            try
            {
                MethodInfo async = adjust.GetMethod(asyncName, BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(Action<bool>) }, null);
                if (async != null)
                {
                    async.Invoke(null, new object[] { (Action<bool>)(value => Set(label, value ? "Yes" : "No")) });
                    return;
                }

                MethodInfo sync = adjust.GetMethod(syncName, BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null);
                Set(label, sync != null && sync.Invoke(null, null) is bool on ? on ? "Yes" : "No" : "(not in this SDK)");
            }
            catch (Exception exception)
            {
                Set(label, "Failed: " + exception.GetBaseException().Message);
            }
        }

        private static void Attribution()
        {
            try
            {
                Type attributionType = adjust.Assembly.GetType(adjust.Namespace + ".AdjustAttribution", false);
                if (attributionType == null)
                {
                    Set("Attribution", "(not in this SDK)");
                    return;
                }

                Type callbackType = typeof(Action<>).MakeGenericType(attributionType);
                MethodInfo async = adjust.GetMethod("GetAttribution", BindingFlags.Public | BindingFlags.Static, null, new[] { callbackType }, null);
                if (async != null)
                {
                    MethodInfo handler = typeof(HDCAdjustProbe).GetMethod(nameof(OnAttribution), BindingFlags.NonPublic | BindingFlags.Static);
                    async.Invoke(null, new object[] { Delegate.CreateDelegate(callbackType, handler) });
                    return;
                }

                MethodInfo sync = adjust.GetMethod("getAttribution", BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null);
                OnAttribution(sync?.Invoke(null, null));
            }
            catch (Exception exception)
            {
                Set("Attribution", "Failed: " + exception.GetBaseException().Message);
            }
        }

        private static void OnAttribution(object attribution)
        {
            if (attribution == null)
            {
                Set("Attribution", "None yet");
                return;
            }

            Type type = attribution.GetType();
            foreach (string field in AttributionFields)
            {
                string lower = char.ToLowerInvariant(field[0]) + field.Substring(1);
                object value = type.GetProperty(field)?.GetValue(attribution, null)
                    ?? type.GetProperty(lower)?.GetValue(attribution, null)
                    ?? type.GetField(field)?.GetValue(attribution)
                    ?? type.GetField(lower)?.GetValue(attribution);
                Set(field, value?.ToString());
            }
        }

        private static void Set(string label, string value)
        {
            answered = true;
            values[label] = string.IsNullOrEmpty(value) ? "-" : value;
        }

        internal static IEnumerable<KeyValuePair<string, string>> Ordered()
        {
            string[] order = new[] { "SDK Version", "Enabled", "Adjust ID (adid)", "Attribution" }.Concat(AttributionFields).ToArray();
            return values.OrderBy(pair => Array.IndexOf(order, pair.Key) < 0 ? int.MaxValue : Array.IndexOf(order, pair.Key));
        }
    }
}
