using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace HDC.Tool.Editor
{
    internal static class HDCBuildSdkSection
    {
        private const float RowHeight = 20f;
        private const float Indent = 16f;

        private static IReadOnlyList<HDCSdkVersions.Entry> shownSource;
        private static bool shownIos;
        private static List<HDCSdkVersions.Entry> shown = new List<HDCSdkVersions.Entry>();
        private static GUIContent[] shownValues = new GUIContent[0];
        private static int shownNetworks;

        private static GUIStyle headerStyle;
        private static GUIStyle currentStyle;
        private static GUIStyle groupStyle;
        private static GUIStyle networkStyle;
        private static GUIStyle cellStyle;
        private static GUIStyle declaredStyle;

        internal static void Draw(HDCBuildToolWindow window)
        {
            bool ios = window.Platform == HDCBuildPlatform.Ios;
            Show(window.Sdks, ios);
            List<HDCSdkVersions.Entry> entries = shown;
            int networks = shownNetworks;
            string subtitle = window.Sdks == null ? window.Platform.Title() + " · reading..."
                : window.Platform.Title() + " · " + (networks == 1 ? "1 mediation network" : networks + " mediation networks");
            using (HDCBuildGui.CardScope card = HDCBuildGui.Card("Sdk", "SDK versions", HDCBuildGui.Slate, subtitle))
            {
                if (!card.Open)
                    return;
                if (window.Sdks == null)
                {
                    HDCBuildGui.Note("Reading the SDK versions...");
                    return;
                }
                Styles();
                Header(window.Platform.Title());
                string group = null;
                int row = 0;
                foreach (HDCSdkVersions.Entry entry in entries)
                {
                    if (entry.Group != group)
                    {
                        group = entry.Group;
                        GUILayout.Space(row == 0 ? 2f : 6f);
                        GUILayout.Label(group.ToUpperInvariant(), groupStyle);
                    }

                    Row(entry, ios, row, shownValues[row]);
                    row++;
                }

                GUILayout.Space(6f);
                HDCBuildGui.Note(ios
                    ? "Versions that CocoaPods (Podfile.lock) and Swift Package Manager (Package.resolved) chose in the last export. A gray version is only " +
                      "declared, so export once to see the one in use. Hover a version for its source. Refresh after External Dependency Manager changes them."
                    : "Versions declared in the *Dependencies.xml files; when two declare the same library, the higher one counts. Hover a version for its " +
                      "source. Refresh after External Dependency Manager changes them.");
            }
        }

        private static void Header(string platform)
        {
            Rect rect = GUILayoutUtility.GetRect(1f, RowHeight, GUILayout.ExpandWidth(true));
            (Rect name, Rect version) = Columns(rect);
            GUI.Label(name, "Library", headerStyle);
            GUI.Label(version, platform, currentStyle);
            if (Event.current.type == EventType.Repaint)
                EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), HDCBuildGui.Edge);
        }

        private static void Show(IReadOnlyList<HDCSdkVersions.Entry> source, bool ios)
        {
            if (ReferenceEquals(source, shownSource) && ios == shownIos)
                return;
            shownSource = source;
            shownIos = ios;
            shown = source == null ? new List<HDCSdkVersions.Entry>() : HDCSdkVersions.ForPlatform(source, ios);
            shownNetworks = shown.Count(entry => entry.Network);
            shownValues = shown.Select(entry =>
            {
                string value = HDCSdkVersions.Value(entry, ios);
                string tip = ios ? entry.IosTip : entry.AndroidTip;
                return new GUIContent(value, tip.Length > 0 ? tip : value);
            }).ToArray();
        }

        private static void Row(HDCSdkVersions.Entry entry, bool ios, int index, GUIContent value)
        {
            Rect rect = GUILayoutUtility.GetRect(1f, RowHeight, GUILayout.ExpandWidth(true));
            HDCBuildGui.StripeRow(rect, index);
            (Rect name, Rect version) = Columns(rect);
            if (entry.Sub)
                name.xMin += Indent;
            GUI.Label(name, entry.Name, entry.Network ? networkStyle : EditorStyles.label);
            if (entry.Network)
                return;
            GUI.Label(version, value, ios && entry.IosDeclared ? declaredStyle : cellStyle);
        }

        private static (Rect Name, Rect Version) Columns(Rect rect)
        {
            float nameWidth = Mathf.Clamp(Mathf.Round(rect.width * 0.45f), 170f, 320f);
            return (new Rect(rect.x + 4f, rect.y, nameWidth - 8f, rect.height), new Rect(rect.x + nameWidth, rect.y, rect.width - nameWidth - 6f, rect.height));
        }

        private static void Styles()
        {
            if (headerStyle != null)
                return;
            headerStyle = new GUIStyle(EditorStyles.miniBoldLabel) { alignment = TextAnchor.MiddleLeft };
            headerStyle.normal.textColor = HDCBuildGui.Gray;
            currentStyle = new GUIStyle(headerStyle);
            currentStyle.normal.textColor = HDCBuildGui.Sky;
            groupStyle = new GUIStyle(EditorStyles.miniBoldLabel) { margin = new RectOffset(4, 0, 2, 2) };
            groupStyle.normal.textColor = HDCBuildGui.Slate;
            networkStyle = new GUIStyle(EditorStyles.boldLabel) { clipping = TextClipping.Clip };
            cellStyle = new GUIStyle(EditorStyles.label) { clipping = TextClipping.Clip };
            declaredStyle = new GUIStyle(cellStyle) { fontStyle = FontStyle.Italic };
            declaredStyle.normal.textColor = HDCBuildGui.Gray;
        }
    }
}
