using HDC.Ads;
using UnityEditor;
using UnityEngine;

namespace HDC.Tool.Editor
{
    internal static class HDCBuildPresetSection
    {
        private static readonly HDCBuildPreset[] Presets = { HDCBuildPreset.TestDebug, HDCBuildPreset.Release };

        internal static void DrawBody(HDCBuildToolWindow window)
        {
            HDCBuildPlatform platform = window.Platform;
            using (new EditorGUILayout.HorizontalScope())
            {
                HDCBuildGui.Label("Preset");
                for (int i = 0; i < Presets.Length; i++)
                {
                    if (HDCBuildGui.Segment(Presets[i].Title(), window.Preset == Presets[i], i, Presets.Length, HDCBuildGui.Purple))
                        window.SelectPreset(Presets[i]);
                }

                GUILayout.FlexibleSpace();
            }

            HDCBuildGui.Hint(window.Rules.Describe(platform));
            GUILayout.Space(4f);
            Rules(window, platform);
            HDCBuildGui.Divider();
            using (new EditorGUILayout.HorizontalScope())
            {
                HDCBuildGui.Note(
                    $"The preset rules are saved in {HDCBuildSettings.AssetPath} and shared through git. A build applies them to its own copy of the scenes; your scenes stay as they are.");
                if (GUILayout.Button("Reset to defaults", GUILayout.Width(120f)))
                {
                    Undo.RecordObject(window.Settings, "Reset preset rules");
                    window.Settings.ResetRules(window.Preset);
                    window.Settings.Save();
                    window.MarkStale();
                }
            }
        }

        private static void Rules(HDCBuildToolWindow window, HDCBuildPlatform platform)
        {
            HDCBuildRules rules = window.Rules;
            bool testAds = rules.googleTestAds;
            bool testUnits = rules.googleTestAdUnits;
            bool panel = rules.debugPanel;
            bool log = rules.debugLog;
            bool development = rules.developmentBuild;
            bool bundle = rules.androidAppBundle;
            bool changeLog = rules.requireChangeLog;
            EditorGUI.BeginChangeCheck();
            HDCBuildGui.Pair(() =>
            {
                testAds = EditorGUILayout.Toggle("Google test ads", testAds);
                panel = EditorGUILayout.Toggle("Debug panel", panel);
                development = EditorGUILayout.Toggle("Development build", development);
                if (platform == HDCBuildPlatform.Android)
                    bundle = EditorGUILayout.Toggle("App bundle (AAB)", bundle);
            }, () =>
            {
                testUnits = EditorGUILayout.Toggle("Test ad units", testUnits);
                log = EditorGUILayout.Toggle("Debug log", log);
                changeLog = EditorGUILayout.Toggle("Require change log", changeLog);
            });
            var adjust = (HDCAdjustEnvironment)EditorGUILayout.EnumPopup("Adjust environment", rules.adjustEnvironment, HDCBuildGui.FieldWidth(HDCBuildGui.MediumField));
            if (!EditorGUI.EndChangeCheck())
                return;
            Undo.RecordObject(window.Settings, "Preset rules");
            rules.googleTestAds = testAds;
            rules.googleTestAdUnits = testUnits;
            rules.debugPanel = panel;
            rules.debugLog = log;
            rules.adjustEnvironment = adjust;
            rules.developmentBuild = development;
            rules.androidAppBundle = bundle;
            rules.requireChangeLog = changeLog;
            window.Settings.Save();
            window.MarkStale();
        }
    }
}
