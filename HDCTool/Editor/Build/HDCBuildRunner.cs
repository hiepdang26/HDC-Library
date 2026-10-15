using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using HDC.Ads.Editor;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;

namespace HDC.Tool.Editor
{
    internal enum HDCBuildAction
    {
        Build,
        BuildAndRun,
        ExportAndOpen,
    }

    internal static class HDCBuildRunner
    {
        internal const string PresetArgument = "-hdcPreset";
        internal const string IosTargetArgument = "-hdcIosTarget";

        internal static void BuildFromCommandLine()
        {
            try
            {
                HDCBuildPlatform platform = EditorUserBuildSettings.activeBuildTarget == BuildTarget.iOS ? HDCBuildPlatform.Ios : HDCBuildPlatform.Android;
                string[] arguments = Environment.GetCommandLineArgs();
                HDCBuildPreset preset = PresetFrom(arguments);
                HDCAppleDevice target = platform == HDCBuildPlatform.Ios ? new HDCAppleDevice { Kind = IosTargetFrom(arguments) } : null;
                HDCBuildResult result = Run(platform, preset, HDCBuildAction.Build, null, target);
                UnityEngine.Debug.Log($"[HDCTool] {result.outcome} {result.buildName} at {result.outputPath}, report {result.reportPath}. {result.message}");
                EditorApplication.Exit(result.Succeeded ? 0 : 1);
            }
            catch (Exception exception)
            {
                UnityEngine.Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        internal static HDCAppleTarget IosTargetFrom(string[] arguments)
        {
            int index = Array.IndexOf(arguments ?? new string[0], IosTargetArgument);
            string value = index >= 0 && index + 1 < arguments.Length ? arguments[index + 1] : "";
            return Enum.TryParse(value, true, out HDCAppleTarget target) && Enum.IsDefined(typeof(HDCAppleTarget), target) ? target : HDCAppleTarget.Phone;
        }

        internal static HDCBuildPreset PresetFrom(string[] arguments)
        {
            int index = Array.IndexOf(arguments ?? new string[0], PresetArgument);
            string value = index >= 0 && index + 1 < arguments.Length ? arguments[index + 1] : "";
            return Enum.TryParse(value, true, out HDCBuildPreset preset) && Enum.IsDefined(typeof(HDCBuildPreset), preset) ? preset : HDCBuildPreset.TestDebug;
        }

        internal static HDCBuildResult Run(HDCBuildPlatform platform, HDCBuildPreset preset, HDCBuildAction action, HDCAndroidDevice device,
            HDCAppleDevice appleDevice = null)
        {
            HDCBuildSettings settings = HDCBuildSettings.Load();
            if (settings == null)
                return HDCBuildResult.Stopped(platform, preset, HDCBuildResult.BlockedOutcome, HDCBuildSettings.NotReady);
            HDCBuildRules rules = settings.Rules(preset).Copy();
            List<HDCBuildCheck> checks = HDCBuildChecks.Run(platform, preset, rules, settings);
            List<HDCBuildCheck> blocking = checks.Where(check => check.Level == HDCCheckLevel.Error).ToList();
            if (blocking.Count > 0)
                return HDCBuildResult.Stopped(platform, preset, HDCBuildResult.BlockedOutcome,
                    "Fix these first: " + string.Join("; ", blocking.Select(check => check.Name + " (" + check.Message + ")")));
            string runProblem = platform == HDCBuildPlatform.Ios && action == HDCBuildAction.BuildAndRun ? IosRunProblem(appleDevice) : null;
            if (runProblem != null)
                return HDCBuildResult.Stopped(platform, preset, HDCBuildResult.BlockedOutcome, runProblem);
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return HDCBuildResult.Stopped(platform, preset, HDCBuildResult.CancelledOutcome, "The open scenes were not saved.");

            string productName = PlayerSettings.productName;
            bool appBundle = platform == HDCBuildPlatform.Android && rules.androidAppBundle;
            string buildName = HDCBuildNames.BuildName(productName, preset, PlayerSettings.bundleVersion, HDCBuildNames.Number(platform),
                HDCBuildNames.Artifact(platform, appBundle), rules.developmentBuild);
            HDCAppleTarget target = appleDevice?.Kind ?? HDCBuildState.IosTarget;
            string folder = platform == HDCBuildPlatform.Ios ? HDCBuildFolders.IosExport(target) : settings.OutputPath(platform);
            Directory.CreateDirectory(folder);
            string location = platform == HDCBuildPlatform.Android ? Path.Combine(folder, buildName + (appBundle ? ".aab" : ".apk")) : folder;

            BuildOptions options = BuildOptions.None;
            if (rules.developmentBuild)
                options |= BuildOptions.Development;
            bool append = platform == HDCBuildPlatform.Ios && settings.IosAppendExport && XcodeProject(location) != null &&
                          BuildPipeline.BuildCanBeAppended(BuildTarget.iOS, location) == CanAppendBuild.Yes;
            if (append)
                options |= BuildOptions.AcceptExternalModificationsToPlayer;
            string kept = platform == HDCBuildPlatform.Ios && !append ? KeepAside(location) : null;
            bool unityRuns = action == HDCBuildAction.BuildAndRun && appBundle;
            if (unityRuns)
                options |= BuildOptions.AutoRunPlayer;

            var buildOptions = new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray(),
                locationPathName = location,
                target = platform.Target(),
                targetGroup = platform.Group(),
                options = options,
            };

            bool appBundleBefore = EditorUserBuildSettings.buildAppBundle;
            iOSSdkVersion sdkBefore = PlayerSettings.iOS.sdkVersion;
            AppleMobileArchitectureSimulator simulatorArchitectureBefore = PlayerSettings.iOS.simulatorSdkArchitecture;
            HDCMacRun macRunBefore = HDCIosLocalBuild.MacRun;
            bool simulator = platform == HDCBuildPlatform.Ios && target == HDCAppleTarget.Simulator;
            bool mac = platform == HDCBuildPlatform.Ios && target == HDCAppleTarget.Mac;
            var clock = Stopwatch.StartNew();
            BuildReport report;
            List<string> stampFolders = null;
            try
            {
                if (platform == HDCBuildPlatform.Android)
                    EditorUserBuildSettings.buildAppBundle = appBundle;
                if (simulator)
                {
                    PlayerSettings.iOS.sdkVersion = iOSSdkVersion.SimulatorSDK;
                    if (System.Runtime.InteropServices.RuntimeInformation.OSArchitecture == System.Runtime.InteropServices.Architecture.Arm64)
                        PlayerSettings.iOS.simulatorSdkArchitecture = AppleMobileArchitectureSimulator.ARM64;
                }
                else if (platform == HDCBuildPlatform.Ios)
                {
                    PlayerSettings.iOS.sdkVersion = iOSSdkVersion.DeviceSDK;
                }

                if (mac)
                    HDCIosLocalBuild.MacRun = HDCMacRun.On;
                stampFolders = HDCBuildStamp.Write(rules);
                HDCBuildSceneProcessor.ActiveRules = rules;
                report = BuildPipeline.BuildPlayer(buildOptions);
            }
            finally
            {
                HDCBuildSceneProcessor.ActiveRules = null;
                HDCBuildStamp.Remove(stampFolders);
                if (platform == HDCBuildPlatform.Android)
                    EditorUserBuildSettings.buildAppBundle = appBundleBefore;
                if (platform == HDCBuildPlatform.Ios)
                {
                    PlayerSettings.iOS.sdkVersion = sdkBefore;
                    PlayerSettings.iOS.simulatorSdkArchitecture = simulatorArchitectureBefore;
                }

                if (mac)
                    HDCIosLocalBuild.MacRun = macRunBefore;
                Restore(kept, location);
            }

            clock.Stop();
            var result = new HDCBuildResult
            {
                platform = platform,
                preset = preset,
                buildName = buildName,
                outputPath = location,
                seconds = clock.Elapsed.TotalSeconds,
                finishedAt = DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture),
                iosTarget = target,
            };
            BuildResult outcome = report.summary.result;
            result.outcome = outcome == BuildResult.Succeeded ? HDCBuildResult.SucceededOutcome
                : outcome == BuildResult.Cancelled ? HDCBuildResult.CancelledOutcome
                : HDCBuildResult.FailedOutcome;
            result.sizeBytes = OutputSize(location);
            List<string> errors = report.steps.SelectMany(step => step.messages)
                .Where(message => message.type == UnityEngine.LogType.Error || message.type == UnityEngine.LogType.Exception)
                .Select(message => message.content).ToList();
            if (!result.Succeeded && errors.Count > 0)
                result.message = errors[0].Split('\n')[0];

            if (result.Succeeded)
                AfterBuild(result, action, device, appleDevice, unityRuns);
            string deviceText = platform == HDCBuildPlatform.Ios
                ? appleDevice?.Title ?? ""
                : device == null ? "" : (device.Title + ", " + device.Details).Trim(' ', ',');
            result.reportPath = HDCBuildReport.Write(result, rules, settings, checks, deviceText, errors);
            return result;
        }

