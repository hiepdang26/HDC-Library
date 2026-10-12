using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HDC.Ads.Editor
{
    internal static class HDCAppleTeamField
    {
        private const string ManualKey = "HDC.iOS.TeamManual";
        private const string NoneTitle = "None";
        private const string OtherTitle = "Other team ID…";
        private const string AccountTitle = "Add an Account…";

        internal static string Draw(string label, string teamId, IReadOnlyList<HDCAppleTeam> teams, Action refresh, float fieldWidth = 0f)
        {
            string current = (teamId ?? "").Trim();
            var titles = new List<string> { NoneTitle };
            var ids = new List<string> { "" };
            foreach (HDCAppleTeam team in teams ?? Array.Empty<HDCAppleTeam>())
            {
                if (ids.Contains(team.Id))
                    continue;
                string title = MenuText(team.Name.Length > 0 ? team.Name : team.Id);
                titles.Add(titles.Contains(title) ? MenuText(team.Title) : title);
                ids.Add(team.Id);
            }

            bool known = ids.Contains(current);
            if (!known)
            {
                titles.Add(MenuText(current) + " (not in Xcode)");
                ids.Add(current);
            }

            int other = titles.Count;
            titles.Add(OtherTitle);
            int account = titles.Count;
            titles.Add(AccountTitle);

            string result = current;
            int selected = ids.IndexOf(current);
            GUILayoutOption[] width = fieldWidth > 0f ? new[] { GUILayout.Width(EditorGUIUtility.labelWidth + fieldWidth) } : Array.Empty<GUILayoutOption>();
            using (new EditorGUILayout.HorizontalScope())
            {
                int next = EditorGUILayout.Popup(label, selected, titles.ToArray(), width);
                if (next == other)
                {
                    SessionState.SetBool(ManualKey, true);
                }
                else if (next == account)
                {
                    EditorUtility.DisplayDialog("Add an Apple account",
                        "Xcode opens now. Add your Apple ID in Xcode > Settings > Accounts, then press the refresh button next to Team.", "OK");
                    HDCAppleTeams.OpenXcode();
                    GUIUtility.ExitGUI();
                }
                else if (next != selected && next >= 0 && next < ids.Count)
                {
                    result = ids[next];
                    SessionState.SetBool(ManualKey, false);
                }

                var reload = new GUIContent(EditorGUIUtility.IconContent("Refresh").image, "Read the teams again from Xcode > Settings > Accounts.");
                if (GUILayout.Button(reload, GUILayout.Width(28f), GUILayout.Height(EditorGUIUtility.singleLineHeight)))
                    refresh?.Invoke();
                if (fieldWidth > 0f)
                    GUILayout.FlexibleSpace();
            }

            if (!known || SessionState.GetBool(ManualKey, false))
                result = EditorGUILayout.DelayedTextField(new GUIContent(" ", "A team ID from developer.apple.com > Membership."), result, width).Trim();
            return result;
        }

        private static string MenuText(string text) => (text ?? "").Replace('/', '∕');
    }
}
