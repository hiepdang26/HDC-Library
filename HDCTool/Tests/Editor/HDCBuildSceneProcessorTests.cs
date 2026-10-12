using HDC.Ads;
using HDC.Ads.DebugUI;
using HDC.Tool.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HDC.Tool.Tests
{
    public class HDCBuildSceneProcessorTests
    {
        private GameObject root;
        private HDCAdsSetup setup;
        private HDCAdjust adjust;
        private GameObject panel;

        [SetUp]
        public void CreateScene0()
        {
            root = new GameObject("HDCAdsSetup");
            setup = root.AddComponent<HDCAdsSetup>();
            adjust = root.AddComponent<HDCAdjust>();
            panel = new GameObject("HDCAdsDebugPanel");
            panel.transform.SetParent(root.transform);
            panel.AddComponent<HDCAdsDebugPanel>();
        }

        [TearDown]
        public void DestroyScene0() => Object.DestroyImmediate(root);

        [Test]
        public void ReleaseRulesTurnTheTestToolsOffInTheBuildCopy()
        {
            HDCBuildSceneProcessor.Apply(new[] { root }, HDCBuildRules.Defaults(HDCBuildPreset.Release));

            var serialized = new SerializedObject(setup);
            Assert.IsFalse(serialized.FindProperty("googleTestAds").boolValue);
            Assert.IsFalse(serialized.FindProperty("googleTestAdUnits").boolValue);
            Assert.IsFalse(serialized.FindProperty("debugLog").boolValue);
            Assert.AreEqual((int)HDCAdjustEnvironment.Production, new SerializedObject(adjust).FindProperty("environment").intValue);
            Assert.IsFalse(panel.activeSelf);
        }

        [Test]
        public void TestDebugRulesTurnTheTestToolsOn()
        {
            panel.SetActive(false);

            HDCBuildSceneProcessor.Apply(new[] { root }, HDCBuildRules.Defaults(HDCBuildPreset.TestDebug));

            var serialized = new SerializedObject(setup);
            Assert.IsTrue(serialized.FindProperty("googleTestAds").boolValue);
            Assert.IsTrue(serialized.FindProperty("googleTestAdUnits").boolValue);
            Assert.IsTrue(serialized.FindProperty("debugLog").boolValue);
            Assert.AreEqual((int)HDCAdjustEnvironment.Sandbox, new SerializedObject(adjust).FindProperty("environment").intValue);
            Assert.IsTrue(panel.activeSelf);
        }

        [Test]
        public void ABuildThatTheToolDidNotStartIsLeftAlone()
        {
            HDCBuildSceneProcessor.ActiveRules = null;
            var serialized = new SerializedObject(setup);
            serialized.FindProperty("googleTestAds").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            new HDCBuildSceneProcessor().OnProcessScene(root.scene, null);

            Assert.IsTrue(new SerializedObject(setup).FindProperty("googleTestAds").boolValue);
            Assert.IsTrue(panel.activeSelf);
        }
    }
}
