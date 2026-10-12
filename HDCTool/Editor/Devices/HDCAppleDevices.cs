using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace HDC.Tool.Editor
{
    internal sealed class HDCAppleDevice
    {
        internal const string MacName = "My Mac";

        internal HDCAppleTarget Kind = HDCAppleTarget.Phone;
        internal string Name = "";
        internal string OsVersion = "";
        internal string Udid = "";
        internal bool Booted;

        internal string Title =>
            Kind == HDCAppleTarget.Mac ? MacName + " (Designed for iPad)"
            : $"{Name} · iOS {OsVersion}" + (Kind == HDCAppleTarget.Simulator && Booted ? " · running" : "");
    }

    internal static class HDCAppleDevices
    {
        private static readonly Regex DeviceLine = new Regex(@"^(?<name>.+) \((?<os>\d+(\.\d+)*)\) \((?<udid>[0-9A-Fa-f-]+)\)$");
        private static readonly Regex Runtime = new Regex("\"com\\.apple\\.CoreSimulator\\.SimRuntime\\.iOS-(?<version>[0-9-]+)\"\\s*:\\s*\\[(?<devices>[^\\]]*)\\]");
        private static readonly Regex DeviceObject = new Regex("\\{(?<body>[^{}]*)\\}");
        private static readonly Regex ProvisioningUdid = new Regex(@"Provisioning UDID:\s*(?<udid>[0-9A-Fa-f-]+)");

        internal static List<HDCAppleDevice> All()
        {
            var devices = new List<HDCAppleDevice>(List());
            HDCAppleDevice mac = Mac();
            if (mac != null)
                devices.Add(mac);
            devices.AddRange(Simulators());
            return devices;
        }

        internal static List<HDCAppleDevice> List()
        {
            HDCProcess.Output output = HDCProcess.Run("/usr/bin/xcrun", "xctrace list devices", 30000);
            return Parse(output.All);
        }

        internal static List<HDCAppleDevice> Simulators()
        {
            HDCProcess.Output output = HDCProcess.Run("/usr/bin/xcrun", "simctl list devices available -j", 30000);
            return output.Ok ? ParseSimulators(output.Text) : new List<HDCAppleDevice>();
        }

        internal static HDCAppleDevice Mac()
        {
            if (RuntimeInformation.OSArchitecture != Architecture.Arm64)
                return null;
            HDCProcess.Output output = HDCProcess.Run("/usr/sbin/system_profiler", "SPHardwareDataType", 30000);
            Match match = ProvisioningUdid.Match(output.Text ?? "");
            return new HDCAppleDevice { Kind = HDCAppleTarget.Mac, Name = HDCAppleDevice.MacName, Udid = match.Success ? match.Groups["udid"].Value : "" };
        }

        internal static List<HDCAppleDevice> Parse(string text)
        {
            var devices = new List<HDCAppleDevice>();
            bool online = false;
            foreach (string raw in (text ?? "").Split('\n'))
            {
                string line = raw.Trim();
                if (line.StartsWith("==", StringComparison.Ordinal))
                {
                    online = line == "== Devices ==";
                    continue;
                }

                if (!online)
                    continue;
                Match match = DeviceLine.Match(line);
                if (match.Success)
                    devices.Add(new HDCAppleDevice { Name = match.Groups["name"].Value, OsVersion = match.Groups["os"].Value, Udid = match.Groups["udid"].Value });
            }

            return devices;
        }

        internal static List<HDCAppleDevice> ParseSimulators(string json)
        {
            var devices = new List<HDCAppleDevice>();
            foreach (Match runtime in Runtime.Matches(json ?? ""))
            {
                string version = runtime.Groups["version"].Value.Replace('-', '.');
                foreach (Match device in DeviceObject.Matches(runtime.Groups["devices"].Value))
                {
                    string body = device.Groups["body"].Value;
                    if (Value(body, "isAvailable") == "false")
                        continue;
                    string udid = Value(body, "udid");
                    if (udid.Length == 0)
                        continue;
                    devices.Add(new HDCAppleDevice
                    {
                        Kind = HDCAppleTarget.Simulator,
                        Name = Value(body, "name"),
                        OsVersion = version,
                        Udid = udid,
                        Booted = Value(body, "state") == "Booted",
                    });
                }
            }

            devices.Sort((a, b) => b.Booted != a.Booted ? (b.Booted ? 1 : -1) : string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
            return devices;
        }

        private static string Value(string body, string key)
        {
            Match match = Regex.Match(body, "\"" + key + "\"\\s*:\\s*(\"(?<text>(?:[^\"\\\\]|\\\\.)*)\"|(?<plain>true|false))");
            return !match.Success ? "" : match.Groups["plain"].Success ? match.Groups["plain"].Value : match.Groups["text"].Value.Replace("\\/", "/");
        }
    }
}
