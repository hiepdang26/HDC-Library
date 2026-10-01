#if UNITY_IOS
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEngine;

namespace HDC.Ads.Editor
{
    /// <summary>
    /// Puts the HDCAds framework and its Unity bridge into the Xcode project, with the settings they need,
    /// while HDC ads are enabled for iOS. The plugin importers stay off: adding the files here works the same
    /// in every Unity version and from read-only packages. Another framework built from the same KMP project is
    /// left out (see <see cref="HDCAdsIosConflicts"/>). A build without HDC ads over an earlier export (Append)
    /// takes the HDC files out again.
    /// </summary>
    internal static class HDCAdsIosPostprocess
    {
        private const string MinimumIosVersion = "15.0";
        private const string ComposeResourcesPhase = "HDC Copy Compose Resources";
        private const string EmbedDynamicPodsPhase = "HDC Embed Dynamic Pods";

        private const string FrameworkName = "HDCAds.xcframework";
        private const string BridgeName = "HDCAdsBridge.mm";

        // Paths inside the Xcode project, next to where Unity puts plugins.
        private const string FrameworkPath = "Frameworks/HDCAds/" + FrameworkName;
        private const string BridgePath = "Libraries/HDCAds/" + BridgeName;

        // Unity links simulator builds with -all_load, so that its engine, a dynamic library there, finds IL2CPP
        // by name at run time. -all_load also loads every member of the HDCAds static framework, whose Skia
        // libraries repeat objects, and the link fails on duplicate symbols. Force-loading Unity's own libraries
        // keeps what the engine needs without that.
        private const string AllLoadLookupFlag = "-Wl,-undefined,dynamic_lookup,-all_load";
        private const string LookupFlag = "-Wl,-undefined,dynamic_lookup";
        private static readonly string[] UnityLibraries = { "libil2cpp.a", "libGameAssembly.a", "baselib.a" };

        private static bool IsEnabled => HDCAdsActivation.IsEnabledFor(NamedBuildTarget.iOS);

        // The dependency resolver writes the Podfile at priority 40 and runs pod install at 50.
        [PostProcessBuild(45)]
        private static void RaisePodfilePlatform(BuildTarget target, string buildPath)
        {
            string podfile = Path.Combine(buildPath, "Podfile");
            if (target != BuildTarget.iOS || !IsEnabled || !File.Exists(podfile))
                return;

            string[] lines = File.ReadAllLines(podfile);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (!line.StartsWith("platform :ios", StringComparison.Ordinal))
                    continue;

                string version = line.Split('\'', '"').Length > 1 ? line.Split('\'', '"')[1] : string.Empty;
                if (!IsAtLeast(version, MinimumIosVersion))
                    lines[i] = $"platform :ios, '{MinimumIosVersion}'";
                break;
            }

            File.WriteAllLines(podfile, lines);
        }

        [PostProcessBuild(100)]
        private static void ConfigureXcodeProject(BuildTarget target, string buildPath)
        {
            if (target != BuildTarget.iOS)
                return;

            string projectPath = PBXProject.GetPBXProjectPath(buildPath);
            var project = new PBXProject();
            project.ReadFromFile(projectPath);

            if (!IsEnabled)
            {
                if (RemoveNativeFiles(project, buildPath))
                    project.WriteToFile(projectPath);
                return;
            }

            string mainTarget = project.GetUnityMainTargetGuid();
            string frameworkTarget = project.GetUnityFrameworkTargetGuid();
            string pluginsFolder = Path.GetFullPath(HDCAdsActivation.Root + "/Plugins/iOS");
            string projectText = File.ReadAllText(projectPath);

            // Skipped when the plugin importer was turned on by hand, so Unity already added the file.
            if (!IsInProjectElsewhere(projectText, FrameworkName, FrameworkPath))
            {
                CopyDirectory(Path.Combine(pluginsFolder, FrameworkName), Path.Combine(buildPath, FrameworkPath));
                if (project.FindFileGuidByProjectPath(FrameworkPath) == null)
                {
                    string frameworkGuid = project.AddFile(FrameworkPath, FrameworkPath, PBXSourceTree.Source);
                    // Linked, not embedded: the framework is static.
                    project.AddFileToBuildSection(frameworkTarget, project.GetFrameworksBuildPhaseByTarget(frameworkTarget), frameworkGuid);
                }
            }

            if (!IsInProjectElsewhere(projectText, BridgeName, BridgePath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(buildPath, BridgePath)) ?? buildPath);
                File.Copy(Path.Combine(pluginsFolder, BridgeName), Path.Combine(buildPath, BridgePath), true);
                string bridgeGuid = project.FindFileGuidByProjectPath(BridgePath) ?? project.AddFile(BridgePath, BridgePath, PBXSourceTree.Source);
                project.AddFileToBuild(frameworkTarget, bridgeGuid);
            }

