using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace HDC.Ads.Editor
{
    [CustomEditor(typeof(HDCAdsSettings))]
    internal sealed class HDCAdsSettingsInspector : UnityEditor.Editor
    {
        private GUIStyle statusStyle;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.HelpBox("Ads config, ad core, custom keys and country mode. Edit them in the HDC Ads Settings window. Double-click the asset " +
                                    "to open it too.", MessageType.None);
            if (GUILayout.Button("Open HDC Ads Settings", GUILayout.Height(30f)))
                HDCAdsConfigWindow.OpenCard(-1, (HDCAdsSettings)target);
            EditorGUILayout.Space();
            if (statusStyle == null)
                statusStyle = new GUIStyle(EditorStyles.miniBoldLabel) { alignment = TextAnchor.MiddleRight };
            int platform = HDCAdsConfigWindow.ActivePlatform;
            EditorGUILayout.LabelField("Platform", HDCAdsConfigWindow.PlatformName(platform) + " (active build target)", EditorStyles.miniLabel);
            for (int card = 0; card < HDCAdsConfigWindow.CardCount; card++)
            {
                (string text, Color color) = HDCAdsConfigWindow.SavedStatus(serializedObject, card, platform);
                Rect row = EditorGUILayout.GetControlRect(false, 22f);
                if (Event.current.type == EventType.Repaint)
                    EditorGUI.DrawRect(new Rect(row.x, row.y + 3f, 4f, row.height - 6f), HDCAdsConfigWindow.CardAccent(card));
                GUI.Label(new Rect(row.x + 12f, row.y, row.width - 240f, row.height), HDCAdsConfigWindow.CardTitle(card), EditorStyles.boldLabel);
                Color old = GUI.color;
                GUI.color = color;
                GUI.Label(new Rect(row.xMax - 236f, row.y, 180f, row.height), text, statusStyle);
                GUI.color = old;
                if (GUI.Button(new Rect(row.xMax - 50f, row.y + 2f, 50f, row.height - 4f), "Edit", EditorStyles.miniButton))
                    HDCAdsConfigWindow.OpenCard(card, (HDCAdsSettings)target);
            }
        }

        [OnOpenAsset(0)]
        private static bool OpenAsset(int instanceId, int line)
        {
            if (!(EditorUtility.InstanceIDToObject(instanceId) is HDCAdsSettings settings))
                return false;
            HDCAdsConfigWindow.OpenCard(-1, settings);
            return true;
        }
    }
}
