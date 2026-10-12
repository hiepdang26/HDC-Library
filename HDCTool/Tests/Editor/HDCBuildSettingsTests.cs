using HDC.Ads;
using HDC.Tool.Editor;
using NUnit.Framework;
using UnityEngine;

namespace HDC.Tool.Tests
{
    public class HDCBuildSettingsTests
    {
        private HDCBuildSettings settings;

        [SetUp]
        public void CreateSettings() => settings = ScriptableObject.CreateInstance<HDCBuildSettings>();

        [TearDown]
        public void DestroySettings() => UnityEngine.Object.DestroyImmediate(settings);

        [Test]
        public void TestDebugTurnsTheTestToolsOn()
        {
            HDCBuildRules rules = HDCBuildRules.Defaults(HDCBuildPreset.TestDebug);

            Assert.IsTrue(rules.googleTestAds);
            Assert.IsTrue(rules.googleTestAdUnits);
            Assert.IsTrue(rules.debugPanel);
            Assert.IsTrue(rules.debugLog);
            Assert.AreEqual(HDCAdjustEnvironment.Sandbox, rules.adjustEnvironment);
            Assert.IsFalse(rules.developmentBuild);
            Assert.IsFalse(rules.androidAppBundle);
            Assert.IsFalse(rules.requireChangeLog);
        }

        [Test]
        public void ReleaseTurnsTheTestToolsOff()
        {
            HDCBuildRules rules = HDCBuildRules.Defaults(HDCBuildPreset.Release);

            Assert.IsFalse(rules.googleTestAds);
            Assert.IsFalse(rules.googleTestAdUnits);
            Assert.IsFalse(rules.debugPanel);
            Assert.IsFalse(rules.debugLog);
            Assert.AreEqual(HDCAdjustEnvironment.Production, rules.adjustEnvironment);
            Assert.IsTrue(rules.androidAppBundle);
            Assert.IsTrue(rules.requireChangeLog);
        }

        [Test]
        public void TheRulesDescribeTheBuild()
        {
            Assert.AreEqual("APK, Google test ads, debug panel, Adjust sandbox", HDCBuildRules.Defaults(HDCBuildPreset.TestDebug).Describe(HDCBuildPlatform.Android));
            Assert.AreEqual("Xcode project, real ads, no debug panel, Adjust production", HDCBuildRules.Defaults(HDCBuildPreset.Release).Describe(HDCBuildPlatform.Ios));
        }

        [Test]
        public void ACopyDoesNotChangeTheSavedRules()
        {
            HDCBuildRules copy = settings.Rules(HDCBuildPreset.Release).Copy();

            copy.debugPanel = true;

            Assert.IsFalse(settings.Rules(HDCBuildPreset.Release).debugPanel);
        }

        [Test]
        public void ResetBringsBackTheDefaultsOfOnePreset()
        {
            settings.Rules(HDCBuildPreset.Release).debugPanel = true;
            settings.Rules(HDCBuildPreset.TestDebug).debugPanel = false;

            settings.ResetRules(HDCBuildPreset.Release);

            Assert.IsFalse(settings.Rules(HDCBuildPreset.Release).debugPanel);
            Assert.IsFalse(settings.Rules(HDCBuildPreset.TestDebug).debugPanel, "the other preset keeps its change");
        }
    }
}
