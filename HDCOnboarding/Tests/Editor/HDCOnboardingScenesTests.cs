using System.Collections.Generic;
using System.Linq;
using HDC.Ads;
using HDC.Onboarding.Editor;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HDC.Onboarding.Tests
{
    public class HDCOnboardingScenesTests
    {
        [Test]
        public void TheSplashSceneIsWired()
        {
            Scene scene = EditorSceneManager.OpenScene(HDCOnboardingPaths.SplashScene, OpenSceneMode.Additive);
            try
            {
                List<MonoBehaviour> components = Components(scene);
                AssertWired(components);
                HDCSplash splash = components.OfType<HDCSplash>().Single();
                Assert.AreEqual(1, splash.ProgressChanged.GetPersistentEventCount());
                Assert.IsInstanceOf<HDCSplashLoadingView>(splash.ProgressChanged.GetPersistentTarget(0));

                var setup = new SerializedObject(components.OfType<HDCAdsSetup>().Single());
                Assert.AreEqual("", setup.FindProperty("nextScene").stringValue, "the splash opens the next scene");
                Assert.IsFalse(setup.FindProperty("startAllChannels").boolValue, "the onboarding loads its ads itself, as the old system did");
                Assert.IsFalse(setup.FindProperty("runAppLaunch").boolValue, "the splash shows the launch ads");
                Assert.AreEqual(1, components.OfType<HDCNoInternet>().Count());

                var splashFields = new SerializedObject(splash);
                AssertArea((RectTransform)splashFields.FindProperty("nativeSplashArea").objectReferenceValue, HDCSplashAdAreaKind.NativeSplash,
                    HDCOnboardingPaths.NativeSplashAreaPrefab);
                AssertArea((RectTransform)splashFields.FindProperty("nativeSplashAdMobArea").objectReferenceValue, HDCSplashAdAreaKind.NativeSplashAdMob,
                    HDCOnboardingPaths.NativeSplashAdMobAreaPrefab);
                Transform canvas = scene.GetRootGameObjects().Single(root => root.name == "Canvas").transform;
                CollectionAssert.AreEqual(new[] { "Backdrop", "Header" }, canvas.Cast<Transform>().Select(child => child.name).ToArray(),
                    "the splash canvas only holds the backdrop and the header with the loading bar");

                var banner = (RectTransform)new SerializedObject(components.OfType<HDCNativeBannerLoader>().Single()).FindProperty("bannerArea").objectReferenceValue;
                var bannerArea = banner.GetComponent<HDCSplashAdArea>();
                Assert.IsNotNull(bannerArea, "the banner area follows the orientation like the splash areas");
                Assert.AreEqual(HDCSplashAdAreaKind.NativeBanner, bannerArea.Kind);
                Assert.IsTrue(bannerArea.FollowsOrientation);

                SerializedProperty tracked = new SerializedObject(components.OfType<HDCAdTracking>().Single()).FindProperty("trackedAds");
                CollectionAssert.AreEqual(new[] { "splash", "native_splash", "native_intro1", "native_intro2", "native_intro_full" },
                    Enumerable.Range(0, tracked.arraySize).Select(i => tracked.GetArrayElementAtIndex(i).stringValue).ToArray());
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void TheIntroSceneIsWired()
        {
            Scene scene = EditorSceneManager.OpenScene(HDCOnboardingPaths.IntroScene, OpenSceneMode.Additive);
            try
            {
                List<MonoBehaviour> components = Components(scene);
                AssertWired(components);
                var intro = new SerializedObject(components.OfType<HDCIntro>().Single());
                SerializedProperty views = intro.FindProperty("views");
                Assert.AreEqual(3, views.arraySize);
                var types = new[] { HDCIntroType.AL, HDCIntroType.AR, HDCIntroType.PT };
                for (int i = 0; i < types.Length; i++)
                {
                    var view = (HDCIntroView)views.GetArrayElementAtIndex(i).objectReferenceValue;
                    Assert.IsTrue(view.CanRender(new HDCIntroPage { IntroType = types[i] }), view.name);
                    Assert.IsFalse(view.gameObject.activeSelf, view.name + " starts hidden");
                }

                Assert.AreEqual(3, components.OfType<HDCIntroView>().Count(), "only the AL, AR and PT pages");
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static void AssertArea(RectTransform area, HDCSplashAdAreaKind kind, string prefabPath)
        {
            Assert.IsNotNull(area, kind.ToString());
            Assert.AreEqual(kind, area.GetComponent<HDCSplashAdArea>().Kind);
            Assert.AreEqual(prefabPath, PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(area), "the area comes from its own prefab");
            Assert.IsNull(area.root.parent);
            Assert.AreNotEqual("Canvas", area.root.name);
        }

        private static List<MonoBehaviour> Components(Scene scene) =>
            scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<MonoBehaviour>(true)).ToList();

        private static void AssertWired(IEnumerable<MonoBehaviour> components)
        {
            foreach (MonoBehaviour component in components)
            {
                Assert.IsNotNull(component, "a script is missing");
                if (component is TMP_Text text)
                {
                    Assert.IsNotNull(text.font, text.name + " has no font asset: import TMP Essential Resources");
                    continue;
                }

                if (component.GetType().Namespace != typeof(HDCSplash).Namespace)
                    continue;
                SerializedProperty property = new SerializedObject(component).GetIterator();
                while (property.NextVisible(true))
                {
                    if (property.propertyType == SerializedPropertyType.ObjectReference && !property.propertyPath.Contains("m_PersistentCalls"))
                        Assert.IsNotNull(property.objectReferenceValue, component.GetType().Name + " on " + component.name + ": " + property.propertyPath);
                }
            }
        }
    }
}
