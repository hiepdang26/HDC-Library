using System.Collections.Generic;
using HDC.Ads;
using HDC.Ads.DebugUI;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HDC.Tool.Editor
{
    internal sealed class HDCBuildSceneProcessor : IProcessSceneWithReport
    {
        internal static HDCBuildRules ActiveRules { get; set; }

        public int callbackOrder => 0;

        public void OnProcessScene(Scene scene, BuildReport report)
        {
            if (report == null || ActiveRules == null)
                return;
            Apply(scene.GetRootGameObjects(), ActiveRules);
        }

        internal static void Apply(IEnumerable<GameObject> roots, HDCBuildRules rules)
        {
            foreach (GameObject root in roots)
            {
                foreach (HDCAdsSetup setup in root.GetComponentsInChildren<HDCAdsSetup>(true))
                {
                    var serialized = new SerializedObject(setup);
                    serialized.FindProperty("debugLog").boolValue = rules.debugLog;
                    serialized.FindProperty("googleTestAds").boolValue = rules.googleTestAds;
                    serialized.FindProperty("googleTestAdUnits").boolValue = rules.googleTestAdUnits;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }

                foreach (HDCAdjust adjust in root.GetComponentsInChildren<HDCAdjust>(true))
                {
                    var serialized = new SerializedObject(adjust);
                    serialized.FindProperty("environment").intValue = (int)rules.adjustEnvironment;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }

                foreach (HDCAdsDebugPanel panel in root.GetComponentsInChildren<HDCAdsDebugPanel>(true))
                    panel.gameObject.SetActive(rules.debugPanel);
            }
        }
    }
}
