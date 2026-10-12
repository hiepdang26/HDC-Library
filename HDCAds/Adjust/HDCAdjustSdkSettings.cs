namespace HDC.Ads
{
    internal sealed class HDCAdjustSdkSettings
    {
        internal string AppToken { get; set; } = "";

        internal HDCAdjustEnvironment Environment { get; set; }

        internal HDCAdjustLogLevel LogLevel { get; set; }

        internal bool CoppaCompliance { get; set; }

        internal bool SendInBackground { get; set; }

        internal bool LaunchDeferredDeeplink { get; set; }

        internal bool CostDataInAttribution { get; set; }

        internal bool LinkMe { get; set; }

        internal string DefaultTracker { get; set; } = "";

        internal bool PreinstallTracking { get; set; }

        internal string PreinstallFilePath { get; set; } = "";

        internal bool AdServices { get; set; }

        internal bool IdfaReading { get; set; }

        internal bool SkanAttribution { get; set; }
    }
}
