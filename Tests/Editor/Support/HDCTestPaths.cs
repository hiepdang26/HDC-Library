using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace HDC.Ads.Tests
{
    /// <summary>Finds the library's files through its assemblies, wherever the project keeps HDCLib.</summary>
    internal static class HDCTestPaths
    {
        /// <summary>The asset folder holding an assembly's .asmdef, such as "Assets/HDCLib/Runtime".</summary>
        internal static string AssemblyFolder(string assembly)
        {
            string asmdef = CompilationPipeline.GetAssemblyDefinitionFilePathFromAssemblyName(assembly);
            Assert.IsNotNull(asmdef, "no .asmdef for " + assembly);
            return Path.GetDirectoryName(asmdef).Replace('\\', '/');
        }

        /// <summary>The library's root folder, the one holding Runtime.</summary>
        internal static string Root => Path.GetDirectoryName(AssemblyFolder("HDC.Ads")).Replace('\\', '/');

        /// <summary>This assembly's folder, which holds the approved public API.</summary>
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
