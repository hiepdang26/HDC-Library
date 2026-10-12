using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using HDC.Ads.Editor;
using UnityEditor;
using UnityEngine;

namespace HDC.Tool.Editor
{
    internal static class HDCBuildAreaSection
    {
        private const float Label = 96f;
        private const float Button = 28f;
        private const float Big = 250f;
        private const float Wide = 170f;
        private const float Narrow = 86f;
        private const float Tall = 30f;
        private const string BundleControl = "HDC.Build.BundleId";

        private static readonly string[] TargetNames = { HDCAppleTarget.Phone.Title(), HDCAppleTarget.Mac.Title(), HDCAppleTarget.Simulator.Title() };
        private static readonly string[] KeystoreNames = { "Unity debug key", "Custom keystore" };

        private static GUIStyle footerStyle;
        private static GUIStyle bigStyle;
        private static GUIStyle captionStyle;
        private static GUIStyle warningStyle;
        private static GUIStyle noteStyle;
        private static GUIStyle placeholderStyle;

        internal static void Draw(HDCBuildToolWindow window)
        {
            Styles();
            Rect line = GUILayoutUtility.GetRect(1f, 2f, GUILayout.ExpandWidth(true));
            Rect area = EditorGUILayout.BeginVertical(footerStyle);
            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(area, HDCBuildGui.Panel);
                EditorGUI.DrawRect(line, HDCBuildGui.Green);
            }

            bool ios = window.Platform == HDCBuildPlatform.Ios;
            float field = FieldWidth();
            using (new HDCBuildGui.LabelScope(Label))
            {
                Caption(window);
                if (ios)
                    IosSetup(window, field);
                else
                    AndroidSetup(window, field);
            }

            string problem = ios ? HDCBuildRunner.IosRunProblem(window.SelectedAppleDevice) : AndroidProblem(window);
            if (problem != null)
                GUILayout.Label(problem, warningStyle);
            else if (ios && HDCBuildState.IosTarget == HDCAppleTarget.Simulator && HDCSimulatorSupport.DeviceOnlyLibraries().Count > 0)
                GUILayout.Label(HDCBuildRunner.SimulatorNote(HDCSimulatorSupport.DeviceOnlyLibraries()), noteStyle);
            GUILayout.Space(6f);
            if (ios)
                IosButtons(window, problem == null);
            else
                AndroidButtons(window);

            HDCBuildResult last = HDCBuildState.LastResult(window.Platform);
            if (window.Notice.Length > 0)
                EditorGUILayout.HelpBox(window.Notice, window.NoticeType);
            else if (last != null && !string.IsNullOrEmpty(last.message))
                HDCBuildGui.Note(last.message);
            EditorGUILayout.EndVertical();
        }

        internal static string Describe(HDCBuildResult result)
        {
            string text = result.outcome;
            if (!string.IsNullOrEmpty(result.buildName))
                text += " " + result.buildName;
            if (result.seconds > 0d)
                text += " in " + Duration(result.seconds);
            if (result.sizeBytes > 0)
                text += ", " + Size(result.sizeBytes);
            if (!string.IsNullOrEmpty(result.finishedAt))
                text += " at " + result.finishedAt;
            if (!string.IsNullOrEmpty(result.message))
                text += ". " + result.message;
            return text;
        }

        internal static string Short(HDCBuildResult result, string word)
        {
            string text = word + " " + (result.Succeeded ? result.finishedAt : result.outcome.ToLowerInvariant() + " " + result.finishedAt).Trim();
            if (result.sizeBytes > 0)
                text += " · " + Size(result.sizeBytes);
            if (result.seconds > 0d)
                text += " · " + Duration(result.seconds);
            return text;
        }

        internal static string AndroidProblem(HDCBuildToolWindow window)
        {
            if (PlayerSettings.Android.useCustomKeystore &&
                (string.IsNullOrEmpty(PlayerSettings.Android.keystorePass) || string.IsNullOrEmpty(PlayerSettings.Android.keyaliasPass)))
                return "Enter both keystore passwords. Unity forgets them when it closes.";
            return window.AndroidDevices.Any(device => device.Ready) || window.ScanningDevices ? null : "No device is ready. Connect one with USB debugging on, then press Refresh.";
        }

