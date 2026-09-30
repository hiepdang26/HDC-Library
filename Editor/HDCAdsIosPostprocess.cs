#if UNITY_IOS
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEngine;

namespace HDC.Ads.Editor
{
    /// <summary>Xcode settings the HDCAds framework needs. Runs only while HDC ads are enabled for iOS.</summary>
    internal static class HDCAdsIosPostprocess
    {
        private const string MinimumIosVersion = "15.0";
        private const string ComposeResourcesPhase = "HDC Copy Compose Resources";
        private const string EmbedDynamicPodsPhase = "HDC Embed Dynamic Pods";

        // -all_load pulls every member of every static library into the link, so symbols the HDCAds framework
        // shares with other static libraries collide.
        private const string AllLoadLookupFlag = "-Wl,-undefined,dynamic_lookup,-all_load";
        private const string LookupFlag = "-Wl,-undefined,dynamic_lookup";

        private static bool IsEnabled(BuildTarget target) =>
            target == BuildTarget.iOS && HDCAdsActivation.IsEnabledFor(NamedBuildTarget.iOS);

        // The dependency resolver writes the Podfile at priority 40 and runs pod install at 50.
        [PostProcessBuild(45)]
        private static void RaisePodfilePlatform(BuildTarget target, string buildPath)
        {
            string podfile = Path.Combine(buildPath, "Podfile");
            if (!IsEnabled(target) || !File.Exists(podfile))
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
            if (!IsEnabled(target))
                return;

            string projectPath = PBXProject.GetPBXProjectPath(buildPath);
            var project = new PBXProject();
            project.ReadFromFile(projectPath);

            string mainTarget = project.GetUnityMainTargetGuid();
            string frameworkTarget = project.GetUnityFrameworkTargetGuid();
            RaiseDeploymentTarget(project, mainTarget);
            RaiseDeploymentTarget(project, frameworkTarget);
            AddShellPhase(project, mainTarget, ComposeResourcesPhase, ComposeResourcesScript);
            AddShellPhase(project, mainTarget, EmbedDynamicPodsPhase, EmbedDynamicPodsScript);
            project.WriteToFile(projectPath);

            string text = File.ReadAllText(projectPath);
            if (text.Contains(AllLoadLookupFlag))
                File.WriteAllText(projectPath, text.Replace(AllLoadLookupFlag, LookupFlag));
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
XCFRAMEWORK=""""
for CANDIDATE in ""${PROJECT_DIR}/Frameworks/HDCLib/Plugins/iOS/HDCAds.xcframework"" ""${PROJECT_DIR}/Libraries/HDCLib/Plugins/iOS/HDCAds.xcframework""; do
    if [ -d ""${CANDIDATE}"" ]; then XCFRAMEWORK=""${CANDIDATE}""; break; fi
done
if [ -z ""${XCFRAMEWORK}"" ]; then echo ""warning: HDCAds.xcframework not found, Compose resources not copied""; exit 0; fi
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
