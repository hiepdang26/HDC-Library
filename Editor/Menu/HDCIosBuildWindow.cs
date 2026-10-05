using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace HDC.Ads.Editor
{
    internal sealed class HDCIosBuildWindow : EditorWindow
    {
        private static readonly string[] MacRunTitles = { "Xcode default", "On", "Off" };

        private Vector2 scroll;

        internal static void Open()
        {
            var window = GetWindow<HDCIosBuildWindow>("HDC iOS build");
            window.minSize = new Vector2(460f, 400f);
            window.Show();
        }

        private void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.HelpBox(
                "These settings belong to this computer and this project. Unity keeps them in the UserSettings folder, which is not committed, and they apply from the next Xcode export.",
                MessageType.None);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Local signing", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Signs the export with an Apple team of this computer, such as a free personal team that cannot sign the publisher's bundle ID. An empty field keeps Player Settings. A team turns on automatic signing for the export.",
                MessageType.None);

            HDCIosLocalBuild.TeamId = EditorGUILayout.DelayedTextField("Team ID", HDCIosLocalBuild.TeamId);
            PlayerSettingsValue(PlayerSettings.iOS.appleDeveloperTeamID);
            if (HDCIosLocalBuild.TeamId.Length > 0 && !HDCIosLocalBuild.IsTeamId(HDCIosLocalBuild.TeamId))
                EditorGUILayout.HelpBox("An Apple team ID has 10 capital letters and digits.", MessageType.Warning);

            HDCIosLocalBuild.BundleId = EditorGUILayout.DelayedTextField("Bundle ID", HDCIosLocalBuild.BundleId);
            PlayerSettingsValue(PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.iOS));
            if (HDCIosLocalBuild.BundleId.Length > 0 && !HDCIosLocalBuild.IsBundleId(HDCIosLocalBuild.BundleId))
                EditorGUILayout.HelpBox("A bundle ID looks like com.company.game: letters, digits and hyphens, separated by dots.", MessageType.Warning);

            using (new EditorGUI.DisabledScope(!HDCIosLocalBuild.HasSigning))
            {
                if (GUILayout.Button("Clear local signing"))
                {
                    GUI.FocusControl(null);
                    HDCIosLocalBuild.ClearSigning();
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Run on My Mac (Designed for iPad)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Xcode default leaves the setting as Unity exports it. On lets Xcode run the app on a Mac with Apple silicon, and needs a Device SDK export. Off removes My Mac from the run destinations of Xcode.",
                MessageType.None);
            HDCIosLocalBuild.MacRun = (HDCMacRun)EditorGUILayout.Popup("My Mac", (int)HDCIosLocalBuild.MacRun, MacRunTitles);
            if (HDCIosLocalBuild.MacRun == HDCMacRun.On && PlayerSettings.iOS.sdkVersion == iOSSdkVersion.SimulatorSDK)
                EditorGUILayout.HelpBox("Player Settings export for the Simulator SDK, so the export skips this. Set Target SDK to Device SDK.", MessageType.Warning);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Next export uses", HDCIosLocalBuild.Summary, EditorStyles.wordWrappedLabel);
            EditorGUILayout.EndScrollView();
        }

        private static void PlayerSettingsValue(string value) =>
            EditorGUILayout.LabelField(" ", "Player Settings: " + (string.IsNullOrEmpty(value) ? "(empty)" : value), EditorStyles.miniLabel);
    }
}
