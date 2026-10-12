using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HDC.Ads.Domain
{
    internal static class HDCDebugDevices
    {
        internal static List<string> Parse(string devicesJson)
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

#pragma warning disable 0649
        [Serializable]
        private sealed class DevicesValue
        {
            public string[] debugDevices;
        }
#pragma warning restore 0649
    }
}
