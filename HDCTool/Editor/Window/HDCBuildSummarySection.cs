using System;
using UnityEditor;
using UnityEngine;

namespace HDC.Tool.Editor
{
    internal static class HDCBuildSummarySection
    {
        private const string AppCard = "Summary.App";
        private const string AppNameControl = "HDC.Build.AppName";

        private static readonly string[] OrientationNames = { "Portrait", "Landscape" };

        private static GUIStyle placeholderStyle;

        internal static void DrawInfo(HDCBuildToolWindow window)
        {
            EditorGUI.BeginChangeCheck();
            App(window);
            if (EditorGUI.EndChangeCheck())
                window.MarkStale();
        }

        internal static void DrawBuild(HDCBuildToolWindow window) => ChangeLog(window);

        internal static string ApiLevel(AndroidSdkVersions version) =>
            version == AndroidSdkVersions.AndroidApiLevelAuto ? "Automatic (highest installed)" : version.ToString().Replace("AndroidApiLevel", "API ");

        private static void App(HDCBuildToolWindow window)
        {
            bool ios = window.Platform == HDCBuildPlatform.Ios;
            using (HDCBuildGui.CardScope card = HDCBuildGui.Card(AppCard, "App", HDCBuildGui.Teal, "Player Settings"))
            {
                if (!card.Open)
                    return;
                Grid(
                    () => AppName(window),
                    () => Text("Product name", PlayerSettings.productName, value => PlayerSettings.productName = value),
                    () => Text("Company name", PlayerSettings.companyName, value => PlayerSettings.companyName = value),
                    () => Text(ios ? "Bundle ID" : "Package name", PlayerSettings.GetApplicationIdentifier(window.Platform.Named()),
                        value => PlayerSettings.SetApplicationIdentifier(window.Platform.Named(), value)),
                    () => Counter("Version", PlayerSettings.bundleVersion, value => PlayerSettings.bundleVersion = value,
                        "Adds 1 to the last number of the version, as 1.2.3 becomes 1.2.4."),
                    () =>
                    {
                        if (ios)
                            Counter("Build number", PlayerSettings.iOS.buildNumber, value => PlayerSettings.iOS.buildNumber = value, "Adds 1 to the build number.");
                        else
                            VersionCode();
                    },
                    () =>
                    {
                        if (ios)
                            Text("Minimum iOS", PlayerSettings.iOS.targetOSVersionString, value => PlayerSettings.iOS.targetOSVersionString = value.Trim());
                        else
                            HDCBuildGui.Row("Minimum API", ApiLevel(PlayerSettings.Android.minSdkVersion));
                    },
                    () =>
                    {
                        if (ios)
                            PodsMinimum(window);
                        else
                            HDCBuildGui.Row("Target API", ApiLevel(PlayerSettings.Android.targetSdkVersion));
                    },
                    () => Orientation(window));
                HDCBuildGui.Note("App name is the name under the icon on the device. Empty: the product name.");
                HDCOrientation orientation = HDCOrientations.Current;
                HDCBuildGui.Note(orientation == HDCOrientation.Other
                    ? "Orientation: the app now turns to every side (Player Settings). Pick Portrait or Landscape to fix it."
                    : "Orientation is shared by iOS and Android. Landscape turns between landscape left and right.");
                if (ios)
                    HDCBuildGui.Note(window.Settings.PodsFollowMinimumIos
                        ? "Pods minimum: every export adds a post_install step to the Podfile that raises each pod below Minimum iOS to it, so nothing needs changing by hand in Xcode."
                        : "Pods minimum is off: each pod keeps the minimum iOS of its podspec.");
                HDCBuildGui.Divider();
                HDCBuildIconsSection.Draw();
            }
        }

        private static void ChangeLog(HDCBuildToolWindow window)
        {
            string subtitle = window.Rules.requireChangeLog ? "this preset expects one" : "optional for this preset";
            using (HDCBuildGui.CardScope card = HDCBuildGui.Card(HDCBuildChecks.ChangeLogFoldout, "Change log", HDCBuildGui.Yellow, subtitle, null, false))
            {
                if (!card.Open)
                    return;
                string log = EditorGUILayout.TextArea(window.Settings.ChangeLog, GUILayout.MinHeight(64f));
                if (log == window.Settings.ChangeLog)
                    return;
                Undo.RecordObject(window.Settings, "Change log");
                window.Settings.ChangeLog = log;
                EditorUtility.SetDirty(window.Settings);
            }
        }

