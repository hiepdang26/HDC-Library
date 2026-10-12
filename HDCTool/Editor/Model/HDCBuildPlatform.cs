using UnityEditor;
using UnityEditor.Build;

namespace HDC.Tool.Editor
{
    internal enum HDCBuildPlatform
    {
        Ios,
        Android,
    }

    internal static class HDCBuildPlatforms
    {
        internal static BuildTarget Target(this HDCBuildPlatform platform) =>
            platform == HDCBuildPlatform.Ios ? BuildTarget.iOS : BuildTarget.Android;

        internal static BuildTargetGroup Group(this HDCBuildPlatform platform) =>
            platform == HDCBuildPlatform.Ios ? BuildTargetGroup.iOS : BuildTargetGroup.Android;

        internal static NamedBuildTarget Named(this HDCBuildPlatform platform) =>
            platform == HDCBuildPlatform.Ios ? NamedBuildTarget.iOS : NamedBuildTarget.Android;

        internal static string Title(this HDCBuildPlatform platform) => platform == HDCBuildPlatform.Ios ? "iOS" : "Android";

        internal static bool IsActive(this HDCBuildPlatform platform) => EditorUserBuildSettings.activeBuildTarget == platform.Target();
    }
}