            HDCAdsIosConflicts.LeaveOut(project, frameworkTarget, buildPath, FrameworkPath);
            ReplaceAllLoad(project, frameworkTarget, projectText);
            RaiseDeploymentTarget(project, mainTarget);
            RaiseDeploymentTarget(project, frameworkTarget);
            AddShellPhase(project, mainTarget, ComposeResourcesPhase, ComposeResourcesScript);
            AddShellPhase(project, mainTarget, EmbedDynamicPodsPhase, EmbedDynamicPodsScript);
            project.WriteToFile(projectPath);

            string text = File.ReadAllText(projectPath);
            string fixedText = MarkAsXcframework(text);
            if (fixedText != text)
                File.WriteAllText(projectPath, fixedText);

            AllowHighFrameRates(buildPath);
        }

        // The native ad views are Compose Multiplatform views. Compose aborts the app when it first shows one and
        // Info.plist lacks CADisableMinimumFrameDurationOnPhone, which Unity writes as false while Player Settings >
        // Enable ProMotion is off.
        private static void AllowHighFrameRates(string buildPath)
        {
            string plistPath = Path.Combine(buildPath, "Info.plist");
            if (!File.Exists(plistPath))
                return;

            var plist = new PlistDocument();
            plist.ReadFromFile(plistPath);
            if (plist.root["CADisableMinimumFrameDurationOnPhone"] is PlistElementBoolean allowed && allowed.value)
                return;
            plist.root.SetBoolean("CADisableMinimumFrameDurationOnPhone", true);
            plist.WriteToFile(plistPath);
        }

        private static void ReplaceAllLoad(PBXProject project, string targetGuid, string projectText)
        {
            if (!projectText.Contains(AllLoadLookupFlag))
                return;

            var flags = new List<string> { LookupFlag };
            foreach (string library in UnityLibraries)
            {
                string path = LibraryPath(projectText, library);
                if (path != null)
                    flags.Add("-Wl,-force_load," + path);
            }

            project.UpdateBuildProperty(targetGuid, "OTHER_LDFLAGS", flags, new[] { AllLoadLookupFlag });
        }

        // A library's path as the linker sees it, from its file reference; null when the project has no such file.
        private static string LibraryPath(string projectText, string fileName)
        {
            foreach (string line in projectText.Split('\n'))
            {
                if (!line.Contains("isa = PBXFileReference"))
                    continue;
                string path = ReadSetting(line, "path");
                if (path == null || Path.GetFileName(path) != fileName)
                    continue;

                switch (ReadSetting(line, "sourceTree"))
                {
                    case "SOURCE_ROOT":
                        return "$(SRCROOT)/" + path;
                    case "BUILT_PRODUCTS_DIR":
                        return "$(BUILT_PRODUCTS_DIR)/" + path;
                }
            }

            return null;
        }

        private static bool RemoveNativeFiles(PBXProject project, string buildPath)
        {
            bool removed = false;
            foreach (string path in new[] { FrameworkPath, BridgePath })
            {
                string guid = project.FindFileGuidByProjectPath(path);
                if (guid != null)
                {
                    project.RemoveFile(guid);
                    removed = true;
                }
            }

            string frameworkCopy = Path.Combine(buildPath, FrameworkPath);
            if (Directory.Exists(frameworkCopy))
                Directory.Delete(frameworkCopy, true);
            string bridgeCopy = Path.Combine(buildPath, BridgePath);
            if (File.Exists(bridgeCopy))
                File.Delete(bridgeCopy);
            return HDCAdsIosConflicts.RemoveStandIns(project, buildPath) || removed;
        }

        // Whether a file reference other than this postprocess's points at the file, as when its plugin
        // importer was turned on by hand and Unity added it.
        private static bool IsInProjectElsewhere(string projectText, string fileName, string ownPath)
        {
            foreach (string line in projectText.Split('\n'))
            {
                if (!line.Contains("isa = PBXFileReference"))
                    continue;
                string path = ReadSetting(line, "path");
                if (path != null && path.EndsWith(fileName, StringComparison.Ordinal) && path != ownPath)
                    return true;
            }

            return false;
        }

        // Older Xcode project APIs do not know the xcframework file type, and Xcode then does not link it.
        private static string MarkAsXcframework(string text)
        {
            string[] lines = text.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (!line.Contains("isa = PBXFileReference") || ReadSetting(line, "path") != FrameworkPath)
                    continue;

                string fileType = ReadSetting(line, "lastKnownFileType");
                if (fileType == "wrapper.xcframework")
                    continue;
                lines[i] = fileType != null
                    ? line.Replace("lastKnownFileType = " + fileType + ";", "lastKnownFileType = wrapper.xcframework;")
                    : line.Replace("isa = PBXFileReference;", "isa = PBXFileReference; lastKnownFileType = wrapper.xcframework;");
            }

            return string.Join("\n", lines);
        }

        // Reads "key = value;" from a one-line project entry, without quotes.
        private static string ReadSetting(string line, string key)
        {
            string marker = " " + key + " = ";
            int start = line.IndexOf(marker, StringComparison.Ordinal);
            if (start < 0)
                return null;
            start += marker.Length;
            int end = line.IndexOf(';', start);
            return end < 0 ? null : line.Substring(start, end - start).Trim('"');
        }

        private static void CopyDirectory(string source, string destination)
        {
            if (Directory.Exists(destination))
                Directory.Delete(destination, true);
            Directory.CreateDirectory(destination);
            foreach (string directory in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
                Directory.CreateDirectory(directory.Replace(source, destination));
            foreach (string file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
            {
                if (!file.EndsWith(".meta", StringComparison.Ordinal))
                    File.Copy(file, file.Replace(source, destination), true);
            }
        }

        private static void RaiseDeploymentTarget(PBXProject project, string targetGuid)
        {
            if (string.IsNullOrEmpty(targetGuid))
                return;

            string current = project.GetBuildPropertyForAnyConfig(targetGuid, "IPHONEOS_DEPLOYMENT_TARGET");
            if (!IsAtLeast(current, MinimumIosVersion))
                project.SetBuildProperty(targetGuid, "IPHONEOS_DEPLOYMENT_TARGET", MinimumIosVersion);
        }

        private static void AddShellPhase(PBXProject project, string targetGuid, string name, string script)
        {
            if (string.IsNullOrEmpty(targetGuid))
                return;
            if (project.GetShellScriptBuildPhaseForTarget(targetGuid, name, "/bin/sh", script) != null)
                return;

            project.AddShellScriptBuildPhase(targetGuid, name, "/bin/sh", script);
        }

        private static bool IsAtLeast(string version, string minimum) =>
            Version.TryParse(Normalize(version), out Version parsed) &&
            parsed >= Version.Parse(Normalize(minimum));

        private static string Normalize(string version)
        {
            string trimmed = (version ?? string.Empty).Trim();
            return trimmed.Contains(".") ? trimmed : trimmed + ".0";
        }

        // Compose Multiplatform reads its resources from the app bundle, which a static framework does not reach.
        private const string ComposeResourcesScript =
@"set -e
XCFRAMEWORK=""${PROJECT_DIR}/" + FrameworkPath + @"""
if [ ! -d ""${XCFRAMEWORK}"" ]; then
    XCFRAMEWORK=""$(find ""${PROJECT_DIR}/Frameworks"" ""${PROJECT_DIR}/Libraries"" -type d -name " + FrameworkName + @" -prune 2>/dev/null | head -n 1)""
fi
if [ -z ""${XCFRAMEWORK}"" ]; then echo ""warning: " + FrameworkName + @" not found, Compose resources not copied""; exit 0; fi
if [[ ""${PLATFORM_NAME}"" == *simulator* ]]; then SLICE=""ios-arm64-simulator""; else SLICE=""ios-arm64""; fi
SOURCE=""${XCFRAMEWORK}/${SLICE}/HDCAds.framework/composeResources""
DESTINATION=""${TARGET_BUILD_DIR}/${UNLOCALIZED_RESOURCES_FOLDER_PATH}/compose-resources/composeResources""
if [ ! -d ""${SOURCE}"" ]; then echo ""warning: ${SOURCE} not found, Compose resources not copied""; exit 0; fi
mkdir -p ""${DESTINATION}""
rsync -a --delete ""${SOURCE}/"" ""${DESTINATION}/""
";

        // Dynamic pods linked only to UnityFramework are not embedded in the app unless something embeds them.
        private const string EmbedDynamicPodsScript =
@"set -e
SOURCE_ROOT=""${PODS_XCFRAMEWORKS_BUILD_DIR:-${BUILT_PRODUCTS_DIR}/XCFrameworkIntermediates}""
DESTINATION=""${TARGET_BUILD_DIR}/${FRAMEWORKS_FOLDER_PATH}""
for FRAMEWORK in ""FBAudienceNetwork/FBAudienceNetwork.framework""; do
    SOURCE=""${SOURCE_ROOT}/${FRAMEWORK}""
    NAME=""$(basename ""${FRAMEWORK}"")""
    if [ ! -d ""${SOURCE}"" ] || [ -d ""${DESTINATION}/${NAME}"" ]; then continue; fi
    mkdir -p ""${DESTINATION}""
    rsync -a --delete --exclude Headers --exclude PrivateHeaders --exclude Modules ""${SOURCE}"" ""${DESTINATION}/""
    if [ ""${CODE_SIGNING_ALLOWED:-NO}"" = ""YES"" ] && [ -n ""${EXPANDED_CODE_SIGN_IDENTITY:-}"" ]; then
        /usr/bin/codesign --force --sign ""${EXPANDED_CODE_SIGN_IDENTITY}"" --preserve-metadata=identifier,entitlements,flags --timestamp=none ""${DESTINATION}/${NAME}""
    fi
done
";
    }
}
#endif
