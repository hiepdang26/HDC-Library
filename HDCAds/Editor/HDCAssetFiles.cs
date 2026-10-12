using System.IO;
using UnityEditor;

namespace HDC.Ads.Editor
{
    internal static class HDCAssetFiles
    {
        internal static bool Exists(string path) =>
            File.Exists(path) || !string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(path, AssetPathToGUIDOptions.OnlyExistingAssets));

        internal static string WhyNotCreate(string path)
        {
            if (Exists(path))
                return path + " is there but Unity cannot load it right now, for example while it imports. It was left untouched; try again once the import ends.";
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                return "Unity is compiling or importing, so " + path + " was not created. Try again once it ends.";
            return null;
        }

        internal static void CreateFolders(string folder)
        {
            if (string.IsNullOrEmpty(folder) || AssetDatabase.IsValidFolder(folder))
                return;
            string parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
            CreateFolders(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }
    }
}
