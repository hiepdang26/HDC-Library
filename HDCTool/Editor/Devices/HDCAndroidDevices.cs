using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace HDC.Tool.Editor
{
    internal sealed class HDCAndroidDevice
    {
        internal string Serial = "";
        internal string State = "";
        internal string Model = "";
        internal string Details = "";

        internal bool Ready => State == "device";

        internal string Title => (Model.Length > 0 ? Model.Replace('_', ' ') : Serial) + (Ready ? "" : " (" + State + ")");
    }

    internal static class HDCAndroidDevices
    {
        private const string LauncherCategory = "android.intent.category.LAUNCHER";

        internal static string AdbPath()
        {
            string executable = Application.platform == RuntimePlatform.WindowsEditor ? "adb.exe" : "adb";
            foreach (string root in new[] { UnitySdkRoot(), Environment.GetEnvironmentVariable("ANDROID_HOME"), Environment.GetEnvironmentVariable("ANDROID_SDK_ROOT") })
            {
                if (string.IsNullOrEmpty(root))
                    continue;
                string candidate = Path.Combine(root, "platform-tools", executable);
                if (File.Exists(candidate))
                    return candidate;
            }

            return HDCProcess.Find("adb") ?? executable;
        }

        internal static List<HDCAndroidDevice> List(string adb)
        {
            HDCProcess.Output output = HDCProcess.Run(adb, "devices -l", 15000);
            List<HDCAndroidDevice> devices = output.Ok ? Parse(output.Text) : new List<HDCAndroidDevice>();
            foreach (HDCAndroidDevice device in devices.Where(device => device.Ready))
                device.Details = Describe(adb, device.Serial);
            return devices;
        }

        internal static List<HDCAndroidDevice> Parse(string text)
        {
            var devices = new List<HDCAndroidDevice>();
            foreach (string raw in (text ?? "").Split('\n'))
            {
                string line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("List of devices", StringComparison.Ordinal) || line.StartsWith("*", StringComparison.Ordinal))
                    continue;
                string[] parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2)
                    continue;
                var device = new HDCAndroidDevice { Serial = parts[0], State = parts[1] };
                foreach (string part in parts.Skip(2))
                {
                    if (part.StartsWith("model:", StringComparison.Ordinal))
                        device.Model = part.Substring("model:".Length);
                }

                devices.Add(device);
            }

            return devices;
        }

        internal static string Describe(string adb, string serial)
        {
            HDCProcess.Output output = HDCProcess.Run(adb, $"-s {serial} shell getprop ro.build.version.release; getprop ro.build.version.sdk; wm size", 10000);
            if (!output.Ok)
                return "";
            string[] lines = output.Text.Split('\n').Select(line => line.Trim()).Where(line => line.Length > 0).ToArray();
            string version = lines.Length > 0 ? lines[0] : "?";
            string sdk = lines.Length > 1 ? lines[1] : "?";
            string size = lines.Length > 2 ? lines[2].Replace("Physical size:", "").Trim() : "";
            return $"Android {version} (SDK {sdk})" + (size.Length > 0 ? ", " + size : "");
        }

        internal static HDCProcess.Output Install(string serial, string apk) => Adb(serial, $"install -r \"{apk}\"", 300000);

        internal static HDCProcess.Output Uninstall(string serial, string package) => Adb(serial, "uninstall " + package, 60000);

        internal static HDCProcess.Output Launch(string serial, string package) =>
            Adb(serial, $"shell monkey -p {package} -c {LauncherCategory} 1", 30000);

        internal static HDCProcess.Output Stop(string serial, string package) => Adb(serial, "shell am force-stop " + package, 15000);

        internal static HDCProcess.Output ClearData(string serial, string package) => Adb(serial, "shell pm clear " + package, 30000);

        private static HDCProcess.Output Adb(string serial, string arguments, int timeoutMs) =>
            HDCProcess.Run(AdbPath(), (string.IsNullOrEmpty(serial) ? "" : $"-s {serial} ") + arguments, timeoutMs);

        private static string UnitySdkRoot()
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType("UnityEditor.Android.AndroidExternalToolsSettings", false);
                PropertyInfo property = type?.GetProperty("sdkRootPath", BindingFlags.Public | BindingFlags.Static);
                if (property != null)
                    return property.GetValue(null) as string;
            }

            return null;
        }
    }
}
