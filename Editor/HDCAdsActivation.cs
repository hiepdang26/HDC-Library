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
    /// Turns HDC ads on or off for builds. While off (the default), nothing from HDCLib reaches a build:
    /// the runtime assembly needs the HDC_ADS define, the iOS postprocess adds the HDCAds framework only
    /// with it, and the native dependencies file is absent. Turn it on only when no other copy of the ads
    /// framework built from the same Kotlin Multiplatform project is in the project: two copies cannot be
    /// linked into one app. When the Firebase Remote Config SDK is in the project, enabling also adds
    /// HDC_FIREBASE, which builds HDCRemoteConfig.
    /// HDCLib works from any folder: under Assets, or as a package in Packages, read-only ones included.
    /// </summary>
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

        [MenuItem("Tools/HDC Ads/Enable")]
        private static void Enable() => SetEnabled(true);

        [MenuItem("Tools/HDC Ads/Enable", true)]
        private static bool CanEnable() => !IsEnabledFor(NamedBuildTarget.iOS) || !IsEnabledFor(NamedBuildTarget.Android);

        [MenuItem("Tools/HDC Ads/Disable")]
        private static void Disable() => SetEnabled(false);

        [MenuItem("Tools/HDC Ads/Disable", true)]
        private static bool CanDisable() => IsEnabledFor(NamedBuildTarget.iOS) || IsEnabledFor(NamedBuildTarget.Android);

        private static void SetEnabled(bool enabled)
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
            string template = Path.GetFullPath(Root + "/Editor/Templates~/" + DependenciesName);
            string file = DependenciesFile;
            Directory.CreateDirectory(Path.GetDirectoryName(file) ?? PackageDependenciesFolder);
            File.WriteAllText(file, File.ReadAllText(template).Replace(RootToken, Root));
            AssetDatabase.ImportAsset(file);
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
