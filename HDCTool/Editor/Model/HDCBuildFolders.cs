using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace HDC.Tool.Editor
{
    internal static class HDCBuildFolders
    {
        internal const string AndroidFolderName = "Android";
        internal const string SimulatorFolderName = "Simulator";
        internal const string BuildFolderName = "Build";

        internal static string ProjectRoot => Path.GetFullPath(Path.GetDirectoryName(Application.dataPath) ?? ".");

        internal static string Base
        {
            get
            {
                string chosen = HDCBuildState.BuildFolder;
                return chosen.Length > 0 && !IsInside(chosen, ProjectRoot) ? Path.GetFullPath(chosen) : DefaultBase(ProjectRoot);
            }
        }

        internal static bool IsDefault => HDCBuildState.BuildFolder.Length == 0 || IsInside(HDCBuildState.BuildFolder, ProjectRoot);

        internal static string DefaultBase(string projectRoot)
        {
            string root = Trim(Path.GetFullPath(projectRoot));
            string workspace = Path.GetDirectoryName(Path.GetDirectoryName(root) ?? root) ?? root;
            return Path.Combine(workspace, BuildFolderName, Path.GetFileName(root));
        }

        internal static string For(HDCBuildPlatform platform) => For(Base, platform);

        internal static string For(string baseFolder, HDCBuildPlatform platform) =>
            platform == HDCBuildPlatform.Ios ? baseFolder : Path.Combine(baseFolder, AndroidFolderName);

        internal static string IosExport(HDCAppleTarget target) => IosExport(Base, target);

        internal static string IosExport(string baseFolder, HDCAppleTarget target) =>
            target == HDCAppleTarget.Simulator ? Path.Combine(baseFolder, SimulatorFolderName) : baseFolder;

        internal static string Choose(string folder)
        {
            if (string.IsNullOrWhiteSpace(folder))
                return "No folder was chosen.";
            if (IsInside(folder, ProjectRoot))
                return "Builds may not go inside the Unity project. Choose a folder outside it, like " + Display(DefaultBase(ProjectRoot)) + ".";
            HDCBuildState.BuildFolder = Path.GetFullPath(folder);
            return null;
        }

        internal static void UseDefault() => HDCBuildState.BuildFolder = "";

        internal static bool IsInside(string folder, string projectRoot)
        {
            if (string.IsNullOrWhiteSpace(folder) || string.IsNullOrWhiteSpace(projectRoot))
                return false;
            string path = Trim(Path.GetFullPath(folder)).Replace('\\', '/');
            string root = Trim(Path.GetFullPath(projectRoot)).Replace('\\', '/');
            return string.Equals(path, root, StringComparison.OrdinalIgnoreCase) || path.StartsWith(root + "/", StringComparison.OrdinalIgnoreCase);
        }

        internal static string Display(string path)
        {
            string home = Trim(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
            return home.Length > 0 && (path ?? "").StartsWith(home + "/", StringComparison.Ordinal) ? "~" + path.Substring(home.Length) : path ?? "";
        }

        internal static void Open(string folder)
        {
            if (Directory.Exists(folder))
                EditorUtility.OpenWithDefaultApp(folder);
        }

        private static string Trim(string path) => path.Length > 1 ? path.TrimEnd('/', '\\') : path;
    }
}
