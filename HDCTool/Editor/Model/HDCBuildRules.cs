using System;
using System.Collections.Generic;
using HDC.Ads;

namespace HDC.Tool.Editor
{
    [Serializable]
    internal sealed class HDCBuildRules
    {
        public bool googleTestAds;
        public bool googleTestAdUnits;
        public bool debugPanel;
        public bool debugLog;
        public HDCAdjustEnvironment adjustEnvironment = HDCAdjustEnvironment.Production;
        public bool developmentBuild;
        public bool androidAppBundle;
        public bool requireChangeLog;

        internal static HDCBuildRules Defaults(HDCBuildPreset preset) =>
            preset == HDCBuildPreset.Release
                ? new HDCBuildRules { adjustEnvironment = HDCAdjustEnvironment.Production, androidAppBundle = true, requireChangeLog = true }
                : new HDCBuildRules
                {
                    googleTestAds = true,
                    googleTestAdUnits = true,
                    debugPanel = true,
                    debugLog = true,
                    adjustEnvironment = HDCAdjustEnvironment.Sandbox,
                };

        internal HDCBuildRules Copy() => (HDCBuildRules)MemberwiseClone();

        internal string Describe(HDCBuildPlatform platform)
        {
            var parts = new List<string>
            {
                platform == HDCBuildPlatform.Android ? (androidAppBundle ? "AAB" : "APK") : "Xcode project",
                googleTestAds || googleTestAdUnits ? "Google test ads" : "real ads",
                debugPanel ? "debug panel" : "no debug panel",
                "Adjust " + adjustEnvironment.ToString().ToLowerInvariant(),
            };
            if (developmentBuild)
                parts.Add("development build");
            return string.Join(", ", parts);
        }
    }
}
