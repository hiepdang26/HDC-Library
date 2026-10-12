using System;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;

namespace HDC.Tool.Editor
{
    internal static class HDCBuildNames
    {
        internal static string ProductCode(string productName)
        {
            string name = productName ?? "";
            string capitals = new string(name.Where(char.IsUpper).ToArray());
            if (capitals.Length > 0)
                return capitals;
            string letters = new string(name.Where(char.IsLetterOrDigit).ToArray());
            return letters.Length > 0 ? letters.Substring(0, Math.Min(3, letters.Length)).ToUpperInvariant() : "APP";
        }

        internal static string Artifact(HDCBuildPlatform platform, bool appBundle) =>
            platform == HDCBuildPlatform.Ios ? "Xcode" : appBundle ? "AAB" : "APK";

        internal static string BuildName(string productName, HDCBuildPreset preset, string version, string number, string artifact, bool development) =>
            $"{ProductCode(productName)}_{preset.Token()}_{Clean(version)}_{Clean(number)}_{artifact}" + (development ? "_dev" : "");

        internal static string Number(HDCBuildPlatform platform) =>
            platform == HDCBuildPlatform.Ios ? PlayerSettings.iOS.buildNumber : PlayerSettings.Android.bundleVersionCode.ToString();

        internal static string Current(HDCBuildPlatform platform, HDCBuildPreset preset, HDCBuildRules rules) =>
            BuildName(PlayerSettings.productName, preset, PlayerSettings.bundleVersion, Number(platform),
                Artifact(platform, rules.androidAppBundle), rules.developmentBuild);

        internal static string NextNumber(string number)
        {
            string value = (number ?? "").Trim();
            int dot = value.LastIndexOf('.');
            string head = dot >= 0 ? value.Substring(0, dot + 1) : "";
            string tail = dot >= 0 ? value.Substring(dot + 1) : value;
            return long.TryParse(tail, out long last) && last >= 0 ? head + (last + 1) : value.Length == 0 ? "1" : value;
        }

        private static string Clean(string value) => Regex.Replace(string.IsNullOrWhiteSpace(value) ? "0" : value.Trim(), "[^A-Za-z0-9._-]", "-");
    }
}
