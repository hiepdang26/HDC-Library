using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace HDC.Ads.Tests
{
    internal static class HDCTestPaths
    {
        internal static string AssemblyFolder(string assembly)
        {
            string asmdef = CompilationPipeline.GetAssemblyDefinitionFilePathFromAssemblyName(assembly);
            Assert.IsNotNull(asmdef, "no .asmdef for " + assembly);
            return Path.GetDirectoryName(asmdef).Replace('\\', '/');
        }

        internal static string Root => Path.GetDirectoryName(AssemblyFolder("HDC.Ads")).Replace('\\', '/');

        internal static string Tests => AssemblyFolder("HDC.Ads.Tests");

        internal static GameObject DebugPanelPrefab()
        {
            string path = AssemblyFolder("HDC.Ads.Debug") + "/HDCAdsDebugPanel.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.IsNotNull(prefab, "no debug panel prefab at " + path);
            return prefab;
        }
    }
}
