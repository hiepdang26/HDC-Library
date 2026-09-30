using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace HDC.Ads.Editor
{
    /// <summary>
    /// Turns HDC ads on or off for builds. While off (the default), nothing from HDCLib reaches a build:
    /// the runtime assembly needs the HDC_ADS define, the iOS plugins are disabled, and the native
    /// dependencies file is absent. Turn it on only when no other copy of the ads framework built from the
    /// same Kotlin Multiplatform project is in the project: two copies cannot be linked into one app.
    /// When the Firebase Remote Config SDK is in the project, enabling also adds HDC_FIREBASE, which builds
    /// HDCRemoteConfig.
    /// </summary>
    internal static class HDCAdsActivation
    {
        internal const string Define = "HDC_ADS";
        internal const string FirebaseDefine = "HDC_FIREBASE";

        private const string Root = "Assets/HDCLib";
        private const string DependenciesTemplate = Root + "/Editor/Templates~/HDCAdsDependencies.xml";
        private const string DependenciesFile = Root + "/Editor/HDCAdsDependencies.xml";

        private static readonly string[] IosPlugins =
        {
            Root + "/Plugins/iOS/HDCAds.xcframework",
            Root + "/Plugins/iOS/HDCAdsBridge.mm",
        };

        private static readonly NamedBuildTarget[] DefineTargets =
        {
            NamedBuildTarget.Android,
            NamedBuildTarget.iOS,
            NamedBuildTarget.Standalone,
        };

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
            foreach (string plugin in IosPlugins)
                SetIosPlugin(plugin, enabled);

            if (enabled)
            {
                File.Copy(DependenciesTemplate, DependenciesFile, true);
                AssetDatabase.ImportAsset(DependenciesFile);
            }
            else if (File.Exists(DependenciesFile))
            {
                AssetDatabase.DeleteAsset(DependenciesFile);
            }

            foreach (NamedBuildTarget target in DefineTargets)
                SetDefine(target, enabled);

            AssetDatabase.Refresh();
            Debug.Log(enabled
                ? "[HDCAds] Enabled. Builds now include the HDCAds framework and its native dependencies."
                    + (HasFirebaseRemoteConfig() ? " HDCRemoteConfig is on." : " Firebase Remote Config not found: HDCRemoteConfig is off.")
                : "[HDCAds] Disabled. Builds no longer include HDC ads.");
        }

        private static bool HasFirebaseRemoteConfig() =>
            AppDomain.CurrentDomain.GetAssemblies().Any(assembly => assembly.GetName().Name == "Firebase.RemoteConfig");

        private static void SetIosPlugin(string path, bool enabled)
        {
            if (!(AssetImporter.GetAtPath(path) is PluginImporter importer))
            {
                Debug.LogWarning($"[HDCAds] Plugin not found: {path}");
                return;
            }

            if (importer.GetCompatibleWithPlatform(BuildTarget.iOS) == enabled)
                return;

            importer.SetCompatibleWithPlatform(BuildTarget.iOS, enabled);
            importer.SaveAndReimport();
        }

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
