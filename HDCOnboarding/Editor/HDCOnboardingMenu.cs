using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HDC.Onboarding.Editor
{
    internal static class HDCOnboardingMenu
    {
        private const string Root = "HDC/Onboarding/";

        [MenuItem(Root + "Open Splash scene", false, 100)]
        private static void OpenSplash() => Open(HDCOnboardingPaths.SplashScene);

        [MenuItem(Root + "Open Intro scene", false, 101)]
        private static void OpenIntro() => Open(HDCOnboardingPaths.IntroScene);

        [MenuItem(Root + "Add scenes to Build Settings", false, 110)]
        internal static void AddScenesToBuild()
        {
            string[] onboarding = { HDCOnboardingPaths.SplashScene, HDCOnboardingPaths.IntroScene };
            EditorBuildSettings.scenes = onboarding.Select(path => new EditorBuildSettingsScene(path, true))
                .Concat(EditorBuildSettings.scenes.Where(scene => !onboarding.Contains(Normalize(scene.path))))
                .ToArray();
            Debug.Log("[Splash] Splash and Intro are now the first scenes in Build Settings, so builds start with the splash.");
        }

        [MenuItem(Root + "Add config keys", false, 111)]
        private static void AddConfigKey() => HDCOnboardingConfigKeys.AddMissing();

        [MenuItem(Root + "Move splash ad areas into prefabs", false, 112)]
        private static void MoveSplashAdAreas()
        {
            Scene scene = SceneManager.GetActiveScene();
            Debug.Log("[Splash] " + HDCSplashAdAreas.Move(scene) + " Save the scene to keep it.");
        }

        [MenuItem(Root + "Move splash ad areas into prefabs", true)]
        private static bool CanMoveSplashAdAreas() => HDCSplashAdAreas.FindSplash(SceneManager.GetActiveScene()) != null;

        private static void Open(string path)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            EditorSceneManager.OpenScene(path);
        }

        private static string Normalize(string path) => (path ?? "").Replace('\\', '/');
    }
}