        internal static string IosRunProblem(HDCAppleDevice appleDevice) => IosRunProblem(appleDevice, HDCBuildState.IosTarget, HasTeam, HDCSimulatorSupport.BlockingLibraries);

        internal static string IosRunProblem(HDCAppleDevice appleDevice, HDCAppleTarget target, Func<bool> hasTeam, Func<List<string>> blocking)
        {
            HDCAppleTarget kind = appleDevice?.Kind ?? target;
            if (appleDevice == null)
                return kind == HDCAppleTarget.Simulator ? "No iOS simulator. Add one in Xcode > Window > Devices and Simulators, then press Refresh."
                    : kind == HDCAppleTarget.Mac ? "Running on My Mac needs a Mac with Apple silicon."
                    : "No iPhone is connected. Connect it with a cable, unlock it, trust this Mac, then press Refresh.";
            if (kind == HDCAppleTarget.Simulator)
            {
                List<string> libraries = blocking();
                return libraries.Count == 0 ? null : SimulatorProblem(libraries);
            }

            return hasTeam() ? null : "Running on " + (kind == HDCAppleTarget.Mac ? "My Mac" : "an iPhone") + " signs the app, so it needs a team: pick one in Team.";
        }

        internal static string SimulatorProblem(IReadOnlyCollection<string> libraries) =>
            "The simulator build cannot link: " + string.Join(", ", libraries) + (libraries.Count == 1 ? " has" : " have") +
            " arm64 code that the tool cannot mark for the simulator. Run on an iPhone or My Mac instead.";

