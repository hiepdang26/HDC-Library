using System.Collections;
using System.Reflection;
using HDC.Ads.DebugUI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace HDC.Ads.Tests
{
    public class HDCSetupPrefabTests : HDCPlayModeTest
    {
        private const string DontDestroyOnLoadScene = "DontDestroyOnLoad";

        [SetUp]
        public void ForgetAdjust() => HDCAdjust.ResetState();

        [TearDown]
        public void ForgetAdjustAgain() => HDCAdjust.ResetState();

        [Test]
        public void TheSetupPrefabCarriesHDCAdjustAndTheDebugPanel()
        {
            GameObject setup = SetupPrefab();

            HDCAdjust[] adjusts = setup.GetComponentsInChildren<HDCAdjust>(true);
            HDCAdsDebugPanel[] panels = setup.GetComponentsInChildren<HDCAdsDebugPanel>(true);

            Assert.AreEqual(1, adjusts.Length, "one HDCAdjust");
            Assert.AreEqual(setup.transform, adjusts[0].transform.parent);
            Assert.AreEqual(HDCAdjustTests.Prefab(), PrefabUtility.GetCorrespondingObjectFromSource(adjusts[0].gameObject),
                "the HDCAdjust prefab itself, so its updates reach every game");
            Assert.AreEqual(1, panels.Length, "one debug panel");
            Assert.AreEqual(setup.transform, panels[0].transform.parent);
            Assert.AreEqual(HDCTestPaths.DebugPanelPrefab(), PrefabUtility.GetCorrespondingObjectFromSource(panels[0].gameObject),
                "the debug panel prefab itself");
            Assert.IsTrue(new SerializedObject(panels[0]).FindProperty("keepAcrossScenes").boolValue, "the panel outlives the first scene");
        }

        [UnityTest]
        public IEnumerator ThePanelAndHDCAdjustInsideSetupLeaveItToOutliveTheFirstScene()
        {
            yield return new EnterPlayMode();
            GameObject setup = Inactive("HDCAdsSetup");
            HDCAdsDebugPanel panel = Object.Instantiate(HDCTestPaths.DebugPanelPrefab(), setup.transform).GetComponent<HDCAdsDebugPanel>();
            Set(panel, "keepAcrossScenes", true);
            HDCAdjust adjust = Adjust(setup, false);

            setup.SetActive(true);
            yield return null;

            Assert.IsNull(panel.transform.parent, "the panel is a root object");
            Assert.AreEqual(DontDestroyOnLoadScene, panel.gameObject.scene.name);
            Assert.IsNull(adjust.transform.parent, "HDCAdjust is a root object");
            Assert.AreEqual(DontDestroyOnLoadScene, adjust.gameObject.scene.name);
            Object.Destroy(setup);
            Object.Destroy(panel.gameObject);
            Object.Destroy(adjust.gameObject);
            yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator OnlyOneDebugPanelRunsAndOneKeptAcrossScenesTakesOver()
        {
            yield return new EnterPlayMode();
            GameObject scenePanel = Object.Instantiate(HDCTestPaths.DebugPanelPrefab());
            yield return null;

            GameObject setup = Inactive("HDCAdsSetup");
            HDCAdsDebugPanel keptPanel = Object.Instantiate(HDCTestPaths.DebugPanelPrefab(), setup.transform).GetComponent<HDCAdsDebugPanel>();
            Set(keptPanel, "keepAcrossScenes", true);
            setup.SetActive(true);
            yield return null;

            Assert.IsTrue(scenePanel == null, "the panel kept across scenes replaces the scene's own");
            Assert.IsTrue(keptPanel != null);

            GameObject nextScenePanel = Object.Instantiate(HDCTestPaths.DebugPanelPrefab());
            yield return null;

            Assert.IsTrue(nextScenePanel == null, "a later panel leaves the kept one alone");
            Assert.IsTrue(keptPanel != null);
            Object.Destroy(setup);
            Object.Destroy(keptPanel.gameObject);
            yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator AnEmptyHDCAdjustGivesWayToAConfiguredOne()
        {
            yield return new EnterPlayMode();
            GameObject scene = Inactive("Scene");
            HDCAdjust emptyInSetup = Adjust(scene, false);
            HDCAdjust configured = Adjust(scene, true);
            scene.SetActive(true);
            yield return null;

            Assert.IsTrue(emptyInSetup == null, "the empty one leaves when a configured one is in the scene");
            Assert.IsTrue(configured != null);

            HDCAdjust later = Adjust(null, false);
            yield return null;
            Assert.IsTrue(later == null, "a later empty one leaves the configured one alone");

            Object.Destroy(configured.gameObject);
            yield return null;
            HDCAdjust first = Adjust(null, false);
            yield return null;
            HDCAdjust takesOver = Adjust(null, true);
            yield return null;

            Assert.IsTrue(first == null, "a configured one takes over from an empty one that came first");
            Assert.IsTrue(takesOver != null);
            Assert.IsTrue(HDCAdjust.InScene);
            Object.Destroy(scene);
            Object.Destroy(takesOver.gameObject);
            yield return new ExitPlayMode();
        }

        private static GameObject SetupPrefab()
        {
            string path = HDCTestPaths.AssemblyFolder("HDC.Ads.Setup") + "/HDCAdsSetup.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.IsNotNull(prefab, "no HDCAdsSetup prefab at " + path);
            return prefab;
        }

        private static GameObject Inactive(string name)
        {
            var holder = new GameObject(name);
            holder.SetActive(false);
            return holder;
        }

        private static HDCAdjust Adjust(GameObject parent, bool startManually)
        {
            GameObject holder = parent != null ? parent : Inactive("Holder");
            var adjustObject = new GameObject("HDCAdjust");
            adjustObject.transform.SetParent(holder.transform, false);
            var adjust = adjustObject.AddComponent<HDCAdjust>();
            Set(adjust, "startManually", startManually);
            if (parent == null)
            {
                adjustObject.transform.SetParent(null, false);
                Object.Destroy(holder);
            }

            return adjust;
        }

        private static void Set(Object target, string field, object value) =>
            target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    }
}
