using System;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;

namespace HDC.Ads
{
    internal static class HDCAdjustAttributions
    {
        private const int NetworkMaxLength = 30;

        internal static HDCAdjustAttribution Current { get; private set; }

        internal static string Network { get; private set; } = "";

        internal static bool TimedOut { get; private set; }

        internal static event Action<HDCAdjustAttribution> Changed;

        internal static string NormalizeNetwork(string network)
        {
            if (string.IsNullOrEmpty(network))
                return "";
            if (char.IsDigit(network[0]))
                network = network.Substring(1);
            string normalized = Regex.Replace(network.Replace(" ", "_"), "[^a-zA-Z0-9_]", "").ToLowerInvariant();
            return normalized.Length > NetworkMaxLength ? normalized.Substring(0, NetworkMaxLength) : normalized;
        }

        internal static void Receive(HDCAdjustAttribution attribution)
        {
            if (attribution == null || attribution.IsEmpty || attribution.SameAs(Current))
                return;

            Current = attribution;
            Network = NormalizeNetwork(attribution.Network);
            Store(attribution);
            Action<HDCAdjustAttribution> handlers = Changed;
            if (handlers == null)
                return;
            foreach (Action<HDCAdjustAttribution> handler in handlers.GetInvocationList())
            {
                try
                {
                    handler(attribution);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }
        }

        internal static void MarkTimedOut()
        {
            if (Current != null)
                return;
            TimedOut = true;
            Network = HDCAdjust.TimedOutNetwork;
        }

        internal static void Reset()
        {
            Current = null;
            Network = "";
            Changed = null;
            TimedOut = false;
        }

        private static void Store(HDCAdjustAttribution attribution)
        {
            PlayerPrefs.SetString(HDCAdjust.NetworkKey, attribution.Network);
            PlayerPrefs.SetString(HDCAdjust.CampaignKey, attribution.Campaign);
            PlayerPrefs.SetString(HDCAdjust.CreativeKey, attribution.Creative);
            if (attribution.CostAmount.HasValue)
                PlayerPrefs.SetString(HDCAdjust.CostKey, attribution.CostAmount.Value.ToString(CultureInfo.InvariantCulture));
            PlayerPrefs.Save();
        }
    }
}
