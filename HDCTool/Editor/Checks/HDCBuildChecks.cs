using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HDC.Ads.Editor;
using HDC.Onboarding.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HDC.Tool.Editor
{
    internal static class HDCBuildChecks
    {
        internal const string ChangeLogFoldout = "ChangeLog";
        private const string AdMobSettingsType = "GoogleMobileAdsSettings";

        internal static List<HDCBuildCheck> Run(HDCBuildPlatform platform, HDCBuildPreset preset, HDCBuildRules rules, HDCBuildSettings settings,
            HDCScene0 scene0 = null)
        {
            scene0 = scene0 ?? HDCScene0.Inspect();
            var checks = new List<HDCBuildCheck>
            {
                Target(platform),
                Scenes(),
                Setup(scene0),
                DebugPanel(scene0, rules),
                AdMob(platform),
                Firebase(platform, preset),
                Version(platform),
                ChangeLog(rules, settings),
            };
            checks.AddRange(Adjust(platform, scene0));
            checks.AddRange(Onboarding(platform));
            if (platform == HDCBuildPlatform.Android)
                checks.AddRange(Keystore(preset));
            else
                checks.AddRange(Ios());
            checks.RemoveAll(check => check == null);
            return checks.OrderByDescending(check => check.Level).ThenBy(check => check.Name, StringComparer.Ordinal).ToList();
        }

        internal static HDCBuildCheck Target(HDCBuildPlatform platform) =>
            platform.IsActive()
                ? HDCBuildCheck.Ok("Build target")
                : new HDCBuildCheck("Build target", HDCCheckLevel.Error,
                    $"The active build target is {EditorUserBuildSettings.activeBuildTarget}, but this tab builds {platform.Title()}.",
                    "Switch target", () => EditorUserBuildSettings.SwitchActiveBuildTarget(platform.Group(), platform.Target()));

        internal static HDCBuildCheck Scenes()
        {
            EditorBuildSettingsScene[] enabled = EditorBuildSettings.scenes.Where(scene => scene.enabled).ToArray();
            string[] missing = enabled.Where(scene => !File.Exists(scene.path)).Select(scene => scene.path).ToArray();
            if (missing.Length > 0)
                return new HDCBuildCheck("Build scenes", HDCCheckLevel.Error, "Missing scene: " + string.Join(", ", missing), "Clean missing", CleanMissingScenes);
            return enabled.Length == 0
                ? new HDCBuildCheck("Build scenes", HDCCheckLevel.Error, "Build Settings has no enabled scene.")
                : HDCBuildCheck.Ok("Build scenes");
        }

        internal static HDCBuildCheck Version(HDCBuildPlatform platform)
        {
            if (string.IsNullOrWhiteSpace(PlayerSettings.bundleVersion))
                return new HDCBuildCheck("Version", HDCCheckLevel.Error, "The version is empty.");
            if (platform == HDCBuildPlatform.Android && PlayerSettings.Android.bundleVersionCode <= 0)
                return new HDCBuildCheck("Version", HDCCheckLevel.Error, "The Android version code must be greater than 0.");
            if (platform == HDCBuildPlatform.Ios && !IsBuildNumber(PlayerSettings.iOS.buildNumber))
                return new HDCBuildCheck("Version", HDCCheckLevel.Error, "The iOS build number must be numbers separated by dots, like 12 or 1.0.12.");
            return HDCBuildCheck.Ok("Version");
        }

        internal static IEnumerable<HDCBuildCheck> Keystore(HDCBuildPreset preset)
        {
            bool custom = PlayerSettings.Android.useCustomKeystore;
            if (preset == HDCBuildPreset.Release &&
                (!custom || !File.Exists(PlayerSettings.Android.keystoreName) || string.IsNullOrWhiteSpace(PlayerSettings.Android.keyaliasName)))
            {
                yield return new HDCBuildCheck("Keystore", HDCCheckLevel.Error, "Release builds need your keystore file and key alias.", "Choose keystore", ChooseKeystore);
                yield break;
            }

            if (custom && (string.IsNullOrEmpty(PlayerSettings.Android.keystorePass) || string.IsNullOrEmpty(PlayerSettings.Android.keyaliasPass)))
            {
                yield return new HDCBuildCheck("Keystore", HDCCheckLevel.Error,
                    "Enter both keystore passwords in Signing & device at the bottom of the window. Unity forgets them when it closes.");
                yield break;
            }

            yield return HDCBuildCheck.Ok("Keystore");
        }

        internal static HDCBuildCheck ChangeLog(HDCBuildRules rules, HDCBuildSettings settings) =>
            rules.requireChangeLog && string.IsNullOrWhiteSpace(settings.ChangeLog)
                ? new HDCBuildCheck("Change log", HDCCheckLevel.Warning, "This preset expects a change log.", "Write log", () => OpenFoldout(ChangeLogFoldout))
                : null;

        internal static void ChooseKeystore()
        {
            string current = PlayerSettings.Android.keystoreName;
            string folder = File.Exists(current) ? Path.GetDirectoryName(current) : HDCBuildSettings.ProjectRoot;
            string path = EditorUtility.OpenFilePanel("Choose keystore", folder, "");
            if (string.IsNullOrEmpty(path))
                return;
            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = path;
        }

        internal static void OpenFoldout(string key) => EditorPrefs.SetBool(HDCBuildGui.FoldoutKey(key), true);

        private static HDCBuildCheck Setup(HDCScene0 scene0)
        {
            if (scene0.Path.Length == 0)
                return null;
            if (!scene0.Inspected)
                return new HDCBuildCheck("Ads setup", HDCCheckLevel.Info, "Leave Play Mode to scan scene 0.");
            return scene0.Setups > 0
                ? HDCBuildCheck.Ok("Ads setup")
                : new HDCBuildCheck("Ads setup", HDCCheckLevel.Error, $"Scene 0 ({scene0.Name}) has no HDCAdsSetup, so ads never start.", "Open scene 0",
                    () => OpenScene(scene0.Path));
        }

        private static HDCBuildCheck DebugPanel(HDCScene0 scene0, HDCBuildRules rules)
        {
            if (!rules.debugPanel || !scene0.Inspected)
                return null;
            return scene0.DebugPanels > 0
                ? HDCBuildCheck.Ok("Debug panel")
                : new HDCBuildCheck("Debug panel", HDCCheckLevel.Warning, "This preset shows the debug panel, but scene 0 has none.", "Open scene 0",
                    () => OpenScene(scene0.Path));
        }

        private static IEnumerable<HDCBuildCheck> Adjust(HDCBuildPlatform platform, HDCScene0 scene0)
        {
            if (!scene0.Inspected || scene0.AdjustTokens.Count == 0)
                yield break;
            bool missing = scene0.AdjustTokens.Any(tokens => string.IsNullOrWhiteSpace(platform == HDCBuildPlatform.Ios ? tokens.Ios : tokens.Android));
            yield return missing
                ? new HDCBuildCheck("Adjust", HDCCheckLevel.Warning, $"HDCAdjust has no {platform.Title()} app token, so Adjust does not start on {platform.Title()}.",
                    "Open scene 0", () => OpenScene(scene0.Path))
                : HDCBuildCheck.Ok("Adjust");
        }

        private static IEnumerable<HDCBuildCheck> Onboarding(HDCBuildPlatform platform)
        {
            string[] scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
            List<(bool Error, string Message)> findings = HDCOnboardingBuildCheck.Findings(HDCOnboardingConfigKeys.FindSettings(), platform.Target(), scenes);
            foreach ((bool error, string message) in findings)
            {
                bool missingKey = message.Contains("custom key");
                yield return new HDCBuildCheck("Onboarding config", error ? HDCCheckLevel.Error : HDCCheckLevel.Warning, message,
                    missingKey ? "Add config keys" : "Edit custom keys",
                    missingKey
                        ? (Action)HDCOnboardingConfigKeys.AddMissing
                        : () => HDCAdsConfigWindow.OpenCard(HDCAdsConfigWindow.KeysCard, null,
                            platform == HDCBuildPlatform.Ios ? HDCAdsConfigWindow.IosPlatform : HDCAdsConfigWindow.AndroidPlatform,
                            "the Build Tool's " + platform.Title() + " tab"));
            }
        }

        private static HDCBuildCheck AdMob(HDCBuildPlatform platform)
        {
            string guid = AssetDatabase.FindAssets("t:" + AdMobSettingsType).FirstOrDefault();
            if (guid == null)
                return new HDCBuildCheck("AdMob app ID", HDCCheckLevel.Warning, "There is no GoogleMobileAdsSettings asset.", "Open settings",
                    () => EditorApplication.ExecuteMenuItem("Assets/Google Mobile Ads/Settings..."));
            var asset = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(guid));
            SerializedProperty property = new SerializedObject(asset).FindProperty(platform == HDCBuildPlatform.Ios ? "adMobIOSAppId" : "adMobAndroidAppId");
            if (property != null && !string.IsNullOrWhiteSpace(property.stringValue))
                return HDCBuildCheck.Ok("AdMob app ID");
            return new HDCBuildCheck("AdMob app ID", HDCCheckLevel.Error, $"Google Mobile Ads has no {platform.Title()} app ID, so the app stops at launch.",
                "Select asset", () => Selection.activeObject = asset);
        }

        internal static HDCBuildCheck Firebase(HDCBuildPlatform platform, HDCBuildPreset preset)
        {
            if (!AppDomain.CurrentDomain.GetAssemblies().Any(assembly => assembly.GetName().Name == "Firebase.App"))
                return null;
            string file = platform == HDCBuildPlatform.Ios ? "GoogleService-Info.plist" : "google-services.json";
            if (Directory.EnumerateFiles(Application.dataPath, file, SearchOption.AllDirectories).Any())
                return HDCBuildCheck.Ok("Firebase config");
            return new HDCBuildCheck("Firebase config", preset == HDCBuildPreset.Release ? HDCCheckLevel.Error : HDCCheckLevel.Warning,
                $"Firebase is in the project, but {file} is missing from Assets, so Firebase does not start: no Remote Config (the default config is used) " +
                "and no Analytics events.");
        }

        private static IEnumerable<HDCBuildCheck> Ios()
        {
            string team = HDCIosLocalBuild.TeamId.Length > 0 ? HDCIosLocalBuild.TeamId : PlayerSettings.iOS.appleDeveloperTeamID ?? "";
            yield return team.Trim().Length > 0
                ? HDCBuildCheck.Ok("Signing")
                : new HDCBuildCheck("Signing", HDCCheckLevel.Warning,
                    "No Apple team, so Xcode asks for one before it runs on a device. Pick one in Team, under Signing & device at the bottom of the window.");
            if (HDCBuildState.IosTarget == HDCAppleTarget.Simulator)
            {
                List<string> blocking = HDCSimulatorSupport.BlockingLibraries();
                List<string> deviceOnly = HDCSimulatorSupport.DeviceOnlyLibraries();
                yield return blocking.Count > 0 ? new HDCBuildCheck("Simulator", HDCCheckLevel.Warning, HDCBuildRunner.SimulatorProblem(blocking))
                    : deviceOnly.Count > 0 ? new HDCBuildCheck("Simulator", HDCCheckLevel.Info, HDCBuildRunner.SimulatorNote(deviceOnly))
                    : HDCBuildCheck.Ok("Simulator");
            }
        }

        internal static bool IsBuildNumber(string value) =>
            !string.IsNullOrWhiteSpace(value) && value.Split('.').All(part => part.Length > 0 && part.All(char.IsDigit));

        private static void CleanMissingScenes() =>
            EditorBuildSettings.scenes = EditorBuildSettings.scenes.Where(scene => File.Exists(scene.path)).ToArray();

        private static void OpenScene(string path)
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(path);
        }
    }
}