        internal static string SimulatorNote(IReadOnlyCollection<string> libraries) =>
            string.Join(", ", libraries) + (libraries.Count == 1 ? " has" : " have") + " arm64 code built for devices only. " +
            "The simulator export marks that code for the simulator, so the build links; the files in your project stay as they are.";

        internal static bool HasTeam() =>
            HDCIosLocalBuild.TeamId.Length > 0 || !string.IsNullOrWhiteSpace(PlayerSettings.iOS.appleDeveloperTeamID);

        private static void AfterBuild(HDCBuildResult result, HDCBuildAction action, HDCAndroidDevice device, HDCAppleDevice appleDevice, bool unityRuns)
        {
            if (UnityEngine.Application.isBatchMode)
                return;
            if (result.platform == HDCBuildPlatform.Ios)
            {
                if (action == HDCBuildAction.Build)
                {
                    HDCBuildFolders.Open(result.outputPath);
                    return;
                }

                bool run = action == HDCBuildAction.BuildAndRun;
                string problem = HDCXcode.Start(XcodeProject(result.outputPath), appleDevice, run);
                if (problem != null && !run)
                    OpenXcode(result.outputPath);
                result.message = problem ?? (run ? "Exported. Xcode is building and running it on " + appleDevice?.Title + "." : "Exported. Xcode is opening it.");
                return;
            }

            if (action != HDCBuildAction.BuildAndRun || unityRuns)
            {
                if (action == HDCBuildAction.Build)
                    EditorUtility.RevealInFinder(result.outputPath);
                return;
            }

            if (device == null || !device.Ready)
            {
                result.message = "Built, but no Android device is ready to run it.";
                return;
            }

            string package = PlayerSettings.GetApplicationIdentifier(HDCBuildPlatform.Android.Named());
            HDCProcess.Output install = HDCAndroidDevices.Install(device.Serial, result.outputPath);
            if (!install.Ok)
            {
                result.message = "Install failed: " + install.All.Split('\n').Last();
                return;
            }

            HDCProcess.Output launch = HDCAndroidDevices.Launch(device.Serial, package);
            result.message = launch.Ok ? "Installed and launched on " + device.Title : "Installed, but the launch failed: " + launch.All.Split('\n').Last();
        }

