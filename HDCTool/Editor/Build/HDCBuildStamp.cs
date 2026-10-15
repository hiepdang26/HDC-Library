using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace HDC.Tool.Editor
{
    internal static class HDCBuildStamp
    {
        internal const string Folder = HDCBuildSettings.AssetFolder + "/Resources";
        internal const string AssetPath = Folder + "/HDCBuildRules.txt";
        internal const string Guid = "297a1a11c1264ae4aa364f4ef207c475";

        private const string Meta = "fileFormatVersion: 2\nguid: " + Guid + "\nTextScriptImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n";

        internal static string Text(HDCBuildRules rules) => JsonUtility.ToJson(rules, true);

        internal static List<string> Write(HDCBuildRules rules)
        {
            List<string> made = MakeFolders(Folder);
            File.WriteAllText(AssetPath, Text(rules));
            File.WriteAllText(AssetPath + ".meta", Meta);
            AssetDatabase.ImportAsset(AssetPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            return made;
        }

        internal static void Remove(List<string> made)
        {
            if (File.Exists(AssetPath))
                AssetDatabase.DeleteAsset(AssetPath);
            if (made == null)
                return;
            for (int i = made.Count - 1; i >= 0; i--)
            {
                if (AssetDatabase.IsValidFolder(made[i]) && Directory.GetFileSystemEntries(made[i]).Length == 0)
                    AssetDatabase.DeleteAsset(made[i]);
            }
        }

        private static List<string> MakeFolders(string folder)
        {
            var made = new List<string>();
            string current = "Assets";
            foreach (string part in folder.Substring("Assets/".Length).Split('/'))
            {
                string next = current + "/" + part;
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, part);
                    made.Add(next);
                }

                current = next;
            }

            return made;
        }
    }
}