        private static void Grid(params Action[] cells)
        {
            using (new HDCBuildGui.LabelScope(HDCBuildGui.ColumnLabelWidth))
            {
                for (int i = 0; i < cells.Length; i += 2)
                {
                    Action right = i + 1 < cells.Length ? cells[i + 1] : () => { };
                    HDCBuildGui.Pair(cells[i], right);
                }
            }
        }

        private static void AppName(HDCBuildToolWindow window)
        {
            string current = window.Settings.AppName;
            GUI.SetNextControlName(AppNameControl);
            string next = EditorGUILayout.DelayedTextField(new GUIContent("App name", "The name under the app icon on the device. Empty: the product name."), current);
            if (current.Length == 0 && Event.current.type == EventType.Repaint && GUI.GetNameOfFocusedControl() != AppNameControl)
            {
                Rect field = GUILayoutUtility.GetLastRect();
                field.xMin += EditorGUIUtility.labelWidth + 2f;
                GUI.Label(field, PlayerSettings.productName, Placeholder());
            }

            if (next == current)
                return;
            Undo.RecordObject(window.Settings, "App name");
            window.Settings.AppName = next.Trim();
            window.Settings.Save();
        }

        private static void Orientation(HDCBuildToolWindow window)
        {
            HDCOrientation current = HDCOrientations.Current;
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(new GUIContent("Orientation", "Default orientation of the app, from Player Settings > Resolution and Presentation."),
                    GUILayout.Width(EditorGUIUtility.labelWidth));
                int index = current == HDCOrientation.Portrait ? 0 : current == HDCOrientation.Landscape ? 1 : -1;
                int next = GUILayout.Toolbar(index, OrientationNames);
                if (next == index || next < 0)
                    return;
                HDCOrientations.Apply((HDCOrientation)next);
                window.MarkStale();
            }
        }

        private static void PodsMinimum(HDCBuildToolWindow window)
        {
            bool current = window.Settings.PodsFollowMinimumIos;
            using (new EditorGUILayout.HorizontalScope())
            {
                bool next = EditorGUILayout.Toggle(new GUIContent("Pods minimum", "Raises every pod below Minimum iOS to it after pod install."), current,
                    GUILayout.Width(EditorGUIUtility.labelWidth + 18f));
                GUILayout.Label(next ? "same as Minimum iOS" : "each pod keeps its own", EditorStyles.miniLabel);
                if (next == current)
                    return;
                Undo.RecordObject(window.Settings, "Pods minimum");
                window.Settings.PodsFollowMinimumIos = next;
                window.Settings.Save();
            }
        }

        private static void Text(string label, string value, Action<string> set)
        {
            string current = value ?? "";
            string next = EditorGUILayout.DelayedTextField(label, current);
            if (next != current)
                set(next);
        }

        private static void Counter(string label, string value, Action<string> set, string tooltip)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                Text(label, value, set);
                if (HDCBuildGui.Small("+1", tooltip))
                {
                    GUI.FocusControl(null);
                    set(HDCBuildNames.NextNumber(value));
                }
            }
        }

        private static void VersionCode()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                int code = EditorGUILayout.DelayedIntField("Version code", PlayerSettings.Android.bundleVersionCode);
                if (code != PlayerSettings.Android.bundleVersionCode)
                    PlayerSettings.Android.bundleVersionCode = code;
                if (HDCBuildGui.Small("+1", "Adds 1 to the version code."))
                {
                    GUI.FocusControl(null);
                    PlayerSettings.Android.bundleVersionCode++;
                }
            }
        }

        private static GUIStyle Placeholder()
        {
            if (placeholderStyle != null)
                return placeholderStyle;
            placeholderStyle = new GUIStyle(EditorStyles.label) { padding = new RectOffset(3, 3, 1, 0), fontStyle = FontStyle.Italic };
            placeholderStyle.normal.textColor = HDCBuildGui.Gray;
            return placeholderStyle;
        }
    }
}
