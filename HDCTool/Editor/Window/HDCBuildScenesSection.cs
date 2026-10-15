using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HDC.Tool.Editor
{
    internal static class HDCBuildScenesSection
    {
        private const float RowHeight = 22f;
        private static readonly Color Missing = new Color(0.92f, 0.35f, 0.32f, 1f);

        private static ReorderableList list;
        private static List<EditorBuildSettingsScene> rows = new List<EditorBuildSettingsScene>();
        private static HDCBuildToolWindow owner;
        private static Facts facts = Read(new EditorBuildSettingsScene[0], File.Exists);
        private static bool listed;
        private static bool changed = true;

        internal static void Draw(HDCBuildToolWindow window)
        {
            Listen();
            if (list == null || owner != window || changed)
                Sync(window);
            string other = window.Platform == HDCBuildPlatform.Ios ? "Android" : "iOS";
            using (HDCBuildGui.CardScope card = HDCBuildGui.Card("Scenes", "Build scenes", HDCBuildGui.Cyan, $"{facts.Enabled} of {rows.Count} on, shared with {other}"))
            {
                if (!card.Open)
                    return;
                list.DoLayoutList();
                AcceptDrops(GUILayoutUtility.GetLastRect());
                HDCBuildGui.Note("Drag the handle to change the order. Drop scenes from the Project window here to add them.");

                GUILayout.Space(6f);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Add open scenes", GUILayout.Width(120f)))
                    {
                        AddOpenScenes();
                        Changed(window);
                    }

                    using (new EditorGUI.DisabledScope(!facts.AnyMissing))
                    {
                        if (GUILayout.Button("Clean missing", GUILayout.Width(110f)))
                        {
                            EditorBuildSettings.scenes = EditorBuildSettings.scenes.Where(scene => File.Exists(scene.path)).ToArray();
                            Changed(window);
                        }
                    }

                    if (GUILayout.Button("Build Settings", GUILayout.Width(110f)))
                        EditorApplication.ExecuteMenuItem("File/Build Settings...");
                    GUILayout.FlexibleSpace();
                }
            }
        }

        internal static void AddOpenScenes()
        {
            var paths = new List<string>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
                paths.Add(SceneManager.GetSceneAt(i).path);
            EditorBuildSettings.scenes = WithScenes(EditorBuildSettings.scenes, paths);
        }

        internal static EditorBuildSettingsScene[] WithScenes(IEnumerable<EditorBuildSettingsScene> scenes, IEnumerable<string> paths)
        {
            var result = new List<EditorBuildSettingsScene>(scenes);
            foreach (string path in paths)
            {
                if (!string.IsNullOrEmpty(path) && result.All(scene => scene.path != path))
                    result.Add(new EditorBuildSettingsScene(path, true));
            }

            return result.ToArray();
        }

        internal static int[] BuildIndexes(IReadOnlyList<EditorBuildSettingsScene> scenes, Func<string, bool> exists) =>
            BuildIndexes(scenes, scenes.Select(scene => exists(scene.path)).ToArray());

        internal static int[] BuildIndexes(IReadOnlyList<EditorBuildSettingsScene> scenes, IReadOnlyList<bool> exists)
        {
            var indexes = new int[scenes.Count];
            int next = 0;
            for (int i = 0; i < scenes.Count; i++)
                indexes[i] = scenes[i].enabled && exists[i] ? next++ : -1;
            return indexes;
        }

        internal static bool Same(IReadOnlyList<EditorBuildSettingsScene> current, IReadOnlyList<EditorBuildSettingsScene> scenes) =>
            current.Count == scenes.Count && current.Zip(scenes, (a, b) => a.path == b.path && a.enabled == b.enabled).All(same => same);

        internal static Facts Read(IReadOnlyList<EditorBuildSettingsScene> scenes, Func<string, bool> fileExists)
        {
            var exists = new bool[scenes.Count];
            var names = new string[scenes.Count];
            var paths = new GUIContent[scenes.Count];
            for (int i = 0; i < scenes.Count; i++)
            {
                string path = scenes[i].path ?? string.Empty;
                exists[i] = fileExists(path);
                names[i] = Path.GetFileNameWithoutExtension(path) + (exists[i] ? "" : " (missing)");
                paths[i] = new GUIContent(path, path);
            }

            return new Facts(exists, BuildIndexes(scenes, exists), names, paths, scenes.Count(scene => scene.enabled));
        }

        private static void Listen()
        {
            if (listed)
                return;
            listed = true;
            EditorBuildSettings.sceneListChanged += () => changed = true;
            EditorApplication.projectChanged += () => changed = true;
        }

        private static void Sync(HDCBuildToolWindow window)
        {
            changed = false;
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            if (list == null || owner != window || !Same(rows, scenes))
                Rebuild(window, scenes);
            facts = Read(rows, File.Exists);
        }

        private static void Rebuild(HDCBuildToolWindow window, IEnumerable<EditorBuildSettingsScene> scenes)
        {
            owner = window;
            rows = scenes.Select(scene => new EditorBuildSettingsScene(scene.path, scene.enabled)).ToList();
            list = new ReorderableList(rows, typeof(EditorBuildSettingsScene), true, false, false, true)
            {
                elementHeight = RowHeight,
                drawElementCallback = (rect, index, active, focused) => DrawRow(rect, index),
                drawNoneElementCallback = rect => GUI.Label(rect, "No scene. Drop scenes from the Project window here.", EditorStyles.miniLabel),
                onReorderCallback = _ => Apply(),
                onRemoveCallback = removed =>
                {
                    if (removed.index >= 0 && removed.index < rows.Count)
                        rows.RemoveAt(removed.index);
                    Apply();
                },
            };
        }

        private static void DrawRow(Rect rect, int index)
        {
            if (index < 0 || index >= rows.Count)
                return;
            if (index >= facts.Count)
                facts = Read(rows, File.Exists);
            EditorBuildSettingsScene scene = rows[index];
            bool exists = facts.Exists[index];
            rect.y += 2f;
            rect.height = EditorGUIUtility.singleLineHeight;
            var toggle = new Rect(rect.x, rect.y, 18f, rect.height);
            var number = new Rect(toggle.xMax + 2f, rect.y, 24f, rect.height);
            var open = new Rect(rect.xMax - 52f, rect.y, 52f, rect.height);
            var name = new Rect(number.xMax + 4f, rect.y, Mathf.Min(170f, Mathf.Max(60f, open.x - number.xMax - 12f)), rect.height);
            var path = new Rect(name.xMax + 8f, rect.y, Mathf.Max(0f, open.x - name.xMax - 14f), rect.height);

            bool on = EditorGUI.Toggle(toggle, scene.enabled);
            if (on != scene.enabled)
            {
                scene.enabled = on;
                Apply();
            }

            int buildIndex = facts.Indexes[index];
            GUI.Label(number, buildIndex >= 0 ? buildIndex.ToString() : "-", EditorStyles.miniLabel);
            Color old = GUI.color;
            if (!exists)
                GUI.color = Missing;
            GUI.Label(name, facts.Names[index], EditorStyles.boldLabel);
            GUI.color = old;
            GUI.Label(path, facts.Paths[index], EditorStyles.miniLabel);
            using (new EditorGUI.DisabledScope(!exists))
            {
                if (GUI.Button(open, "Open", EditorStyles.miniButton))
                {
                    if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                        EditorSceneManager.OpenScene(scene.path);
                    GUIUtility.ExitGUI();
                }
            }
        }

        private static void AcceptDrops(Rect area)
        {
            Event current = Event.current;
            if ((current.type != EventType.DragUpdated && current.type != EventType.DragPerform) || !area.Contains(current.mousePosition))
                return;
            string[] paths = DragAndDrop.objectReferences.OfType<SceneAsset>().Select(AssetDatabase.GetAssetPath).Where(path => !string.IsNullOrEmpty(path)).ToArray();
            if (paths.Length == 0)
                return;
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            if (current.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                EditorBuildSettings.scenes = WithScenes(EditorBuildSettings.scenes, paths);
                Changed(owner);
            }

            current.Use();
        }

        private static void Apply()
        {
            EditorBuildSettings.scenes = rows.Select(scene => new EditorBuildSettingsScene(scene.path, scene.enabled)).ToArray();
            facts = Read(rows, File.Exists);
            owner?.MarkStale();
        }

        private static void Changed(HDCBuildToolWindow window)
        {
            changed = true;
            window?.MarkStale();
        }

        internal sealed class Facts
        {
            internal Facts(bool[] exists, int[] indexes, string[] names, GUIContent[] paths, int enabled)
            {
                Exists = exists;
                Indexes = indexes;
                Names = names;
                Paths = paths;
                Enabled = enabled;
                AnyMissing = Array.IndexOf(exists, false) >= 0;
            }

            internal int Count => Exists.Length;
            internal bool[] Exists { get; }
            internal int[] Indexes { get; }
            internal string[] Names { get; }
            internal GUIContent[] Paths { get; }
            internal int Enabled { get; }
            internal bool AnyMissing { get; }
        }
    }
}
