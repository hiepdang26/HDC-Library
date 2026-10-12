using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace HDC.Onboarding.Editor
{
    internal static class HDCSplashAdAreas
    {
        internal const string NativeAreaName = "Native Splash Area";
        internal const string AdMobAreaName = "Native Splash AdMob Area";
        private const string UndoName = "Move splash ad areas into prefabs";

        internal static HDCSplash FindSplash(Scene scene) =>
            !scene.IsValid() || !scene.isLoaded
                ? null
                : scene.GetRootGameObjects().Select(root => root.GetComponentInChildren<HDCSplash>(true)).FirstOrDefault(found => found != null);

        internal static string Move(Scene scene)
        {
            HDCSplash splash = FindSplash(scene);
            if (splash == null)
                throw new InvalidOperationException("Scene " + scene.name + " has no HDCSplash.");

            var notes = new List<string>();
            var serialized = new SerializedObject(splash);
            Place(scene, serialized, "nativeSplashArea", HDCOnboardingPaths.NativeSplashAreaPrefab, notes);
            Place(scene, serialized, "nativeSplashAdMobArea", HDCOnboardingPaths.NativeSplashAdMobAreaPrefab, notes);
            serialized.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(scene);
            return string.Join(" ", notes);
        }

        private static void Place(Scene scene, SerializedObject splash, string field, string prefabPath, List<string> notes)
        {
            SerializedProperty property = splash.FindProperty(field);
            var old = property.objectReferenceValue as RectTransform;
            string prefabName = Path.GetFileNameWithoutExtension(prefabPath);
            if (old != null && PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(old) == prefabPath)
            {
                notes.Add($"{field} already is the area of {prefabName}.");
                return;
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
                throw new InvalidOperationException(prefabPath + " is missing.");
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            Undo.RegisterCreatedObjectUndo(instance, UndoName);
            HDCSplashAdArea area = instance.GetComponentInChildren<HDCSplashAdArea>(true);
            property.objectReferenceValue = area.Area;
            if (old == null)
            {
                notes.Add($"{field} is now the area of {prefabName}.");
                return;
            }

            CopyLook(old, area);
            if (PrefabUtility.IsPartOfPrefabInstance(old))
            {
                notes.Add($"{field} is now the area of {prefabName}; the old {old.name} is part of another prefab, so it stays: remove it by hand.");
                return;
            }

            string oldPath = PathOf(old);
            Undo.DestroyObjectImmediate(old.gameObject);
            notes.Add($"{field} is now the area of {prefabName}; removed {oldPath}.");
        }

        private static void CopyLook(RectTransform old, HDCSplashAdArea area)
        {
            var oldImage = old.GetComponent<Image>();
            var image = area.GetComponent<Image>();
            if (oldImage != null && image != null && oldImage.color != image.color)
            {
                Undo.RecordObject(image, UndoName);
                image.color = oldImage.color;
                PrefabUtility.RecordPrefabInstancePropertyModifications(image);
            }

            if (area.FollowsOrientation)
                return;
            RectTransform rect = area.Area;
            Undo.RecordObject(rect, UndoName);
            rect.anchorMin = old.anchorMin;
            rect.anchorMax = old.anchorMax;
            rect.pivot = old.pivot;
            rect.anchoredPosition = old.anchoredPosition;
            rect.sizeDelta = old.sizeDelta;
            PrefabUtility.RecordPrefabInstancePropertyModifications(rect);
        }

        private static string PathOf(Transform transform)
        {
            var names = new List<string>();
            for (Transform current = transform; current != null; current = current.parent)
                names.Insert(0, current.name);
            return string.Join("/", names);
        }
    }
}
