namespace HDC.Tool.Editor
{
    internal enum HDCBuildPreset
    {
        TestDebug,
        Release,
    }

    internal static class HDCBuildPresets
    {
        internal static string Title(this HDCBuildPreset preset) => preset == HDCBuildPreset.Release ? "Release" : "Test Debug";

        internal static string Token(this HDCBuildPreset preset) => preset == HDCBuildPreset.Release ? "release" : "testDebug";
    }
}
