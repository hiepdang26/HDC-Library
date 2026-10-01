using UnityEditor;
using UnityEngine;

namespace HDC.Ads.Editor
{
    /// <summary>Finds the project's HDCAdsSettings asset, or creates it in Assets/HDCAds/Resources.</summary>
    internal static class HDCAdsSettingsAsset
    {
        private const string Folder = "Assets/HDCAds/Resources";
        private const string DefaultPath = Folder + "/" + HDCAdsSettings.ResourceName + ".asset";

        internal static HDCAdsSettings LoadOrCreate()
        {
            HDCAdsSettings found = null;
            foreach (string guid in AssetDatabase.FindAssets("t:" + nameof(HDCAdsSettings)))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var settings = AssetDatabase.LoadAssetAtPath<HDCAdsSettings>(path);
                if (settings == null)
                    continue;
                // Builds only read the asset from a Resources folder, under its own name.
                if (path.EndsWith("/Resources/" + HDCAdsSettings.ResourceName + ".asset"))
                    return settings;
                if (found == null)
                    found = settings;
            }

            if (found != null)
            {
                Debug.LogWarning($"[HDCAds] {AssetDatabase.GetAssetPath(found)} is not read by builds: move it to {DefaultPath}.", found);
                return found;
            }

            CreateFolders();
            var created = ScriptableObject.CreateInstance<HDCAdsSettings>();
            AssetDatabase.CreateAsset(created, DefaultPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[HDCAds] Created {DefaultPath}.", created);
            return created;
        }

        private static void CreateFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/HDCAds"))
                AssetDatabase.CreateFolder("Assets", "HDCAds");
            if (!AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.CreateFolder("Assets/HDCAds", "Resources");
        }
    }
}
