using System.IO;
using UnityEditor;
using UnityEngine;

namespace HDC.Ads.Editor
{
    internal static class HDCAdsSettingsAsset
    {
        private const string Folder = "Assets/HDCAds/Resources";
        internal const string DefaultPath = Folder + "/" + HDCAdsSettings.ResourceName + ".asset";

        internal static HDCAdsSettings Load()
        {
            var atDefault = AssetDatabase.LoadAssetAtPath<HDCAdsSettings>(DefaultPath);
            if (atDefault != null)
                return atDefault;
            HDCAdsSettings found = null;
            foreach (string guid in AssetDatabase.FindAssets("t:" + nameof(HDCAdsSettings)))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var settings = AssetDatabase.LoadAssetAtPath<HDCAdsSettings>(path);
                if (settings == null)
                    continue;
                if (path.EndsWith("/Resources/" + HDCAdsSettings.ResourceName + ".asset"))
                    return settings;
                if (found == null)
                    found = settings;
            }

            if (found != null)
                Debug.LogWarning($"[HDCAds] {AssetDatabase.GetAssetPath(found)} is not read by builds: move it to {DefaultPath}.", found);
            return found;
        }

        internal static HDCAdsSettings LoadOrCreate()
        {
            HDCAdsSettings settings = Load();
            return settings != null ? settings : CreateAt(DefaultPath);
        }

        internal static HDCAdsSettings CreateAt(string path)
        {
            string blocker = HDCAssetFiles.WhyNotCreate(path);
            if (blocker != null)
            {
                Debug.LogWarning("[HDCAds] " + blocker);
                return null;
            }

            HDCAssetFiles.CreateFolders(Path.GetDirectoryName(path)?.Replace('\\', '/'));
            var created = ScriptableObject.CreateInstance<HDCAdsSettings>();
            AssetDatabase.CreateAsset(created, path);
            AssetDatabase.SaveAssets();
            Debug.Log($"[HDCAds] Created {path}.", created);
            return created;
        }
    }
}
