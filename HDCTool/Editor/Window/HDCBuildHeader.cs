using HDC.Ads.Editor;
using UnityEditor;
using UnityEngine;

namespace HDC.Tool.Editor
{
    internal static class HDCBuildHeader
    {
        private static readonly GUIContent ExpandAll = new GUIContent("Expand all", "Opens every card.");
        private static readonly GUIContent CollapseAll = new GUIContent("Collapse all", "Closes every card. A closed card costs nothing to draw.");

        private static GUIStyle tabStyle;
        private static GUIStyle brandStyle;

        internal static void Draw(HDCBuildToolWindow window)
        {
            Styles();
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("HDC Build Tool", brandStyle);
                if (HDCBuildGui.Wide)
                    GUILayout.Label("Build iOS and Android from one window", EditorStyles.miniLabel);
                GUILayout.FlexibleSpace();
                using (new EditorGUI.DisabledScope(HDCBuildGui.AllOpen))
                {
                    if (GUILayout.Button(ExpandAll, EditorStyles.toolbarButton, GUILayout.Width(74f)))
                    {
                        GUI.FocusControl(null);
                        HDCBuildGui.SetAll(true);
                    }
                }

                using (new EditorGUI.DisabledScope(HDCBuildGui.AllClosed))
                {
                    if (GUILayout.Button(CollapseAll, EditorStyles.toolbarButton, GUILayout.Width(80f)))
                    {
                        GUI.FocusControl(null);
                        HDCBuildGui.SetAll(false);
                    }
                }

                GUILayout.Space(6f);
                if (GUILayout.Button("Player settings", EditorStyles.toolbarButton, GUILayout.Width(100f)))
                    SettingsService.OpenProjectSettings("Project/Player");
                if (GUILayout.Button("HDC settings", EditorStyles.toolbarButton, GUILayout.Width(90f)))
                    HDCAdsConfigWindow.OpenCard(-1, null, window.Platform == HDCBuildPlatform.Ios ? HDCAdsConfigWindow.IosPlatform : HDCAdsConfigWindow.AndroidPlatform,
                        "the Build Tool's " + window.Platform.Title() + " tab");
                if (GUILayout.Button(new GUIContent(" Refresh", EditorGUIUtility.IconContent("Refresh").image), EditorStyles.toolbarButton, GUILayout.Width(80f)))
                    window.Refresh();
            }

            GUILayout.Space(10f);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(8f);
                Tab(window, HDCBuildPlatform.Ios, " 1. iOS", "BuildSettings.iPhone.Small", HDCBuildGui.Blue);
                GUILayout.Space(8f);
                Tab(window, HDCBuildPlatform.Android, " 2. Android", "BuildSettings.Android.Small", HDCBuildGui.Green);
                GUILayout.Space(8f);
            }

            GUILayout.Space(12f);
            if (window.Platform.IsActive())
                return;
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(8f);
                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    GUILayout.Label(EditorGUIUtility.IconContent("console.warnicon"), GUILayout.Width(32f), GUILayout.Height(32f));
                    GUILayout.Label($"The active build target is {EditorUserBuildSettings.activeBuildTarget}. Switch before building {window.Platform.Title()}.",
                        EditorStyles.wordWrappedLabel);
                    if (GUILayout.Button("Switch to " + window.Platform.Title(), GUILayout.Width(130f), GUILayout.Height(24f)))
                    {
                        HDCBuildPlatform platform = window.Platform;
                        window.Later(() => EditorUserBuildSettings.SwitchActiveBuildTarget(platform.Group(), platform.Target()));
                    }
                }

                GUILayout.Space(8f);
            }

            GUILayout.Space(12f);
        }

        private static void Tab(HDCBuildToolWindow window, HDCBuildPlatform platform, string title, string icon, Color color)
        {
            bool selected = window.Platform == platform;
            Color old = GUI.backgroundColor;
            if (selected)
                GUI.backgroundColor = color;
            var content = new GUIContent(title, EditorGUIUtility.IconContent(icon).image);
            if (GUILayout.Button(content, tabStyle, GUILayout.Height(34f), GUILayout.MinWidth(120f)) && !selected)
                window.SelectPlatform(platform);
            GUI.backgroundColor = old;
        }

        private static void Styles()
        {
            if (tabStyle != null)
                return;
            tabStyle = new GUIStyle(GUI.skin.button) { fontSize = 13, fontStyle = FontStyle.Bold, fixedHeight = 0f };
            brandStyle = new GUIStyle(EditorStyles.miniBoldLabel) { fontSize = 11, margin = new RectOffset(4, 8, 2, 0) };
        }
    }
}
