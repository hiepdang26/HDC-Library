using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HDC.Ads.Editor
{
    internal static class HDCTestScenes
    {
        internal const string AssemblyName = "HDC.Ads.TestScenes";
        internal const string BootScene = "HDCAdsTestBoot";
        internal const string GameScene = "HDCAdsTestGame";

        internal static string Folder => HDCAdsActivation.Root + "/Tests/Scenes";

        internal static string[] Scenes => new[] { Folder + "/" + BootScene + ".unity", Folder + "/" + GameScene + ".unity" };

        internal static bool IsInBuild => EditorBuildSettings.scenes.Any(scene => IsTestScene(scene.path));

        internal static void Open()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            EditorSceneManager.OpenScene(Scenes[0]);
            if (!IsInBuild)
                Debug.Log("[HDCAds] The boot scene opens the game scene by name, so add both to Build Settings before pressing Play: " +
                          "HDC > Test Scenes > Add to Build Settings.");
        }

        internal static void AddToBuild()
        {
            EditorBuildSettings.scenes = Scenes.Select(path => new EditorBuildSettingsScene(path, true))
                .Concat(EditorBuildSettings.scenes.Where(scene => !IsTestScene(scene.path)))
                .ToArray();
            Debug.LogWarning("[HDCAds] The HDC test scenes are now first in Build Settings, so builds start with them. " +
                             "Remove them before a release build: HDC > Test Scenes > Remove from Build Settings.");
        }

        internal static void RemoveFromBuild()
        {
            EditorBuildSettings.scenes = EditorBuildSettings.scenes.Where(scene => !IsTestScene(scene.path)).ToArray();
            Debug.Log("[HDCAds] The HDC test scenes are out of Build Settings.");
        }

        internal static string[] Filter(string[] assemblies, IEnumerable<string> buildScenes) =>
            buildScenes.Any(IsTestScene)
                ? assemblies
                : assemblies.Where(path => Path.GetFileNameWithoutExtension(path) != AssemblyName).ToArray();

        private static bool IsTestScene(string path) =>
            !string.IsNullOrEmpty(path) && path.Replace('\\', '/').StartsWith(Folder + "/", StringComparison.Ordinal);

        internal sealed class BuildFilter : IFilterBuildAssemblies
        {
            public int callbackOrder => 0;

            public string[] OnFilterAssemblies(BuildOptions buildOptions, string[] assemblies)
            {
                string[] kept = Filter(assemblies, EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path));
                if (kept.Length < assemblies.Length)
                    Debug.Log("[HDCAds] " + AssemblyName + " is left out of this build: no HDC test scene is in Build Settings.");
                return kept;
            }
        }
    }
}
