namespace HDC.Ads.Diagnostics
{
    internal sealed class HDCConfigFinding
    {
        internal HDCConfigFinding(HDCConfigLevel level, string text)
        {
            Level = level;
            Text = text;
        }

        internal HDCConfigLevel Level { get; }
        internal string Text { get; }

        internal static HDCConfigFinding Error(string text) => new HDCConfigFinding(HDCConfigLevel.Error, text);

        internal static HDCConfigFinding Warning(string text) => new HDCConfigFinding(HDCConfigLevel.Warning, text);

        internal static HDCConfigFinding Info(string text) => new HDCConfigFinding(HDCConfigLevel.Info, text);
    }
}
