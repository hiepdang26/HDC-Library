using UnityEditor;

namespace HDC.Ads.Editor
{
    [CustomEditor(typeof(HDCAdjust))]
    internal sealed class HDCAdjustInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            if (!HDCAdsActivation.HasAdjust)
            {
                EditorGUILayout.HelpBox("The Adjust Unity SDK 5 is not in the project, so HDCAdjust does nothing. Import it to start Adjust.",
                    MessageType.Warning);
            }
            else if (!serializedObject.FindProperty("startManually").boolValue)
            {
                WarnIfEmpty("androidAppToken", "Android");
                WarnIfEmpty("iosAppToken", "iOS");
            }

            if ((HDCAdjustEnvironment)serializedObject.FindProperty("environment").enumValueIndex == HDCAdjustEnvironment.Auto)
            {
                EditorGUILayout.HelpBox(EditorUserBuildSettings.development
                    ? "Environment Auto: the next build is a Development build, so Adjust runs in Sandbox."
                    : "Environment Auto: the next build is a release build, so Adjust runs in Production.", MessageType.Info);
            }

            DrawDefaultInspector();
        }

        private void WarnIfEmpty(string property, string platform)
        {
            if (string.IsNullOrWhiteSpace(serializedObject.FindProperty(property).stringValue))
                EditorGUILayout.HelpBox("The " + platform + " app token is empty: Adjust does not start on " + platform + ".", MessageType.Warning);
        }
    }
}
