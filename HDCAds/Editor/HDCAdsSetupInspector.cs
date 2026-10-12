using System.Linq;
using UnityEditor;
using UnityEngine;

namespace HDC.Ads.Editor
{
    [CustomEditor(typeof(HDCAdsSetup))]
    internal sealed class HDCAdsSetupInspector : UnityEditor.Editor
    {
        internal static readonly string[] BuildToolFields = { "debugLog", "googleTestAds", "googleTestAdUnits" };

        private static readonly string[] Titles = { "Debug log", "Google test ads", "Google test ad units" };

        private static void OpenBuildTool()
        {
            System.Type window = System.AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType("HDC.Tool.Editor.HDCBuildToolWindow", false)).FirstOrDefault(type => type != null);
            System.Reflection.MethodInfo open = window?.GetMethod("Open", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            if (open != null)
                open.Invoke(null, null);
            else
                EditorApplication.ExecuteMenuItem("HDC/Build Tool");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawDefaultInspector();
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Debug log, Google test ads and Google test ad units come from the preset in HDC > Build Tool (Overview). " +
                                    "Each build made there writes them into its own copy of the scene.", MessageType.Info);
            if (GUILayout.Button("Open Build Tool"))
                OpenBuildTool();

            string[] on = BuildToolFields.Where(field => serializedObject.FindProperty(field).boolValue)
                .Select(field => Titles[System.Array.IndexOf(BuildToolFields, field)]).ToArray();
            if (on.Length == 0)
                return;
            EditorGUILayout.HelpBox(string.Join(", ", on) + (on.Length == 1 ? " is" : " are") + " still on in this HDCAdsSetup. Builds from the Build Tool " +
                                    "replace the value; Play Mode and other builds use it.", MessageType.Warning);
            if (!GUILayout.Button("Turn " + (on.Length == 1 ? "it" : "them") + " off"))
                return;
            foreach (string field in BuildToolFields)
                serializedObject.FindProperty(field).boolValue = false;
            serializedObject.ApplyModifiedProperties();
        }
    }
}
