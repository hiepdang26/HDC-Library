using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace HDC.Tool.Editor
{
    internal static class HDCBuildCheckSection
    {
        private const float LevelWidth = 70f;
        private const float NameWidth = 120f;
        private const float FixWidth = 110f;

        internal static void Draw(HDCBuildToolWindow window)
        {
            List<HDCBuildCheck> checks = window.Checks;
            if (checks == null)
                window.QueueScan();
            int errors = checks?.Count(check => check.Level == HDCCheckLevel.Error) ?? 0;
            int warnings = checks?.Count(check => check.Level == HDCCheckLevel.Warning) ?? 0;
            Color accent = checks == null ? HDCBuildGui.Gray : errors > 0 ? HDCBuildGui.Red : warnings > 0 ? HDCBuildGui.Amber : HDCBuildGui.Green;
            using (HDCBuildGui.CardScope card = HDCBuildGui.Card("Checks", "Validation scan", accent, null, () => Header(window, checks, errors, warnings)))
            {
                if (!card.Open)
                    return;
                if (checks == null)
                {
                    HDCBuildGui.Note("Scanning...");
                    return;
                }

                int row = 0;
                foreach (HDCBuildCheck check in checks.Where(check => check.Level != HDCCheckLevel.Ok))
                    DrawCheck(window, check, row++);

                string[] passed = checks.Where(check => check.Level == HDCCheckLevel.Ok).Select(check => check.Name).ToArray();
                if (passed.Length == 0)
                    return;
                if (row > 0)
                    GUILayout.Space(4f);
                using (new EditorGUILayout.HorizontalScope())
                {
                    HDCBuildGui.Pill(passed.Length + " passed", HDCBuildGui.Green);
                    HDCBuildGui.Note(string.Join(", ", passed));
                }
            }
        }

        private static void Header(HDCBuildToolWindow window, List<HDCBuildCheck> checks, int errors, int warnings)
        {
            if (checks != null)
            {
                HDCBuildGui.Pill(Count(errors, "error"), errors > 0 ? HDCBuildGui.Red : HDCBuildGui.Gray);
                HDCBuildGui.Pill(Count(warnings, "warning"), warnings > 0 ? HDCBuildGui.Amber : HDCBuildGui.Gray);
                GUILayout.Label("Scanned " + window.ScannedAt.ToString("HH:mm:ss", CultureInfo.InvariantCulture), EditorStyles.miniLabel);
            }

            if (GUILayout.Button("Scan", EditorStyles.miniButton, GUILayout.Width(56f)))
                window.Scan();
        }

        private static void DrawCheck(HDCBuildToolWindow window, HDCBuildCheck check, int row)
        {
            Rect line = EditorGUILayout.BeginHorizontal();
            HDCBuildGui.StripeRow(line, row);
            using (new EditorGUILayout.HorizontalScope(GUILayout.Width(LevelWidth)))
                HDCBuildGui.Pill(check.Level.ToString(), HDCBuildGui.LevelColor(check.Level));
            EditorGUILayout.LabelField(check.Name, EditorStyles.boldLabel, GUILayout.Width(NameWidth));
            EditorGUILayout.LabelField(check.Message, EditorStyles.wordWrappedMiniLabel);
            if (check.Fix != null && GUILayout.Button(check.FixLabel ?? "Fix", GUILayout.Width(FixWidth)))
            {
                window.Later(() =>
                {
                    check.Fix();
                    window.Scan();
                });
            }

            EditorGUILayout.EndHorizontal();
            GUILayout.Space(2f);
        }

        private static string Count(int count, string word) => count + " " + word + (count == 1 ? "" : "s");
    }
}
