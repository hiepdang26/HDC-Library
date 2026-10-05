using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Compilation;
using UnityEngine;

namespace HDC.Ads.Editor
{
    [InitializeOnLoad]
    internal static class HDCAdsActivation
    {
        internal const string Define = "HDC_ADS";
        internal const string FirebaseDefine = "HDC_FIREBASE";
        internal const string AdjustDefine = "HDC_ADJUST";

        private const string EditorAssembly = "HDC.Ads.Editor";
        private const string AdjustAssembly = "AdjustSdk.Scripts";
        private const string RootToken = "{HDC_ROOT}";
        private const string DependenciesName = "HDCAdsDependencies.xml";

        private const string PackageFolder = "Assets/HDCAds";
        private const string PackageDependenciesFolder = PackageFolder + "/Editor";

        private static readonly NamedBuildTarget[] DefineTargets =
        {
            NamedBuildTarget.Android,
            NamedBuildTarget.iOS,
            NamedBuildTarget.Standalone,
        };

        static HDCAdsActivation()
        {
            EditorApplication.delayCall += RefreshDependencies;
            EditorApplication.delayCall += SyncAdjustDefine;
            CompilationPipeline.compilationFinished += _ => EditorApplication.delayCall += SyncAdjustDefine;
        }

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

        internal static bool IsEnabledFor(NamedBuildTarget target) =>
            SplitDefines(PlayerSettings.GetScriptingDefineSymbols(target)).Contains(Define);

        internal static bool IsFullyEnabled => IsEnabledFor(NamedBuildTarget.iOS) && IsEnabledFor(NamedBuildTarget.Android);

        internal static bool IsPartlyEnabled => IsEnabledFor(NamedBuildTarget.iOS) || IsEnabledFor(NamedBuildTarget.Android);

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

        private static void WriteDependencies()
        {
            string file = DependenciesFile;
            Directory.CreateDirectory(Path.GetDirectoryName(file) ?? PackageDependenciesFolder);
            File.WriteAllText(file, ExpectedDependencies());
            AssetDatabase.ImportAsset(file);
        }

        private static string ExpectedDependencies() =>
            File.ReadAllText(Path.GetFullPath(Root + "/Editor/Templates~/" + DependenciesName)).Replace(RootToken, Root);

        private static void RefreshDependencies()
        {
            if (!IsPartlyEnabled)
                return;
            string file = DependenciesFile;
            if (!File.Exists(file) || File.ReadAllText(file) != ExpectedDependencies())
                WriteDependencies();
        }

        internal sealed class BuildRefresh : IPreprocessBuildWithReport
        {
            public int callbackOrder => int.MinValue;

            public void OnPreprocessBuild(BuildReport report)
            {
                SyncAdjustDefine();
                RefreshDependencies();
            }
        }

        private static void DeleteDependencies()
        {
            string file = DependenciesFile;
            if (File.Exists(file))
                AssetDatabase.DeleteAsset(file);

            if (!IsPackage)
                return;
            foreach (string folder in new[] { PackageDependenciesFolder, PackageFolder })
            {
                if (AssetDatabase.IsValidFolder(folder) && !Directory.EnumerateFileSystemEntries(folder).Any(entry => !entry.EndsWith(".meta", StringComparison.Ordinal)))
                    AssetDatabase.DeleteAsset(folder);
            }
        }

        internal static bool HasAdjust =>
            !string.IsNullOrEmpty(CompilationPipeline.GetAssemblyDefinitionFilePathFromAssemblyName(AdjustAssembly));

        internal static void SyncAdjustDefine()
        {
            bool adjust = HasAdjust;
            foreach (NamedBuildTarget target in DefineTargets)
            {
                List<string> defines = SplitDefines(PlayerSettings.GetScriptingDefineSymbols(target));
                if (SetAdjustDefine(defines, adjust))
                    PlayerSettings.SetScriptingDefineSymbols(target, string.Join(";", defines));
            }
        }

        internal static bool SetAdjustDefine(List<string> defines, bool adjust) =>
            adjust ? AddMissing(defines, AdjustDefine) : defines.Remove(AdjustDefine);

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
