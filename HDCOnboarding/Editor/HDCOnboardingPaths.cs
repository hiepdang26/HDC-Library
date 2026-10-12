using System.IO;
using UnityEditor.Compilation;

namespace HDC.Onboarding.Editor
{
    internal static class HDCOnboardingPaths
    {
        internal const string SplashSceneName = "Splash";
        internal const string IntroSceneName = "Intro";
        private const string EditorAssembly = "HDC.Onboarding.Editor";
        private const string Fallback = "Assets/HDCLib/HDCOnboarding";

        internal static string Root
        {
            get
            {
                string asmdef = CompilationPipeline.GetAssemblyDefinitionFilePathFromAssemblyName(EditorAssembly);
                if (string.IsNullOrEmpty(asmdef))
                    return Fallback;
                string editorFolder = Path.GetDirectoryName(asmdef);
                return (Path.GetDirectoryName(editorFolder) ?? Fallback).Replace('\\', '/');
            }
        }

        internal static string ScenesFolder => Root + "/Scenes";

        internal static string PrefabsFolder => Root + "/Prefabs";

        internal static string SplashScene => ScenesFolder + "/" + SplashSceneName + ".unity";

        internal static string IntroScene => ScenesFolder + "/" + IntroSceneName + ".unity";

        internal static string NoInternetPrefab => PrefabsFolder + "/HDCNoInternet.prefab";

        internal static string NativeBannerPrefab => PrefabsFolder + "/HDCNativeBanner.prefab";

        internal static string NativeSplashAreaPrefab => PrefabsFolder + "/HDCNativeSplashArea.prefab";

        internal static string NativeSplashAdMobAreaPrefab => PrefabsFolder + "/HDCNativeSplashAdMobArea.prefab";
    }
}
