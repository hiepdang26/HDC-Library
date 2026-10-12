using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using HDC.Ads.Editor;
using UnityEditor;
using UnityEngine;

namespace HDC.Tool.Editor
{
    internal static class HDCBuildReport
    {
        internal const string Suffix = "_buildreport.txt";

        internal static string Write(HDCBuildResult result, HDCBuildRules rules, HDCBuildSettings settings, IEnumerable<HDCBuildCheck> checks,
            string device, IEnumerable<string> buildErrors)
        {
            string output = result.outputPath.TrimEnd('/', '\\');
            string folder = result.platform == HDCBuildPlatform.Ios ? output : Path.GetDirectoryName(output);
            if (string.IsNullOrEmpty(folder))
                return "";
            string path = Path.Combine(folder, result.buildName + Suffix);
            try
            {
                Directory.CreateDirectory(folder);
                File.WriteAllText(path, Text(result, rules, settings, checks, device, buildErrors), new UTF8Encoding(false));
                return path;
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[HDCTool] Could not write the build report: " + exception.Message);
                return "";
            }
        }

        internal static string Text(HDCBuildResult result, HDCBuildRules rules, HDCBuildSettings settings, IEnumerable<HDCBuildCheck> checks,
            string device, IEnumerable<string> buildErrors)
        {
            HDCBuildPlatform platform = result.platform;
            var text = new StringBuilder();
            text.AppendLine("HDC BUILD REPORT");
            Section(text, "1. Overview");
            Line(text, "Platform", platform.Title());
            Line(text, "Preset", result.preset.Title());
            Line(text, "Build name", result.buildName);
            Line(text, "Result", result.outcome + (result.message.Length > 0 ? " (" + result.message + ")" : ""));
            Line(text, "Output", result.outputPath);
            Line(text, "Size", result.sizeBytes > 0 ? (result.sizeBytes / 1048576d).ToString("0.0", CultureInfo.InvariantCulture) + " MB" : "-");
            Line(text, "Duration", TimeSpan.FromSeconds(result.seconds).ToString(@"m\m\ s\s", CultureInfo.InvariantCulture));
            Line(text, "Finished", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));

            Section(text, "2. Player settings");
            Line(text, "Product name", PlayerSettings.productName);
            Line(text, "Company name", PlayerSettings.companyName);
            Line(text, "Version", PlayerSettings.bundleVersion);
            Line(text, platform == HDCBuildPlatform.Ios ? "Build number" : "Version code", HDCBuildNames.Number(platform));
            Line(text, platform == HDCBuildPlatform.Ios ? "Bundle ID" : "Package name", PlayerSettings.GetApplicationIdentifier(platform.Named()));
            Line(text, "Orientation", HDCOrientations.Current.Title());
            if (platform == HDCBuildPlatform.Android)
            {
                Line(text, "Keystore", PlayerSettings.Android.useCustomKeystore ? PlayerSettings.Android.keystoreName : "Unity debug keystore");
                Line(text, "Key alias", PlayerSettings.Android.useCustomKeystore ? PlayerSettings.Android.keyaliasName : "-");
            }
            else
            {
                Line(text, "Run on", result.iosTarget.Title());
                Line(text, "Target SDK", result.iosTarget.Sdk());
                Line(text, "Minimum iOS", PlayerSettings.iOS.targetOSVersionString);
                Line(text, "Team ID (this Mac)", HDCIosLocalBuild.TeamId.Length > 0 ? HDCIosLocalBuild.TeamId : "-");
            }

            Section(text, "3. Preset rules");
            Line(text, "Google test ads", OnOff(rules.googleTestAds));
            Line(text, "Google test ad units", OnOff(rules.googleTestAdUnits));
            Line(text, "Debug panel", OnOff(rules.debugPanel));
            Line(text, "Debug log", OnOff(rules.debugLog));
            Line(text, "Adjust environment", rules.adjustEnvironment.ToString());
            Line(text, "Development build", OnOff(rules.developmentBuild));
            if (platform == HDCBuildPlatform.Android)
                Line(text, "App bundle (AAB)", OnOff(rules.androidAppBundle));

            Section(text, "4. Machine and device");
            Line(text, "Machine", Environment.MachineName);
            Line(text, "Unity", Application.unityVersion);
            Line(text, "Device", string.IsNullOrEmpty(device) ? "-" : device);

            Section(text, "5. Change log");
            text.AppendLine(string.IsNullOrWhiteSpace(settings.ChangeLog) ? "-" : settings.ChangeLog.Trim());

            Section(text, "6. Validation scan");
            foreach (HDCBuildCheck check in checks ?? Enumerable.Empty<HDCBuildCheck>())
                text.AppendLine($"[{check.Level.ToString().ToUpperInvariant()}] {check.Name}" + (check.Message.Length > 0 ? ": " + check.Message : ""));

            List<string> errors = (buildErrors ?? Enumerable.Empty<string>()).Take(50).ToList();
            if (errors.Count > 0)
            {
                Section(text, "7. Build errors");
                foreach (string error in errors)
                    text.AppendLine(error);
            }

            return text.ToString();
        }

        private static void Section(StringBuilder text, string title)
        {
            text.AppendLine();
            text.AppendLine(title);
            text.AppendLine(new string('-', title.Length));
        }

        private static void Line(StringBuilder text, string label, string value) => text.AppendLine($"{label}: {(string.IsNullOrEmpty(value) ? "-" : value)}");

        private static string OnOff(bool value) => value ? "ON" : "OFF";
    }
}
