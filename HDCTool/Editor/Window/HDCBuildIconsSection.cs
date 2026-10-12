using UnityEditor;
using UnityEngine;

namespace HDC.Tool.Editor
{
    internal static class HDCBuildIconsSection
    {
        internal const float Thumb = 64f;

        internal static void Draw()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(new GUIContent("Default Icon", "Player Settings > Default Icon. iOS and Android use it for every icon size that has no icon of its own."),
                    GUILayout.Width(EditorGUIUtility.labelWidth));
                GUILayout.FlexibleSpace();
                Texture2D current = HDCIcons.DefaultIcon;
                var next = (Texture2D)EditorGUILayout.ObjectField(current, typeof(Texture2D), false, GUILayout.Width(Thumb), GUILayout.Height(Thumb));
                if (next != current)
                    HDCIcons.DefaultIcon = next;
            }
        }
    }
}