        internal static string KeepAside(string folder)
        {
            if (!Directory.Exists(folder))
                return null;
            string[] keptFolders = new[] { HDCBuildFolders.AndroidFolderName, HDCBuildFolders.SimulatorFolderName }
                .Select(name => Path.Combine(folder, name)).Where(Directory.Exists).ToArray();
            string[] reports = Directory.GetFiles(folder, "*" + HDCBuildReport.Suffix);
            if (keptFolders.Length == 0 && reports.Length == 0)
                return null;
            string kept = Path.Combine(Path.GetDirectoryName(folder.TrimEnd('/', '\\')) ?? folder,
                "." + Path.GetFileName(folder.TrimEnd('/', '\\')) + "-kept-" + DateTime.Now.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture));
            Directory.CreateDirectory(kept);
            foreach (string keptFolder in keptFolders)
                Directory.Move(keptFolder, Path.Combine(kept, Path.GetFileName(keptFolder)));
            foreach (string report in reports)
                File.Move(report, Path.Combine(kept, Path.GetFileName(report)));
            return kept;
        }

        internal static void Restore(string kept, string folder)
        {
            if (string.IsNullOrEmpty(kept) || !Directory.Exists(kept))
                return;
            Directory.CreateDirectory(folder);
            foreach (string directory in Directory.GetDirectories(kept))
            {
                string target = Path.Combine(folder, Path.GetFileName(directory));
                if (!Directory.Exists(target))
                    Directory.Move(directory, target);
            }

            foreach (string file in Directory.GetFiles(kept))
            {
                string target = Path.Combine(folder, Path.GetFileName(file));
                if (!File.Exists(target))
                    File.Move(file, target);
            }

            if (Directory.EnumerateFileSystemEntries(kept).Any())
                UnityEngine.Debug.LogWarning("[HDCTool] Some kept files could not go back into " + folder + "; they are in " + kept);
            else
                Directory.Delete(kept);
        }

        internal static string XcodeProject(string exportFolder)
        {
            string workspace = Path.Combine(exportFolder, "Unity-iPhone.xcworkspace");
            if (Directory.Exists(workspace))
                return workspace;
            string project = Path.Combine(exportFolder, "Unity-iPhone.xcodeproj");
            return Directory.Exists(project) ? project : null;
        }

        internal static void OpenXcode(string exportFolder)
        {
            string project = XcodeProject(exportFolder);
            if (project != null)
                HDCProcess.Start("/usr/bin/open", $"\"{project}\"");
        }

        private static long OutputSize(string location)
        {
            if (File.Exists(location))
                return new FileInfo(location).Length;
            return 0;
        }
    }
}
