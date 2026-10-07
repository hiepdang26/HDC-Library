using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using HDC.Ads.Composition;
using HDC.Ads.DebugUI;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HDC.Ads.Tests
{
    public class HDCTestScenesTests
    {
        private const string BootScene = "HDCAdsTestBoot";
        private const string GameScene = "HDCAdsTestGame";

        private static string Folder => HDCTestPaths.AssemblyFolder("HDC.Ads.TestScenes");

        [Test]
        public void TheBootSceneStartsAdsAsAGameDoesAndThenOpensTheGameScene()
        {
            InScene(BootScene, roots =>
            {
                Component setup = Single(roots, "HDCAdsSetup");
                Assert.IsTrue(PrefabUtility.IsPartOfPrefabInstance(setup), "HDCAdsSetup is the Setup prefab, as a game adds it");
                var settings = new SerializedObject(setup);
                Assert.AreEqual(GameScene, settings.FindProperty("nextScene").stringValue);
                Assert.IsTrue(settings.FindProperty("googleTestAds").boolValue, "the test scenes ask Google for test ads");

                Component adjust = Single(roots, nameof(HDCAdjust));
                Assert.IsTrue(PrefabUtility.IsPartOfPrefabInstance(adjust), "HDCAdjust is the Adjust prefab");

                var panel = (HDCAdsDebugPanel)Single(roots, nameof(HDCAdsDebugPanel));
                Assert.IsTrue(PrefabUtility.IsPartOfPrefabInstance(panel), "the debug panel is its prefab");
                Assert.IsTrue(new SerializedObject(panel).FindProperty("keepAcrossScenes").boolValue, "the debug panel stays in the game scene");
            });
        }

        [Test]
        public void TheGameSceneHoldsTheTestPanel() => InScene(GameScene, roots => Single(roots, "HDCAdsTestScene"));

        [Test]
        public void TheTestScenesSeeOnlyThePublicApi()
        {
            string[] libraries = { "HDC.Ads", "HDC.Ads.Settings", "HDC.Ads.Setup", "HDC.Ads.Adjust", "HDC.Ads.Debug" };
            Assembly[] loaded = AppDomain.CurrentDomain.GetAssemblies().Where(assembly => libraries.Contains(assembly.GetName().Name)).ToArray();
            Assert.AreEqual(libraries.Length, loaded.Length, "every library the test scenes use is loaded");
            foreach (Assembly assembly in loaded)
            {
                IEnumerable<string> friends = assembly.GetCustomAttributes<InternalsVisibleToAttribute>().Select(friend => friend.AssemblyName);
                CollectionAssert.DoesNotContain(friends, "HDC.Ads.TestScenes", assembly.GetName().Name + " shows its internals to the test scenes");
            }
        }

        [Test]
        public void GameBuildsLeaveTheTestScenesOut()
        {
            string[] assemblies = { "Library/PlayerScriptAssemblies/HDC.Ads.dll", "Library/PlayerScriptAssemblies/HDC.Ads.TestScenes.dll" };

            string[] game = Filter(assemblies, new[] { "Assets/Scenes/Splash.unity" });
            string[] test = Filter(assemblies, new[] { Folder + "/" + BootScene + ".unity", Folder + "/" + GameScene + ".unity", "Assets/Scenes/Splash.unity" });

            CollectionAssert.AreEqual(new[] { assemblies[0] }, game);
            CollectionAssert.AreEqual(assemblies, test);
        }

        [Test]
        public void TheMenuPutsTheTestScenesFirstInTheBuildAndTakesThemOut()
        {
            EditorBuildSettingsScene[] original = EditorBuildSettings.scenes;
            try
            {
                EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/Splash.unity", true) };

                Call("AddToBuild");
                string[] added = EditorBuildSettings.scenes.Select(scene => scene.path).ToArray();
                Call("RemoveFromBuild");

                CollectionAssert.AreEqual(new[] { Folder + "/" + BootScene + ".unity", Folder + "/" + GameScene + ".unity", "Assets/Scenes/Splash.unity" }, added);
                CollectionAssert.AreEqual(new[] { "Assets/Scenes/Splash.unity" }, EditorBuildSettings.scenes.Select(scene => scene.path));
            }
            finally
            {
                EditorBuildSettings.scenes = original;
            }
        }

        [Test]
        public void TestingListsTheGroupsAndPositionsOfTheAppliedConfig()
        {
            var ads = new HDCFakeAds();
            HDCAdsRuntime.Use(ads.Runtime);
            try
            {
                CollectionAssert.IsEmpty(HDCAds.Testing.Groups(HDCAdChannel.ForceAd), "no names before HDCAds is initialized");

                ads.Start(HDCTestConfigs.Ads, HDCTestConfigs.Core);

                CollectionAssert.AreEqual(new[] { "native_gameplay", "native_ui" }, HDCAds.Testing.Groups(HDCAdChannel.ForceAd));
                CollectionAssert.AreEqual(new[] { "gameplay", "ui", "orphan" }, HDCAds.Testing.Positions(HDCAdChannel.ForceAd));
                CollectionAssert.AreEqual(new[] { "gameplay" }, HDCAds.Testing.Positions(HDCAdChannel.ForceAd, "native_gameplay"));
                CollectionAssert.AreEqual(new[] { "popup" }, HDCAds.Testing.Groups(HDCAdChannel.Popup));
                CollectionAssert.AreEqual(new[] { "popup" }, HDCAds.Testing.Positions(HDCAdChannel.Popup));
                CollectionAssert.IsEmpty(HDCAds.Testing.Groups(HDCAdChannel.Banner));
                CollectionAssert.IsEmpty(HDCAds.Testing.Positions(HDCAdChannel.Rewarded));
            }
            finally
            {
                HDCAdsRuntime.Use(HDCAdsRuntime.CreateDefault());
            }
        }

        private static void InScene(string name, Action<GameObject[]> check)
        {
            Scene scene = EditorSceneManager.OpenScene(Folder + "/" + name + ".unity", OpenSceneMode.Additive);
            try
            {
                check(scene.GetRootGameObjects());
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static Component Single(GameObject[] roots, string type)
        {
            Component[] found = roots.Select(root => root.GetComponent(type)).Where(component => component != null).ToArray();
            Assert.AreEqual(1, found.Length, "one " + type + " in the scene");
            return found[0];
        }

        private static string[] Filter(string[] assemblies, string[] scenes) =>
            (string[])TestScenes().GetMethod("Filter", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { assemblies, scenes });

        private static void Call(string method) => TestScenes().GetMethod(method, BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);

        private static Type TestScenes()
        {
            Type type = Type.GetType("HDC.Ads.Editor.HDCTestScenes, HDC.Ads.Editor");
            Assert.IsNotNull(type, "HDCTestScenes is gone");
            return type;
        }
    }
}
