using System.Collections.Generic;
using System.Linq;
using HDC.Tool.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HDC.Tool.Tests
{
    public class HDCBuildChecksTests
    {
        private HDCBuildSettings settings;

        [SetUp]
        public void CreateSettings() => settings = ScriptableObject.CreateInstance<HDCBuildSettings>();

        [TearDown]
        public void DestroySettings() => Object.DestroyImmediate(settings);

        [TestCase("12", true)]
        [TestCase("1.0.12", true)]
        [TestCase("", false)]
        [TestCase("1..2", false)]
        [TestCase("1.a", false)]
        public void AnIosBuildNumberIsDotSeparatedNumbers(string value, bool valid) => Assert.AreEqual(valid, HDCBuildChecks.IsBuildNumber(value));

        [Test]
        public void ReleaseWarnsWithoutAChangeLog()
        {
            HDCBuildRules rules = HDCBuildRules.Defaults(HDCBuildPreset.Release);

            HDCBuildCheck check = HDCBuildChecks.ChangeLog(rules, settings);

            Assert.AreEqual(HDCCheckLevel.Warning, check.Level);
            Assert.AreEqual("Write log", check.FixLabel);
            settings.ChangeLog = "Fix the splash";
            Assert.IsNull(HDCBuildChecks.ChangeLog(rules, settings));
            settings.ChangeLog = "";
            Assert.IsNull(HDCBuildChecks.ChangeLog(HDCBuildRules.Defaults(HDCBuildPreset.TestDebug), settings));
        }

        [Test]
        public void TheOtherPlatformIsAnErrorWithASwitch()
        {
            HDCBuildPlatform active = EditorUserBuildSettings.activeBuildTarget == BuildTarget.iOS ? HDCBuildPlatform.Ios : HDCBuildPlatform.Android;
            HDCBuildPlatform other = active == HDCBuildPlatform.Ios ? HDCBuildPlatform.Android : HDCBuildPlatform.Ios;

            Assert.AreEqual(HDCCheckLevel.Ok, HDCBuildChecks.Target(active).Level);
            HDCBuildCheck check = HDCBuildChecks.Target(other);
            Assert.AreEqual(HDCCheckLevel.Error, check.Level);
            Assert.AreEqual("Switch target", check.FixLabel);
        }

        [Test]
        public void ReleaseNeedsAKeystore()
        {
            bool custom = PlayerSettings.Android.useCustomKeystore;
            try
            {
                PlayerSettings.Android.useCustomKeystore = false;

                HDCBuildCheck release = HDCBuildChecks.Keystore(HDCBuildPreset.Release).Single();
                HDCBuildCheck test = HDCBuildChecks.Keystore(HDCBuildPreset.TestDebug).Single();

                Assert.AreEqual(HDCCheckLevel.Error, release.Level);
                Assert.AreEqual("Choose keystore", release.FixLabel);
                Assert.AreEqual(HDCCheckLevel.Ok, test.Level, "a test build may use the Unity debug key");
            }
            finally
            {
                PlayerSettings.Android.useCustomKeystore = custom;
            }
        }

        [Test]
        public void AnAndroidVersionCodeOfZeroIsAnError()
        {
            int code = PlayerSettings.Android.bundleVersionCode;
            try
            {
                PlayerSettings.Android.bundleVersionCode = 0;
                Assert.AreEqual(HDCCheckLevel.Error, HDCBuildChecks.Version(HDCBuildPlatform.Android).Level);
                PlayerSettings.Android.bundleVersionCode = 3;
                Assert.AreEqual(HDCCheckLevel.Ok, HDCBuildChecks.Version(HDCBuildPlatform.Android).Level);
            }
            finally
            {
                PlayerSettings.Android.bundleVersionCode = code;
            }
        }

        [Test]
        public void TheScanListsErrorsFirst()
        {
            foreach (HDCBuildPlatform platform in new[] { HDCBuildPlatform.Android, HDCBuildPlatform.Ios })
            {
                List<HDCBuildCheck> checks = HDCBuildChecks.Run(platform, HDCBuildPreset.Release, HDCBuildRules.Defaults(HDCBuildPreset.Release), settings, new HDCScene0());

                CollectionAssert.IsNotEmpty(checks);
                CollectionAssert.AreEqual(checks.Select(check => check.Level).OrderByDescending(level => level).ToArray(), checks.Select(check => check.Level).ToArray());
                Assert.IsTrue(checks.Any(check => check.Name == "Change log"), "Release expects a change log");
            }
        }
    }
}