        private static float FieldWidth() => Mathf.Clamp(EditorGUIUtility.currentViewWidth - 28f - Label - Button - 12f, 200f, 470f);

        private static void Caption(HDCBuildToolWindow window)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("Signing & device", captionStyle);
                GUILayout.FlexibleSpace();
                HDCBuildResult last = HDCBuildState.LastResult(window.Platform);
                if (last == null)
                    return;
                Color color = last.Succeeded ? HDCBuildGui.Sky : last.outcome == HDCBuildResult.FailedOutcome ? HDCBuildGui.Red : HDCBuildGui.Amber;
                HDCBuildGui.Pill(Short(last, window.Platform == HDCBuildPlatform.Ios ? "Last export" : "Last build"), color, Describe(last));
            }
        }

        private static void IosSetup(HDCBuildToolWindow window, float field)
        {
            HDCAppleTarget target = HDCBuildState.IosTarget;
            using (new EditorGUI.DisabledScope(target == HDCAppleTarget.Simulator))
            {
                string team = HDCAppleTeamField.Draw("Team", HDCIosLocalBuild.TeamId, window.Teams, window.RefreshTeams, field);
                if (team != HDCIosLocalBuild.TeamId)
                {
                    HDCIosLocalBuild.TeamId = team;
                    window.MarkStale();
                }

                BundleId(field);
            }

            Rect run = Row(field);
            int next = GUI.Toolbar(EditorGUI.PrefixLabel(run, new GUIContent("Run on", "Where Build and run installs and opens the app. A simulator build uses the Simulator SDK, the others the Device SDK.")), (int)target, TargetNames);
            if (next != (int)target)
                window.SelectIosTarget((HDCAppleTarget)next);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (target == HDCAppleTarget.Mac)
                {
                    HDCAppleDevice mac = window.SelectedAppleDevice;
                    using (new EditorGUI.DisabledScope(true))
                        EditorGUILayout.TextField("Device", mac != null ? mac.Title : "Needs a Mac with Apple silicon", GUILayout.Width(Label + field));
                }
                else
                {
                    List<HDCAppleDevice> devices = window.AppleDevicesFor(target);
                    HDCAppleDevice selected = window.SelectedAppleDevice;
                    string empty = window.ScanningDevices ? "Looking for devices..." : target == HDCAppleTarget.Simulator ? "No iOS simulator" : "No iPhone connected";
                    DevicePopup(devices.Select(device => device.Title).ToArray(), devices.IndexOf(selected), empty, field,
                        index => window.SelectAppleDevice(devices[index]));
                }

                Refresh(window);
            }
        }

        private static void AndroidSetup(HDCBuildToolWindow window, float field)
        {
            bool custom = PlayerSettings.Android.useCustomKeystore;
            int choice = GUI.Toolbar(EditorGUI.PrefixLabel(Row(field), new GUIContent("Keystore")), custom ? 1 : 0, KeystoreNames);
            if (choice != (custom ? 1 : 0))
            {
                PlayerSettings.Android.useCustomKeystore = choice == 1;
                window.MarkStale();
            }

            if (PlayerSettings.Android.useCustomKeystore)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    Rect file = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight, GUILayout.Width(Label + field - 74f));
                    EditorGUI.SelectableLabel(EditorGUI.PrefixLabel(file, new GUIContent("File")), PlayerSettings.Android.keystoreName, EditorStyles.textField);
                    if (GUILayout.Button("Browse", GUILayout.Width(70f)))
                    {
                        HDCBuildChecks.ChooseKeystore();
                        window.MarkStale();
                        GUIUtility.ExitGUI();
                    }

                    GUILayout.FlexibleSpace();
                }

                string alias = EditorGUILayout.DelayedTextField("Key alias", PlayerSettings.Android.keyaliasName ?? "", GUILayout.Width(Label + field));
                if (alias != (PlayerSettings.Android.keyaliasName ?? ""))
                    PlayerSettings.Android.keyaliasName = alias;
                Passwords(window, field);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                HDCAndroidDevice selected = window.SelectedAndroidDevice;
                string[] names = window.AndroidDevices.Select(device => device.Title + (device.Details.Length > 0 ? " · " + device.Details : "")).ToArray();
                DevicePopup(names, window.AndroidDevices.IndexOf(selected), window.ScanningDevices ? "Looking for devices..." : "No device", field,
                    index => window.SelectAndroidDevice(window.AndroidDevices[index]));
                Refresh(window);
            }
        }

        private static void Passwords(HDCBuildToolWindow window, float field)
        {
            Rect rect = EditorGUI.PrefixLabel(Row(field), new GUIContent("Passwords", "Unity keeps them until it closes."));
            float half = (rect.width - 4f) / 2f;
            var store = new Rect(rect.x, rect.y, half, rect.height);
            var key = new Rect(rect.x + half + 4f, rect.y, half, rect.height);
            string storePass = EditorGUI.PasswordField(store, PlayerSettings.Android.keystorePass ?? "");
            string keyPass = EditorGUI.PasswordField(key, PlayerSettings.Android.keyaliasPass ?? "");
            Placeholder(store, PlayerSettings.Android.keystorePass, "keystore password");
            Placeholder(key, PlayerSettings.Android.keyaliasPass, "key password");
            if (storePass == (PlayerSettings.Android.keystorePass ?? "") && keyPass == (PlayerSettings.Android.keyaliasPass ?? ""))
                return;
            PlayerSettings.Android.keystorePass = storePass;
            PlayerSettings.Android.keyaliasPass = keyPass;
            window.MarkStale();
        }

        private static void BundleId(float field)
        {
            string current = HDCIosLocalBuild.BundleId;
            GUI.SetNextControlName(BundleControl);
            string next = EditorGUILayout.DelayedTextField(new GUIContent("Bundle ID", "The app bundle ID on this Mac. Empty: the project bundle ID."), current,
                GUILayout.Width(Label + field));
            if (GUI.GetNameOfFocusedControl() != BundleControl)
            {
                Rect rect = GUILayoutUtility.GetLastRect();
                rect.xMin += EditorGUIUtility.labelWidth + 2f;
                Placeholder(rect, current, PlayerSettings.GetApplicationIdentifier(HDCBuildPlatform.Ios.Named()) + "  (project)");
            }

            if (next != current)
                HDCIosLocalBuild.BundleId = next;
        }

        private static void DevicePopup(string[] names, int current, string empty, float field, Action<int> select)
        {
            if (names.Length == 0)
            {
                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.Popup("Device", 0, new[] { empty }, GUILayout.Width(Label + field));
                return;
            }

            int index = Math.Max(0, current);
            int next = EditorGUILayout.Popup("Device", index, names, GUILayout.Width(Label + field));
            if (next != index || current < 0)
                select(next);
        }

        private static void Refresh(HDCBuildToolWindow window)
        {
            using (new EditorGUI.DisabledScope(window.ScanningDevices))
            {
                var refresh = new GUIContent(EditorGUIUtility.IconContent("Refresh").image, "Look for devices again");
                if (GUILayout.Button(refresh, GUILayout.Width(Button), GUILayout.Height(EditorGUIUtility.singleLineHeight)))
                    window.RefreshDevices();
            }

            GUILayout.FlexibleSpace();
        }

        private static Rect Row(float field) => EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight, GUILayout.Width(Label + field));

        private static void Placeholder(Rect rect, string value, string text)
        {
            if (string.IsNullOrEmpty(value) && Event.current.type == EventType.Repaint)
                GUI.Label(rect, text, placeholderStyle);
        }

        private static void IosButtons(HDCBuildToolWindow window, bool canRun)
        {
            HDCAppleTarget target = HDCBuildState.IosTarget;
            string export = window.IosExportFolder;
            if (HDCXcode.IsBusy)
                EditorGUILayout.HelpBox("Xcode is " + HDCXcode.Activity + ". Follow it in the Xcode window.", MessageType.Info);
            float[] main = Fit(Big, Wide, Wide - 30f);
            using (new EditorGUILayout.HorizontalScope())
            using (new EditorGUI.DisabledScope(HDCXcode.IsBusy))
            {
                using (new EditorGUI.DisabledScope(!canRun))
                {
                    if (Colored("▶  Build and run on " + target.Title(), HDCBuildGui.Green, main[0], "Exports, then Xcode builds, installs and opens the app."))
                        window.Build(HDCBuildAction.BuildAndRun);
                }

                if (Colored("Export and open Xcode", HDCBuildGui.Blue, main[1], "Opens the export in Xcode with the device above as the run destination."))
                    window.Build(HDCBuildAction.ExportAndOpen);
                if (GUILayout.Button(new GUIContent("Export Xcode", "Exports into " + HDCBuildFolders.Display(export)), GUILayout.Width(main[2]), GUILayout.Height(Tall)))
                    window.Build(HDCBuildAction.Build);
                GUILayout.FlexibleSpace();
            }

            float[] files = Fit(Wide - 30f, Wide - 30f, Narrow + 14f);
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(HDCBuildRunner.XcodeProject(export) == null))
                {
                    if (GUILayout.Button("Open Xcode project", GUILayout.Width(files[0])))
                        HDCBuildRunner.OpenXcode(export);
                }

                OpenFolder("Open export folder", export, files[1]);
                Report(HDCBuildPlatform.Ios, files[2]);
                GUILayout.FlexibleSpace();
            }
        }

        private static void AndroidButtons(HDCBuildToolWindow window)
        {
            HDCAndroidDevice selected = window.SelectedAndroidDevice;
            bool ready = selected != null && selected.Ready;
            bool appBundle = window.Rules.androidAppBundle;
            string package = PlayerSettings.GetApplicationIdentifier(HDCBuildPlatform.Android.Named());
            string output = window.Settings.OutputPath(HDCBuildPlatform.Android);
            float[] main = Fit(Big, Wide);
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(!ready && !appBundle))
                {
                    if (Colored("▶  Build and run", HDCBuildGui.Green, main[0],
                            appBundle ? "Builds the AAB; Unity installs it on its Run Device." : "Builds the APK, installs it on the device above and opens it."))
                        window.Build(HDCBuildAction.BuildAndRun);
                }

                if (Colored("Build " + (appBundle ? "AAB" : "APK"), HDCBuildGui.Blue, main[1], "Builds into " + HDCBuildFolders.Display(output)))
                    window.Build(HDCBuildAction.Build);
                GUILayout.FlexibleSpace();
            }

            float[] files = Fit(Wide - 30f, Narrow + 14f);
            using (new EditorGUILayout.HorizontalScope())
            {
                OpenFolder("Open build folder", output, files[0]);
                Report(HDCBuildPlatform.Android, files[1]);
                GUILayout.FlexibleSpace();
            }

            float[] actions = Fit(Narrow, Narrow, Narrow, Narrow, Narrow, Narrow + 14f);
            using (new EditorGUILayout.HorizontalScope())
            using (new EditorGUI.DisabledScope(!ready))
            {
                string apk = window.LatestApk();
                using (new EditorGUI.DisabledScope(apk == null))
                {
                    if (GUILayout.Button(new GUIContent("Install", apk == null ? "Build an APK first." : "Installs " + Path.GetFileName(apk)), GUILayout.Width(actions[0])))
                        window.RunOnDevice("Install", device => HDCAndroidDevices.Install(device.Serial, apk));
                }

                if (GUILayout.Button("Launch", GUILayout.Width(actions[1])))
                    window.RunOnDevice("Launch", device => HDCAndroidDevices.Launch(device.Serial, package));
                if (GUILayout.Button("Stop", GUILayout.Width(actions[2])))
                    window.RunOnDevice("Stop", device => HDCAndroidDevices.Stop(device.Serial, package));
                if (GUILayout.Button("Clear data", GUILayout.Width(actions[3])))
                {
                    if (EditorUtility.DisplayDialog("Clear app data", $"Delete all data of {package} on the device?", "Clear", "Cancel"))
                        window.RunOnDevice("Clear data", device => HDCAndroidDevices.ClearData(device.Serial, package));
                    GUIUtility.ExitGUI();
                }

                if (GUILayout.Button("Uninstall", GUILayout.Width(actions[4])))
                    window.RunOnDevice("Uninstall", device => HDCAndroidDevices.Uninstall(device.Serial, package));
                string mirror = HDCScrcpy.IsRunning ? "Stop mirror" : "Mirror screen";
                if (GUILayout.Button(new GUIContent(mirror, "Shows the device screen with scrcpy."), GUILayout.Width(actions[5])))
                {
                    if (HDCScrcpy.IsRunning)
                        HDCScrcpy.Stop();
                    else
                        window.SetNotice(HDCScrcpy.Start(selected.Serial), HDCScrcpy.Path == null ? MessageType.Warning : MessageType.Info);
                }

                GUILayout.FlexibleSpace();
            }
        }

        internal static float[] FitInto(float room, params float[] widths)
        {
            float space = room - 4f * (widths.Length + 1);
            float total = widths.Sum();
            float scale = total > space && total > 0f ? Mathf.Max(0.5f, space / total) : 1f;
            return widths.Select(width => Mathf.Floor(width * scale)).ToArray();
        }

        private static float[] Fit(params float[] widths) => FitInto(EditorGUIUtility.currentViewWidth - footerStyle.padding.horizontal, widths);

        private static void OpenFolder(string label, string folder, float width)
        {
            using (new EditorGUI.DisabledScope(!Directory.Exists(folder)))
            {
                if (GUILayout.Button(new GUIContent(label, HDCBuildFolders.Display(folder)), GUILayout.Width(width)))
                    HDCBuildFolders.Open(folder);
            }
        }

        private static void Report(HDCBuildPlatform platform, float width)
        {
            HDCBuildResult last = HDCBuildState.LastResult(platform);
            bool exists = last != null && !string.IsNullOrEmpty(last.reportPath) && File.Exists(last.reportPath);
            using (new EditorGUI.DisabledScope(!exists))
            {
                if (GUILayout.Button("Open report", GUILayout.Width(width)))
                    EditorUtility.OpenWithDefaultApp(last.reportPath);
            }
        }

        private static bool Colored(string text, Color color, float width, string tooltip)
        {
            Color old = GUI.backgroundColor;
            GUI.backgroundColor = color;
            bool clicked = GUILayout.Button(new GUIContent(text, tooltip), bigStyle, GUILayout.Width(width), GUILayout.Height(Tall));
            GUI.backgroundColor = old;
            return clicked;
        }

        private static string Duration(double seconds) => TimeSpan.FromSeconds(seconds).ToString(@"m\m\ s\s", CultureInfo.InvariantCulture);

        private static string Size(long bytes) => (bytes / 1048576d).ToString("0.0", CultureInfo.InvariantCulture) + " MB";

        private static void Styles()
        {
            if (footerStyle != null)
                return;
            footerStyle = new GUIStyle { padding = new RectOffset(14, 14, 10, 10), margin = new RectOffset(0, 0, 0, 0) };
            bigStyle = new GUIStyle(GUI.skin.button) { fontStyle = FontStyle.Bold, fontSize = 12 };
            captionStyle = new GUIStyle(EditorStyles.boldLabel) { margin = new RectOffset(0, 0, 0, 4) };
            warningStyle = new GUIStyle(EditorStyles.wordWrappedMiniLabel) { padding = new RectOffset(Mathf.RoundToInt(Label) + 2, 0, 2, 0) };
            warningStyle.normal.textColor = HDCBuildGui.Orange;
            noteStyle = new GUIStyle(warningStyle);
            noteStyle.normal.textColor = HDCBuildGui.Gray;
            placeholderStyle = new GUIStyle(EditorStyles.label) { padding = new RectOffset(3, 3, 1, 0), fontStyle = FontStyle.Italic };
            placeholderStyle.normal.textColor = HDCBuildGui.Gray;
        }
    }
}
