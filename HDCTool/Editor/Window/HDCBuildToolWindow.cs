using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HDC.Ads.Editor;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace HDC.Tool.Editor
{
    internal sealed class HDCBuildToolWindow : EditorWindow
    {
        private readonly Dictionary<HDCBuildPlatform, List<HDCBuildCheck>> checks = new Dictionary<HDCBuildPlatform, List<HDCBuildCheck>>();
        private readonly Dictionary<HDCBuildPlatform, DateTime> scannedAt = new Dictionary<HDCBuildPlatform, DateTime>();
        [NonSerialized] private readonly Queue<Action> later = new Queue<Action>();
        [NonSerialized] private Task<List<HDCAndroidDevice>> androidScan;
        [NonSerialized] private Task<List<HDCAppleDevice>> appleScan;
        [NonSerialized] private HDCScene0 scene0;
        [NonSerialized] private bool building;
        [NonSerialized] private bool scanQueued;
        [NonSerialized] private bool devicesQueued;
        [NonSerialized] private List<HDCAppleTeam> teams;
        private Vector2 scroll;

        internal HDCBuildSettings Settings { get; private set; }

        internal HDCBuildPlatform Platform { get; private set; }

        internal List<HDCAndroidDevice> AndroidDevices { get; private set; } = new List<HDCAndroidDevice>();

        internal List<HDCAppleDevice> AppleDevices { get; private set; } = new List<HDCAppleDevice>();

        internal List<HDCSdkVersions.Entry> Sdks { get; private set; }

        internal string Notice { get; private set; } = "";

        internal MessageType NoticeType { get; private set; }

        internal bool Busy => building;

        internal bool ScanningDevices => Platform == HDCBuildPlatform.Android ? androidScan != null : appleScan != null;

        internal HDCBuildPreset Preset => HDCBuildState.Preset(Platform);

        internal HDCBuildRules Rules => Settings.Rules(Preset);

        internal List<HDCBuildCheck> Checks => checks.TryGetValue(Platform, out List<HDCBuildCheck> list) ? list : null;

        internal DateTime ScannedAt => scannedAt.TryGetValue(Platform, out DateTime at) ? at : default;

        internal HDCAndroidDevice SelectedAndroidDevice =>
            AndroidDevices.FirstOrDefault(device => device.Serial == HDCBuildState.AndroidDevice) ?? AndroidDevices.FirstOrDefault(device => device.Ready);

        internal HDCAppleDevice SelectedAppleDevice
        {
            get
            {
                HDCAppleTarget target = HDCBuildState.IosTarget;
                List<HDCAppleDevice> devices = AppleDevicesFor(target);
                string saved = target == HDCAppleTarget.Simulator ? HDCBuildState.IosSimulator : HDCBuildState.IosDevice;
                return devices.FirstOrDefault(device => device.Udid == saved) ?? devices.FirstOrDefault(device => device.Booted) ?? devices.FirstOrDefault();
            }
        }

        internal string IosExportFolder => HDCBuildFolders.IosExport(HDCBuildState.IosTarget);

        internal List<HDCAppleDevice> AppleDevicesFor(HDCAppleTarget target) => AppleDevices.Where(device => device.Kind == target).ToList();

        internal List<HDCAppleTeam> Teams => teams ?? (teams = HDCAppleTeams.List());

        internal const float MinWidth = 560f;

        [MenuItem("HDC/Build Tool #b", false, 5)]
        private static void Toggle()
        {
            HDCBuildToolWindow[] open = Resources.FindObjectsOfTypeAll<HDCBuildToolWindow>();
            if (open.Length == 0)
            {
                Open();
                return;
            }

            foreach (HDCBuildToolWindow window in open)
                window.Close();
        }

        internal static void Open()
        {
            bool created = !HasOpenInstances<HDCBuildToolWindow>();
            var window = GetWindow<HDCBuildToolWindow>();
            window.titleContent = new GUIContent("HDC Build Tool");
            window.minSize = new Vector2(MinWidth, 600f);
            if (created)
                window.position = DefaultPosition(ScreenArea());
        }

        internal static Rect DefaultPosition(Rect screen)
        {
            float width = Mathf.Min(screen.width, Mathf.Max(MinWidth, Mathf.Round(screen.width / 2f)));
            return new Rect(screen.xMax - width, screen.y, width, screen.height);
        }

        private static Rect ScreenArea()
        {
            Rect main = EditorGUIUtility.GetMainWindowPosition();
            if (Application.platform == RuntimePlatform.OSXEditor)
                return main;
            try
            {
                Rect desktop = InternalEditorUtility.GetBoundsOfDesktopAtPoint(main.center);
                return desktop.width > 0f && desktop.height > 0f ? desktop : main;
            }
            catch (ArgumentException)
            {
                return main;
            }
        }

        internal void SelectPlatform(HDCBuildPlatform platform)
        {
            if (platform == Platform)
                return;
            Platform = platform;
            HDCBuildState.Tab = platform;
            SetNotice("", MessageType.None);
            RefreshDevices();
        }

        internal void SelectPreset(HDCBuildPreset preset)
        {
            HDCBuildState.SetPreset(Platform, preset);
            MarkStale();
        }

        internal void Scan(bool inspectScene0 = true)
        {
            if (inspectScene0 || scene0 == null || scene0.Path != HDCScene0.FirstScenePath() || HDCScene0.IsLoaded(scene0.Path))
                scene0 = HDCScene0.Inspect();
            checks[Platform] = HDCBuildChecks.Run(Platform, Preset, Rules, Settings, scene0);
            scannedAt[Platform] = DateTime.Now;
            Repaint();
        }

        internal void QueueScan()
        {
            if (scanQueued)
                return;
            scanQueued = true;
            Later(() =>
            {
                scanQueued = false;
                if (Settings != null && Checks == null && !building)
                    Scan(false);
            });
        }

        internal void Later(Action action)
        {
            later.Enqueue(action);
            Repaint();
        }

        internal void MarkStale() => checks.Clear();

        internal void Refresh()
        {
            teams = null;
            Sdks = HDCSdkVersions.Read(HDCBuildFolders.IosExport(HDCAppleTarget.Phone));
            RefreshDevices();
            Scan();
        }

        internal void RefreshDevices()
        {
            if (Platform == HDCBuildPlatform.Android)
            {
                if (androidScan == null)
                {
                    string adb = HDCAndroidDevices.AdbPath();
                    androidScan = Task.Run(() => HDCAndroidDevices.List(adb));
                }
            }
            else if (Application.platform == RuntimePlatform.OSXEditor && appleScan == null)
            {
                appleScan = Task.Run(HDCAppleDevices.All);
            }

            Repaint();
        }

        internal void SelectAndroidDevice(HDCAndroidDevice device)
        {
            HDCBuildState.AndroidDevice = device?.Serial ?? "";
            Repaint();
        }

        internal void SelectAppleDevice(HDCAppleDevice device)
        {
            if (device?.Kind == HDCAppleTarget.Simulator)
                HDCBuildState.IosSimulator = device.Udid;
            else if (device?.Kind == HDCAppleTarget.Phone)
                HDCBuildState.IosDevice = device.Udid;
            Repaint();
        }

        internal void SelectIosTarget(HDCAppleTarget target)
        {
            HDCBuildState.IosTarget = target;
            MarkStale();
            Repaint();
        }

        internal void RefreshTeams() => teams = null;

        internal void SetNotice(string message, MessageType type)
        {
            Notice = message ?? "";
            NoticeType = type;
            Repaint();
        }

        internal void Build(HDCBuildAction action)
        {
            if (building)
                return;
            HDCBuildPlatform platform = Platform;
            HDCBuildPreset preset = Preset;
            HDCAndroidDevice device = platform == HDCBuildPlatform.Android ? SelectedAndroidDevice : null;
            HDCAppleDevice appleDevice = platform == HDCBuildPlatform.Ios ? SelectedAppleDevice : null;
            building = true;
            SetNotice("", MessageType.None);
            Later(() =>
            {
                try
                {
                    HDCBuildResult result = HDCBuildRunner.Run(platform, preset, action, device, appleDevice);
                    HDCBuildState.SetLastResult(result);
                    if (result.outcome == HDCBuildResult.BlockedOutcome)
                    {
                        building = false;
                        Scan();
                        EditorUtility.DisplayDialog("HDC Build Tool", result.message, "OK");
                    }
                }
                finally
                {
                    building = false;
                    Repaint();
                }
            });
        }

        internal void RunOnDevice(string title, Func<HDCAndroidDevice, HDCProcess.Output> command)
        {
            HDCAndroidDevice device = SelectedAndroidDevice;
            if (device == null || !device.Ready)
            {
                SetNotice("No Android device is ready. Connect one with USB debugging on, then press Refresh.", MessageType.Warning);
                return;
            }

            Later(() =>
            {
                HDCProcess.Output output;
                try
                {
                    EditorUtility.DisplayProgressBar("HDC Build Tool", $"{title} on {device.Title}", 0.5f);
                    output = command(device);
                }
                finally
                {
                    EditorUtility.ClearProgressBar();
                }

                string last = output.All.Split('\n').Select(line => line.Trim()).LastOrDefault(line => line.Length > 0) ?? "";
                SetNotice(output.Ok ? $"{title}: done on {device.Title}." : $"{title} failed: {last}", output.Ok ? MessageType.Info : MessageType.Error);
            });
        }

        internal string LatestApk()
        {
            HDCBuildResult last = HDCBuildState.LastResult(HDCBuildPlatform.Android);
            if (last != null && last.Succeeded && last.outputPath.EndsWith(".apk", StringComparison.OrdinalIgnoreCase) && File.Exists(last.outputPath))
                return last.outputPath;
            string folder = Settings.OutputPath(HDCBuildPlatform.Android);
            return Directory.Exists(folder)
                ? new DirectoryInfo(folder).GetFiles("*.apk").OrderByDescending(file => file.LastWriteTimeUtc).FirstOrDefault()?.FullName
                : null;
        }

        private void OnEnable()
        {
            Platform = HDCBuildState.Tab;
            devicesQueued = true;
        }

        private void OnDisable()
        {
            if (Settings != null)
                Settings.Save();
        }

        private void OnDestroy() => HDCScrcpy.Stop();

        private void OnFocus()
        {
            if (Settings != null)
                MarkStale();
        }

        private void OnLostFocus()
        {
            if (Settings != null)
                Settings.Save();
        }

        private void Update()
        {
            HDCXcode.Outcome xcode = HDCXcode.Poll();
            if (xcode != null)
            {
                SetNotice(xcode.Message, xcode.Ok ? MessageType.Info : MessageType.Error);
                HDCBuildResult last = HDCBuildState.LastResult(HDCBuildPlatform.Ios);
                if (last != null)
                {
                    last.message = xcode.Message;
                    HDCBuildState.SetLastResult(last);
                }
            }

            while (later.Count > 0)
            {
                Action action = later.Dequeue();
                try
                {
                    action();
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }

            if (androidScan != null && androidScan.IsCompleted)
            {
                AndroidDevices = androidScan.Status == TaskStatus.RanToCompletion ? androidScan.Result : new List<HDCAndroidDevice>();
                androidScan = null;
                Repaint();
            }

            if (appleScan != null && appleScan.IsCompleted)
            {
                AppleDevices = appleScan.Status == TaskStatus.RanToCompletion ? appleScan.Result : new List<HDCAppleDevice>();
                appleScan = null;
                Repaint();
            }
        }

        private void OnInspectorUpdate()
        {
            if (Settings == null)
                Repaint();
        }

        private void OnGUI()
        {
            if (Settings == null)
                Settings = HDCBuildSettings.Find();
            if (Settings == null && HDCAssetFiles.WhyNotCreate(HDCBuildSettings.AssetPath) == null)
                Settings = HDCBuildSettings.CreateAt(HDCBuildSettings.AssetPath);
            if (Settings == null)
            {
                EditorGUILayout.Space(8f);
                EditorGUILayout.HelpBox(HDCBuildSettings.NotReady, MessageType.Warning);
                return;
            }

            if (Sdks == null)
                Sdks = HDCSdkVersions.Read(HDCBuildFolders.IosExport(HDCAppleTarget.Phone));
            if (devicesQueued)
            {
                devicesQueued = false;
                RefreshDevices();
            }

            using (new EditorGUI.DisabledScope(building || EditorApplication.isCompiling))
            {
                HDCBuildHeader.Draw(this);
                scroll = EditorGUILayout.BeginScrollView(scroll);
                HDCBuildOverviewSection.Draw(this);
                HDCBuildSummarySection.DrawInfo(this);
                HDCBuildSdkSection.Draw(this);
                HDCBuildCheckSection.Draw(this);
                HDCBuildSummarySection.DrawBuild(this);
                HDCBuildScenesSection.Draw(this);
                EditorGUILayout.EndScrollView();
                HDCBuildAreaSection.Draw(this);
            }
        }
    }
}
