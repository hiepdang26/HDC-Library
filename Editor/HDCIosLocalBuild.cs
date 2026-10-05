using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;

namespace HDC.Ads.Editor
{
    internal enum HDCMacRun
    {
        XcodeDefault,
        On,
        Off,
    }

    internal static class HDCIosLocalBuild
    {
        private const string TeamIdKey = "HDC.iOS.TeamId";
        private const string BundleIdKey = "HDC.iOS.BundleId";
        private const string MacRunKey = "HDC.iOS.MacRun";

        private static readonly Regex TeamIdPattern = new Regex("^[A-Z0-9]{10}$");
        private static readonly Regex BundleIdPattern = new Regex(@"^[A-Za-z0-9-]+(\.[A-Za-z0-9-]+)+$");

        internal static string TeamId
        {
            get => Read(TeamIdKey);
            set => Write(TeamIdKey, value);
        }

        internal static string BundleId
        {
            get => Read(BundleIdKey);
            set => Write(BundleIdKey, value);
        }

        internal static HDCMacRun MacRun
        {
            get => Enum.TryParse(Read(MacRunKey), out HDCMacRun mode) && Enum.IsDefined(typeof(HDCMacRun), mode) ? mode : HDCMacRun.XcodeDefault;
            set => Write(MacRunKey, value == HDCMacRun.XcodeDefault ? "" : value.ToString());
        }

        internal static bool HasSigning => TeamId.Length > 0 || BundleId.Length > 0;

        internal static bool IsSet => HasSigning || MacRun != HDCMacRun.XcodeDefault;

        internal static string Summary
        {
            get
            {
                var parts = new List<string>();
                if (TeamId.Length > 0)
                    parts.Add("team " + TeamId);
                if (BundleId.Length > 0)
                    parts.Add("bundle ID " + BundleId);
                if (MacRun != HDCMacRun.XcodeDefault)
                    parts.Add("run on My Mac " + MacRun);
                return parts.Count > 0 ? string.Join(", ", parts) : "none";
            }
        }

        internal static void ClearSigning()
        {
            TeamId = "";
            BundleId = "";
        }

        internal static bool IsTeamId(string value) => TeamIdPattern.IsMatch(value ?? "");

        internal static bool IsBundleId(string value) => BundleIdPattern.IsMatch(value ?? "");

        private static string Read(string key) => (EditorUserSettings.GetConfigValue(key) ?? "").Trim();

        private static void Write(string key, string value)
        {
            string trimmed = (value ?? "").Trim();
            if (trimmed != Read(key))
                EditorUserSettings.SetConfigValue(key, trimmed);
        }
    }
}
