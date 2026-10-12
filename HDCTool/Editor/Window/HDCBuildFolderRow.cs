using System.IO;
using UnityEditor;
using UnityEngine;

namespace HDC.Tool.Editor
{
    internal static class HDCBuildFolderRow
    {
        internal static void Draw(HDCBuildToolWindow window)
        {
            string folder = HDCBuildFolders.Base;
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel("Build folder");
                EditorGUILayout.SelectableLabel(HDCBuildFolders.Display(folder), EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
                using (new EditorGUI.DisabledScope(!Directory.Exists(folder)))
                {
                    if (GUILayout.Button(new GUIContent("Open", folder), GUILayout.Width(60f)))
                        HDCBuildFolders.Open(folder);
                }

                if (GUILayout.Button("Set", GUILayout.Width(60f)))
                {
                    Choose(window, folder);
                    GUIUtility.ExitGUI();
                }

                using (new EditorGUI.DisabledScope(HDCBuildFolders.IsDefault))
                {
                    string defaultFolder = HDCBuildFolders.DefaultBase(HDCBuildFolders.ProjectRoot);
                    if (GUILayout.Button(new GUIContent("Default", HDCBuildFolders.Display(defaultFolder)), GUILayout.Width(64f)))
                    {
                        HDCBuildFolders.UseDefault();
                        window.MarkStale();
                    }
                }
            }

            HDCBuildGui.Hint((window.Platform == HDCBuildPlatform.Ios
                ? "The Xcode project is exported straight into this folder."
                : "APK and AAB files go to its " + HDCBuildFolders.AndroidFolderName + " folder.") + " Saved on this computer only, never inside the Unity project.");
        }

        private static void Choose(HDCBuildToolWindow window, string current)
        {
            string start = Directory.Exists(current) ? current : Path.GetDirectoryName(current);
            if (string.IsNullOrEmpty(start) || !Directory.Exists(start))
                start = Path.GetDirectoryName(HDCBuildFolders.ProjectRoot) ?? "";
            string chosen = EditorUtility.OpenFolderPanel("Choose the build folder", start, "");
            if (string.IsNullOrEmpty(chosen))
                return;
            string problem = HDCBuildFolders.Choose(chosen);
            if (problem != null)
                EditorUtility.DisplayDialog("HDC Build Tool", problem, "OK");
            window.MarkStale();
        }
    }
}
