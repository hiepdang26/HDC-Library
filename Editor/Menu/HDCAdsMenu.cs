using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HDC.Ads.Editor
{
    internal static class HDCAdsMenu
    {
        private const string Root = "HDC/";

        private const string SetupPrefabGuid = "10e1a444d69fc4f72a856e51d2def004";
        private const string AdjustPrefabGuid = "13fb02986b6ae448a95246f7eb8bf7d5";

        [MenuItem(Root + "Ads/Enable", false, 1)]
        private static void Enable() => HDCAdsActivation.SetEnabled(true);

        [MenuItem(Root + "Ads/Enable", true)]
        private static bool CanEnable() => !HDCAdsActivation.IsFullyEnabled;

        [MenuItem(Root + "Ads/Disable", false, 2)]
        private static void Disable() => HDCAdsActivation.SetEnabled(false);

        [MenuItem(Root + "Ads/Disable", true)]
        private static bool CanDisable() => HDCAdsActivation.IsPartlyEnabled;

        [MenuItem(Root + "Edit configs/Ads configs", false, 20)]
        private static void EditAdsConfigs() => HDCAdsConfigWindow.Open(HDCAdsConfigWindow.AdsAndroidPage);

        [MenuItem(Root + "Edit configs/Ad core Android configs", false, 21)]
        private static void EditCoreAndroidConfigs() => HDCAdsConfigWindow.Open(HDCAdsConfigWindow.CoreAndroidPage);

        [MenuItem(Root + "Edit configs/Ad core iOS configs", false, 22)]
        private static void EditCoreIosConfigs() => HDCAdsConfigWindow.Open(HDCAdsConfigWindow.CoreIosPage);

        [MenuItem(Root + "Settings asset", false, 40)]
        private static void SelectSettings()
        {
            HDCAdsSettings settings = HDCAdsSettingsAsset.LoadOrCreate();
            Selection.activeObject = settings;
            EditorGUIUtility.PingObject(settings);
        }

        [MenuItem(Root + "Setup/Add to open scene", false, 50)]
        private static void AddSetup() => AddPrefab(SetupPrefabGuid, "HDCAdsSetup", "Add HDC ads setup");

        [MenuItem(Root + "Adjust/Add to open scene", false, 60)]
        private static void AddAdjust()
        {
#if UNITY_2023_1_OR_NEWER
            HDCAdjust existing = Object.FindAnyObjectByType<HDCAdjust>();
#else
            HDCAdjust existing = Object.FindObjectOfType<HDCAdjust>();
#endif
            if (existing != null)
            {
                Selection.activeGameObject = existing.gameObject;
                EditorGUIUtility.PingObject(existing.gameObject);
                Debug.Log("[HDCAdjust] The open scene already has HDCAdjust.");
                return;
            }

            AddPrefab(AdjustPrefabGuid, "HDCAdjust", "Add HDC Adjust");
        }

        private static void AddPrefab(string guid, string name, string undo)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
            if (prefab == null)
            {
                Debug.LogError("[HDCAds] The " + name + " prefab is missing from HDCLib.");
                return;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Undo.RegisterCreatedObjectUndo(instance, undo);
            Selection.activeGameObject = instance;
            EditorSceneManager.MarkSceneDirty(instance.scene);
        }
    }
}
