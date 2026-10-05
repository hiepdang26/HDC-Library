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
    internal static class HDCAdsIosPostprocess
    {
        private const string MinimumIosVersion = "15.0";
        private const string ComposeResourcesPhase = "HDC Copy Compose Resources";
        private const string EmbedDynamicPodsPhase = "HDC Embed Dynamic Pods";

        private const string FrameworkName = "HDCAds.xcframework";
        private const string BridgeName = "HDCAdsBridge.mm";

        private const string FrameworkPath = "Frameworks/HDCAds/" + FrameworkName;
        private const string BridgePath = "Libraries/HDCAds/" + BridgeName;

        private const string AllLoadLookupFlag = "-Wl,-undefined,dynamic_lookup,-all_load";
        private const string LookupFlag = "-Wl,-undefined,dynamic_lookup";
        private static readonly string[] UnityLibraries = { "libil2cpp.a", "libGameAssembly.a", "baselib.a" };

        private static bool IsEnabled => HDCAdsActivation.IsEnabledFor(NamedBuildTarget.iOS);

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
            RemoveOwnPhases(projectPath);
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

            if (!IsInProjectElsewhere(projectText, FrameworkName, FrameworkPath))
            {
                CopyDirectory(Path.Combine(pluginsFolder, FrameworkName), Path.Combine(buildPath, FrameworkPath));
                if (project.FindFileGuidByProjectPath(FrameworkPath) == null)
                {
                    string frameworkGuid = project.AddFile(FrameworkPath, FrameworkPath, PBXSourceTree.Source);
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

        [PostProcessBuild(101)]
        private static void ApplyLocalBuildSettings(BuildTarget target, string buildPath)
        {
            if (target != BuildTarget.iOS || !HDCIosLocalBuild.IsSet)
                return;

            bool simulator = PlayerSettings.iOS.sdkVersion == iOSSdkVersion.SimulatorSDK;
            string projectPath = PBXProject.GetPBXProjectPath(buildPath);
            File.WriteAllText(projectPath, WithLocalBuild(File.ReadAllText(projectPath), HDCIosLocalBuild.TeamId,
                HDCIosLocalBuild.BundleId, HDCIosLocalBuild.MacRun, simulator));
            Debug.Log("[HDCAds] iOS local build settings of this computer: " + HDCIosLocalBuild.Summary);
            if (simulator && HDCIosLocalBuild.MacRun == HDCMacRun.On)
                Debug.LogWarning("[HDCAds] Run on My Mac needs a Device SDK export; this export uses the Simulator SDK, so it is skipped.");
        }

        internal static string WithLocalBuild(string projectText, string teamId, string bundleId, HDCMacRun macRun, bool simulator)
        {
            var project = new PBXProject();
            project.ReadFromString(projectText);
            string mainTarget = project.GetUnityMainTargetGuid();
            string frameworkTarget = project.GetUnityFrameworkTargetGuid();

            if (!string.IsNullOrEmpty(teamId))
            {
                foreach (string target in new[] { mainTarget, frameworkTarget })
                {
                    if (string.IsNullOrEmpty(target))
                        continue;
                    project.SetBuildProperty(target, "DEVELOPMENT_TEAM", teamId);
                    project.SetBuildProperty(target, "CODE_SIGN_STYLE", "Automatic");
                    project.SetBuildProperty(target, "CODE_SIGN_IDENTITY", "Apple Development");
                    project.SetBuildProperty(target, "PROVISIONING_PROFILE_SPECIFIER", "");
                    project.SetBuildProperty(target, "PROVISIONING_PROFILE", "");
                }
            }

            if (!string.IsNullOrEmpty(bundleId) && !string.IsNullOrEmpty(mainTarget))
                project.SetBuildProperty(mainTarget, "PRODUCT_BUNDLE_IDENTIFIER", bundleId);

            if (macRun == HDCMacRun.Off || (macRun == HDCMacRun.On && !simulator))
            {
                foreach (string target in new[] { mainTarget, frameworkTarget, project.TargetGuidByName("GameAssembly") })
                {
                    if (string.IsNullOrEmpty(target))
                        continue;
                    project.SetBuildProperty(target, "SUPPORTS_MAC_DESIGNED_FOR_IPHONE_IPAD", macRun == HDCMacRun.On ? "YES" : "NO");
                    if (macRun == HDCMacRun.On)
                        project.SetBuildProperty(target, "SUPPORTS_MACCATALYST", "NO");
                }
            }

            return project.WriteToString();
        }

        private static void RemoveOwnPhases(string projectPath)
        {
            if (!File.Exists(projectPath))
                return;
            string text = File.ReadAllText(projectPath);
            string cleaned = WithoutPhases(text, ComposeResourcesPhase, EmbedDynamicPodsPhase);
            if (cleaned != text)
                File.WriteAllText(projectPath, cleaned);
        }

        internal static string WithoutPhases(string projectText, params string[] names)
        {
            var lines = new List<string>(projectText.Split('\n'));
            var removed = new HashSet<string>();
            for (int i = 0; i + 1 < lines.Count; i++)
            {
                if (!lines[i].TrimEnd().EndsWith("= {", StringComparison.Ordinal) || lines[i + 1].Trim() != "isa = PBXShellScriptBuildPhase;")
                    continue;

                int end = i + 1;
                string name = null;
                while (end < lines.Count && lines[end].Trim() != "};")
                {
                    string line = lines[end].Trim();
                    if (line.StartsWith("name = ", StringComparison.Ordinal))
                        name = line.Substring("name = ".Length).TrimEnd(';').Trim('"');
                    end++;
                }

                if (end == lines.Count || name == null || Array.IndexOf(names, name) < 0)
                    continue;
                removed.Add(lines[i].Trim().Split(' ')[0]);
                lines.RemoveRange(i, end - i + 1);
                i--;
            }

            if (removed.Count == 0)
                return projectText;
            lines.RemoveAll(line => line.Trim().EndsWith(",", StringComparison.Ordinal) && removed.Contains(line.Trim().Split(' ', ',')[0]));
            return string.Join("\n", lines);
        }

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

        private const string EmbedDynamicPodsScript =
@"set -e
SOURCE_ROOT=""${PODS_XCFRAMEWORKS_BUILD_DIR:-${BUILT_PRODUCTS_DIR}/XCFrameworkIntermediates}""
DESTINATION=""${TARGET_BUILD_DIR}/${FRAMEWORKS_FOLDER_PATH}""
if [ ! -d ""${SOURCE_ROOT}"" ]; then exit 0; fi
find ""${SOURCE_ROOT}"" -maxdepth 3 -type d -name ""*.framework"" | while IFS= read -r SOURCE; do
    NAME=""$(basename ""${SOURCE}"")""
    BINARY=""${SOURCE}/${NAME%.framework}""
    if [ ! -f ""${BINARY}"" ] || [ -d ""${DESTINATION}/${NAME}"" ]; then continue; fi
    if ! /usr/bin/file -b ""${BINARY}"" | grep -q ""dynamically linked shared library""; then continue; fi
    echo ""Embedding ${NAME}""
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
