using System;
using System.Globalization;

namespace HDC.Tool.Editor
{
    [Serializable]
    internal sealed class HDCBuildResult
    {
        internal const string SucceededOutcome = "Succeeded";
        internal const string FailedOutcome = "Failed";
        internal const string CancelledOutcome = "Cancelled";
        internal const string BlockedOutcome = "Blocked";

        public HDCBuildPlatform platform;
        public HDCBuildPreset preset;
        public string outcome = "";
        public string message = "";
        public string buildName = "";
        public string outputPath = "";
        public string reportPath = "";
        public double seconds;
        public long sizeBytes;
        public string finishedAt = "";
        public HDCAppleTarget iosTarget;

        internal bool Succeeded => outcome == SucceededOutcome;

        internal static HDCBuildResult Stopped(HDCBuildPlatform platform, HDCBuildPreset preset, string outcome, string message) =>
            new HDCBuildResult { platform = platform, preset = preset, outcome = outcome, message = message, finishedAt = DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture) };
    }
}
