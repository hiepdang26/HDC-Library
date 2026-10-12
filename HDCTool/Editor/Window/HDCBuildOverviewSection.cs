using UnityEditor;
using UnityEngine;

namespace HDC.Tool.Editor
{
    internal static class HDCBuildOverviewSection
    {
        internal static void Draw(HDCBuildToolWindow window)
        {
            HDCBuildPlatform platform = window.Platform;
            using (HDCBuildGui.CardScope card = HDCBuildGui.Card("Overview", "Overview", HDCBuildGui.Blue, "what the next build will be",
                       () => HDCBuildGui.Pill(window.Preset.Title(), HDCBuildGui.Purple, "The preset of the next build")))
            {
                if (!card.Open)
                    return;
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PrefixLabel("Build name");
                    EditorGUILayout.SelectableLabel(HDCBuildNames.Current(platform, window.Preset, window.Rules), EditorStyles.textField,
                        GUILayout.Height(EditorGUIUtility.singleLineHeight));
                }

                HDCBuildFolderRow.Draw(window);
                if (platform == HDCBuildPlatform.Ios)
                    AppendExport(window);
                HDCBuildGui.Divider();
                HDCBuildPresetSection.DrawBody(window);
            }
        }

        private static void AppendExport(HDCBuildToolWindow window)
        {
            bool append = EditorGUILayout.Toggle(new GUIContent("Append to last export", "Keeps your changes in the exported Xcode project when Unity allows it."),
                window.Settings.IosAppendExport);
            if (append == window.Settings.IosAppendExport)
                return;
            Undo.RecordObject(window.Settings, "Append export");
            window.Settings.IosAppendExport = append;
            window.Settings.Save();
        }
    }
}
