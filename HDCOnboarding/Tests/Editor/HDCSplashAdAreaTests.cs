using System.Linq;
using HDC.Onboarding.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace HDC.Onboarding.Tests
{
    public class HDCSplashAdAreaTests
    {
        [Test]
        public void PortraitAndLandscapeFillTheWidthAtTheBottom()
        {
            var root = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
            try
            {
                var area = new GameObject("Area", typeof(RectTransform)).AddComponent<HDCSplashAdArea>();
                area.transform.SetParent(root.transform, false);
                area.Area.offsetMin = new Vector2(10, 20);
                area.Area.offsetMax = new Vector2(-30, -40);

                area.Apply(true);
                Assert.AreEqual(Vector2.zero, area.Area.anchorMin);
                Assert.AreEqual(new Vector2(1f, 0.075f), area.Area.anchorMax);
                Assert.AreEqual(Vector2.zero, area.Area.offsetMin, "full width, no offsets");
                Assert.AreEqual(Vector2.zero, area.Area.offsetMax);

                area.Apply(false);
                Assert.AreEqual(Vector2.zero, area.Area.anchorMin);
                Assert.AreEqual(new Vector2(1f, 0.175f), area.Area.anchorMax);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ATallerScreenIsPortrait()
        {
            Assert.IsTrue(HDCSplashAdArea.IsPortraitSize(new Vector2(1242, 2688)));
            Assert.IsFalse(HDCSplashAdArea.IsPortraitSize(new Vector2(2688, 1242)));
            Assert.IsFalse(HDCSplashAdArea.IsPortraitSize(new Vector2(1000, 1000)));
        }

        [Test]
        public void EachAreaIsItsOwnPrefab()
        {
            HDCSplashAdArea native = Area(HDCOnboardingPaths.NativeSplashAreaPrefab);
            Assert.AreEqual(HDCSplashAdAreaKind.NativeSplash, native.Kind);
            Assert.IsTrue(native.FollowsOrientation);
            Assert.IsNotNull(native.GetComponent<Image>(), "the strip behind the native ad");

            HDCSplashAdArea adMob = Area(HDCOnboardingPaths.NativeSplashAdMobAreaPrefab);
            Assert.AreEqual(HDCSplashAdAreaKind.NativeSplashAdMob, adMob.Kind);
            Assert.IsFalse(adMob.FollowsOrientation);
            Assert.AreEqual(new Vector2(0.5f, 0f), adMob.Area.anchorMin);
            Assert.AreEqual(new Vector2(1f, 0.9167f), adMob.Area.anchorMax);

            foreach (HDCSplashAdArea area in new[] { native, adMob })
            {
                Canvas canvas = area.transform.root.GetComponent<Canvas>();
                Assert.IsNotNull(canvas, "each prefab has its own canvas");
                Assert.AreEqual(RenderMode.ScreenSpaceOverlay, canvas.renderMode);
                Assert.AreEqual(1, canvas.sortingOrder, "over the splash canvas");
            }
        }

        [Test]
        public void MovingTheAreasTakesThemOutOfTheCanvasAndKeepsTheirLook()
        {
            Scene scene = SceneManager.GetActiveScene();
            var before = new System.Collections.Generic.HashSet<GameObject>(scene.GetRootGameObjects());
            try
            {
                var canvas = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
                new GameObject("Backdrop", typeof(RectTransform)).transform.SetParent(canvas.transform, false);
                var native = new GameObject(HDCSplashAdAreas.NativeAreaName, typeof(RectTransform)).GetComponent<RectTransform>();
                native.SetParent(canvas.transform, false);
                native.gameObject.AddComponent<Image>().color = new Color(0.2f, 0.3f, 0.4f, 0.5f);
                var adMob = new GameObject(HDCSplashAdAreas.AdMobAreaName, typeof(RectTransform)).GetComponent<RectTransform>();
                adMob.SetParent(canvas.transform, false);
                adMob.anchorMin = new Vector2(0.4f, 0.1f);
                adMob.anchorMax = new Vector2(0.9f, 0.8f);
                var splashObject = new GameObject("HDCSplash");
                splashObject.SetActive(false);
                var splash = splashObject.AddComponent<HDCSplash>();
                var fields = new SerializedObject(splash);
                fields.FindProperty("nativeSplashArea").objectReferenceValue = native;
                fields.FindProperty("nativeSplashAdMobArea").objectReferenceValue = adMob;
                fields.ApplyModifiedPropertiesWithoutUndo();

                HDCSplashAdAreas.Move(scene);

                CollectionAssert.AreEqual(new[] { "Backdrop" }, canvas.transform.Cast<Transform>().Select(child => child.name).ToArray());
                fields.Update();
                var newNative = (RectTransform)fields.FindProperty("nativeSplashArea").objectReferenceValue;
                var newAdMob = (RectTransform)fields.FindProperty("nativeSplashAdMobArea").objectReferenceValue;
                Assert.AreEqual(HDCOnboardingPaths.NativeSplashAreaPrefab, PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(newNative));
                Assert.AreEqual(HDCOnboardingPaths.NativeSplashAdMobAreaPrefab, PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(newAdMob));
                Assert.AreEqual(new Color(0.2f, 0.3f, 0.4f, 0.5f), newNative.GetComponent<Image>().color, "the old strip colour is kept");
                Assert.AreEqual(new Vector2(0.4f, 0.1f), newAdMob.anchorMin, "the AdMob area keeps its old rect");
                Assert.AreEqual(new Vector2(0.9f, 0.8f), newAdMob.anchorMax);

                int roots = scene.rootCount;
                StringAssert.Contains("already", HDCSplashAdAreas.Move(scene));
                Assert.AreEqual(roots, scene.rootCount, "moving again changes nothing");
            }
            finally
            {
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    if (!before.Contains(root))
                        Object.DestroyImmediate(root);
                }
            }
        }

        [Test]
        public void TheSplashFindsAnAreaByKindWhenItsFieldIsEmpty()
        {
            var area = new GameObject("Area", typeof(RectTransform)).AddComponent<HDCSplashAdArea>();
            var assigned = new GameObject("Assigned", typeof(RectTransform)).GetComponent<RectTransform>();
            try
            {
                var fields = new SerializedObject(area);
                fields.FindProperty("kind").intValue = (int)HDCSplashAdAreaKind.NativeSplashAdMob;
                fields.ApplyModifiedPropertiesWithoutUndo();

                Assert.AreEqual(area.Area, HDCSplash.AreaFor(null, HDCSplashAdAreaKind.NativeSplashAdMob));
                Assert.IsNull(HDCSplash.AreaFor(null, HDCSplashAdAreaKind.NativeSplash));
                Assert.AreEqual(assigned, HDCSplash.AreaFor(assigned, HDCSplashAdAreaKind.NativeSplashAdMob), "the field wins");
            }
            finally
            {
                Object.DestroyImmediate(area.gameObject);
                Object.DestroyImmediate(assigned.gameObject);
            }
        }

        [Test]
        public void TheNativeBannerAreaFollowsTheOrientation()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HDCOnboardingPaths.NativeBannerPrefab);
            Assert.IsNotNull(prefab, HDCOnboardingPaths.NativeBannerPrefab);
            GameObject copy = Object.Instantiate(prefab);
            try
            {
                HDCSplashAdArea area = copy.GetComponentInChildren<HDCSplashAdArea>(true);
                Assert.IsNotNull(area, "the Banner Area carries HDCSplashAdArea");
                Assert.AreEqual("Banner Area", area.name);
                Assert.AreEqual(HDCSplashAdAreaKind.NativeBanner, area.Kind);
                Assert.IsTrue(area.FollowsOrientation);

                area.Apply(true);
                Assert.AreEqual(Vector2.zero, area.Area.anchorMin);
                Assert.AreEqual(new Vector2(1f, 0.075f), area.Area.anchorMax, "portrait: the bottom 7.5% of the screen");

                area.Apply(false);
                Assert.AreEqual(Vector2.zero, area.Area.anchorMin);
                Assert.AreEqual(new Vector2(1f, 0.175f), area.Area.anchorMax, "landscape: the bottom 17.5% of the screen");
                Assert.AreEqual(Vector2.zero, area.Area.offsetMin);
                Assert.AreEqual(Vector2.zero, area.Area.offsetMax);
            }
            finally
            {
                Object.DestroyImmediate(copy);
            }
        }

        [Test]
        public void ANativeBannerAreaAlwaysSpansTheFullWidth()
        {
            HDCSplashAdArea area = AreaWithSides(HDCSplashAdAreaKind.NativeBanner, out GameObject root);
            try
            {
                area.Apply(true);
                Assert.AreEqual(new Vector2(0f, 0.01f), area.Area.anchorMin, "portrait: from the left edge, at the configured height");
                Assert.AreEqual(new Vector2(1f, 0.075f), area.Area.anchorMax, "portrait: to the right edge");

                area.Apply(false);
                Assert.AreEqual(new Vector2(0f, 0.02f), area.Area.anchorMin, "landscape: from the left edge, at the configured height");
                Assert.AreEqual(new Vector2(1f, 0.175f), area.Area.anchorMax, "landscape: to the right edge");
                Assert.AreEqual(Vector2.zero, area.Area.offsetMin);
                Assert.AreEqual(Vector2.zero, area.Area.offsetMax);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void SplashAreasKeepTheSidesOfTheirAnchors()
        {
            HDCSplashAdArea area = AreaWithSides(HDCSplashAdAreaKind.NativeSplash, out GameObject root);
            try
            {
                area.Apply(true);
                Assert.AreEqual(new Vector2(0.2f, 0.01f), area.Area.anchorMin);
                Assert.AreEqual(new Vector2(0.8f, 0.075f), area.Area.anchorMax);

                area.Apply(false);
                Assert.AreEqual(new Vector2(0.25f, 0.02f), area.Area.anchorMin);
                Assert.AreEqual(new Vector2(0.75f, 0.175f), area.Area.anchorMax);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ABannerAreaIsNeverTakenForASplashArea()
        {
            var area = new GameObject("Banner Area", typeof(RectTransform)).AddComponent<HDCSplashAdArea>();
            try
            {
                var fields = new SerializedObject(area);
                fields.FindProperty("kind").intValue = (int)HDCSplashAdAreaKind.NativeBanner;
                fields.ApplyModifiedPropertiesWithoutUndo();

                Assert.IsNull(HDCSplash.AreaFor(null, HDCSplashAdAreaKind.NativeSplash));
                Assert.IsNull(HDCSplash.AreaFor(null, HDCSplashAdAreaKind.NativeSplashAdMob));
            }
            finally
            {
                Object.DestroyImmediate(area.gameObject);
            }
        }

        private static HDCSplashAdArea AreaWithSides(HDCSplashAdAreaKind kind, out GameObject root)
        {
            root = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
            var area = new GameObject("Area", typeof(RectTransform)).AddComponent<HDCSplashAdArea>();
            area.transform.SetParent(root.transform, false);
            var fields = new SerializedObject(area);
            fields.FindProperty("kind").intValue = (int)kind;
            fields.FindProperty("portraitAnchorMin").vector2Value = new Vector2(0.2f, 0.01f);
            fields.FindProperty("portraitAnchorMax").vector2Value = new Vector2(0.8f, 0.075f);
            fields.FindProperty("landscapeAnchorMin").vector2Value = new Vector2(0.25f, 0.02f);
            fields.FindProperty("landscapeAnchorMax").vector2Value = new Vector2(0.75f, 0.175f);
            fields.ApplyModifiedPropertiesWithoutUndo();
            area.Refresh();
            return area;
        }

        private static HDCSplashAdArea Area(string prefabPath)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Assert.IsNotNull(prefab, prefabPath);
            return prefab.GetComponentInChildren<HDCSplashAdArea>(true);
        }
    }
}
