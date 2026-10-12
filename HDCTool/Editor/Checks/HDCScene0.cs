using System.Collections.Generic;
using System.IO;
using System.Linq;
using HDC.Ads;
using HDC.Ads.DebugUI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HDC.Tool.Editor
{
    internal sealed class HDCScene0
    {
        internal string Path { get; private set; } = "";

        internal bool Inspected { get; private set; }

        internal int Setups { get; private set; }

        internal int DebugPanels { get; private set; }

        internal List<(string Android, string Ios)> AdjustTokens { get; } = new List<(string Android, string Ios)>();

        internal string Name => System.IO.Path.GetFileNameWithoutExtension(Path);

        internal static string FirstScenePath() =>
            EditorBuildSettings.scenes.FirstOrDefault(scene => scene.enabled && File.Exists(scene.path))?.path ?? "";

        internal static bool IsLoaded(string path)
        {
            Scene scene = SceneManager.GetSceneByPath(path ?? "");
            return scene.IsValid() && scene.isLoaded;
        }

        internal static HDCScene0 Inspect()
        {
            var result = new HDCScene0 { Path = FirstScenePath() };
            if (result.Path.Length == 0 || EditorApplication.isPlayingOrWillChangePlaymode)
                return result;

            Scene scene = SceneManager.GetSceneByPath(result.Path);
            bool opened = false;
            if (!scene.IsValid() || !scene.isLoaded)
            {
                scene = EditorSceneManager.OpenScene(result.Path, OpenSceneMode.Additive);
                opened = true;
            }

            try
            {
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    result.Setups += root.GetComponentsInChildren<HDCAdsSetup>(true).Length;
                    result.DebugPanels += root.GetComponentsInChildren<HDCAdsDebugPanel>(true).Length;
                    foreach (HDCAdjust adjust in root.GetComponentsInChildren<HDCAdjust>(true))
                    {
                        var serialized = new SerializedObject(adjust);
                        result.AdjustTokens.Add((serialized.FindProperty("androidAppToken").stringValue, serialized.FindProperty("iosAppToken").stringValue));
                    }
                }

                result.Inspected = true;
            }
            finally
            {
                if (opened)
                    EditorSceneManager.CloseScene(scene, true);
            }

            return result;
        }
    }
}
