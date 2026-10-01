using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Compilation;
using UnityEngine;

namespace HDC.Ads.Editor
{
    /// <summary>
    /// Turns HDC ads on or off for builds (HDC > Ads). While off (the default), nothing from HDCLib reaches a build:
    /// the runtime assembly needs the HDC_ADS define, the iOS postprocess adds the HDCAds framework only
    /// with it, and the native dependencies file is absent. When the Firebase Remote Config SDK is in the
    /// project, enabling also adds HDC_FIREBASE, which builds HDCRemoteConfig. While on, the dependencies file
    /// follows its template, so a library update that changes the native versions takes effect by itself.
    /// HDCLib works from any folder: under Assets, or as a package in Packages, read-only ones included.
    /// </summary>
    [InitializeOnLoad]
    internal static class HDCAdsActivation
    {
        internal const string Define = "HDC_ADS";
        internal const string FirebaseDefine = "HDC_FIREBASE";

        private const string EditorAssembly = "HDC.Ads.Editor";
        private const string RootToken = "{HDC_ROOT}";
        private const string DependenciesName = "HDCAdsDependencies.xml";

        // Where the dependencies file goes when HDCLib is a package, which may be read-only.
        private const string PackageFolder = "Assets/HDCAds";
        private const string PackageDependenciesFolder = PackageFolder + "/Editor";

        private static readonly NamedBuildTarget[] DefineTargets =
        {
            NamedBuildTarget.Android,
            NamedBuildTarget.iOS,
            NamedBuildTarget.Standalone,
        };

        static HDCAdsActivation() => EditorApplication.delayCall += RefreshDependencies;

        /// <summary>
        /// HDCLib's folder as a project path, such as "Assets/HDCLib" or "Packages/com.hdc.ads".
        /// Path.GetFullPath turns it into a disk path, package cache included.
        /// </summary>
        internal static string Root
        {
            get
            {
                string asmdef = CompilationPipeline.GetAssemblyDefinitionFilePathFromAssemblyName(EditorAssembly);
                if (string.IsNullOrEmpty(asmdef))
                    return "Assets/HDCLib";
                string editorFolder = Path.GetDirectoryName(asmdef);
                return (Path.GetDirectoryName(editorFolder) ?? "Assets/HDCLib").Replace('\\', '/');
            }
        }

        private static bool IsPackage => !Root.StartsWith("Assets/", StringComparison.Ordinal);

        private static string DependenciesFile =>
            (IsPackage ? PackageDependenciesFolder : Root + "/Editor") + "/" + DependenciesName;

        /// <summary>Whether builds for <paramref name="target"/> include HDC ads.</summary>
        internal static bool IsEnabledFor(NamedBuildTarget target) =>
            SplitDefines(PlayerSettings.GetScriptingDefineSymbols(target)).Contains(Define);

        internal static bool IsFullyEnabled => IsEnabledFor(NamedBuildTarget.iOS) && IsEnabledFor(NamedBuildTarget.Android);

        internal static bool IsPartlyEnabled => IsEnabledFor(NamedBuildTarget.iOS) || IsEnabledFor(NamedBuildTarget.Android);

        /// <summary>HDC > Ads > Enable and Disable.</summary>
        internal static void SetEnabled(bool enabled)
        {
            if (enabled)
                WriteDependencies();
            else
                DeleteDependencies();

            foreach (NamedBuildTarget target in DefineTargets)
                SetDefine(target, enabled);

            AssetDatabase.Refresh();
            Debug.Log(enabled
                ? "[HDCAds] Enabled. Builds now include the HDCAds framework and its native dependencies."
                    + (HasFirebaseRemoteConfig() ? " HDCRemoteConfig is on." : " Firebase Remote Config not found: HDCRemoteConfig is off.")
                : "[HDCAds] Disabled. Builds no longer include HDC ads.");
        }

        // The dependency manager reads every *Dependencies.xml in an Editor folder. The template's local Maven
        // repository path is filled in with where HDCLib is, which the dependency manager resolves in packages too.
        private static void WriteDependencies()
        {
            string file = DependenciesFile;
            Directory.CreateDirectory(Path.GetDirectoryName(file) ?? PackageDependenciesFolder);
            File.WriteAllText(file, ExpectedDependencies());
            AssetDatabase.ImportAsset(file);
        }

        private static string ExpectedDependencies() =>
            File.ReadAllText(Path.GetFullPath(Root + "/Editor/Templates~/" + DependenciesName)).Replace(RootToken, Root);

        // Rewrites a dependencies file an older HDCLib wrote, such as one naming an earlier Android library version.
        private static void RefreshDependencies()
        {
            if (!IsPartlyEnabled)
                return;
            string file = DependenciesFile;
            if (!File.Exists(file) || File.ReadAllText(file) != ExpectedDependencies())
                WriteDependencies();
        }

        private static void DeleteDependencies()
        {
            string file = DependenciesFile;
            if (File.Exists(file))
                AssetDatabase.DeleteAsset(file);

            if (!IsPackage)
                return;
            // The folders made for a package install, once empty.
            foreach (string folder in new[] { PackageDependenciesFolder, PackageFolder })
            {
                if (AssetDatabase.IsValidFolder(folder) && !Directory.EnumerateFileSystemEntries(folder).Any(entry => !entry.EndsWith(".meta", StringComparison.Ordinal)))
                    AssetDatabase.DeleteAsset(folder);
            }
        }

        private static bool HasFirebaseRemoteConfig() =>
            AppDomain.CurrentDomain.GetAssemblies().Any(assembly => assembly.GetName().Name == "Firebase.RemoteConfig");

        private static void SetDefine(NamedBuildTarget target, bool enabled)
        {
            List<string> defines = SplitDefines(PlayerSettings.GetScriptingDefineSymbols(target));
            bool changed = enabled ? AddMissing(defines, Define) : defines.Remove(Define);
            bool firebase = enabled && HasFirebaseRemoteConfig();
            changed |= firebase ? AddMissing(defines, FirebaseDefine) : defines.Remove(FirebaseDefine);
            if (changed)
                PlayerSettings.SetScriptingDefineSymbols(target, string.Join(";", defines));
        }

        private static bool AddMissing(List<string> defines, string define)
        {
            if (defines.Contains(define))
                return false;
            defines.Add(define);
            return true;
        }

        private static List<string> SplitDefines(string defines) =>
            new List<string>(defines.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
    }
}
