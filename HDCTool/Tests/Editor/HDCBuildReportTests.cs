using System.IO;
using HDC.Tool.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HDC.Tool.Tests
{
    public class HDCBuildReportTests
    {
        private HDCBuildSettings settings;
        private string folder;

        [SetUp]
        public void CreateSettings()
        {
            settings = ScriptableObject.CreateInstance<HDCBuildSettings>();
            settings.ChangeLog = "Fix the splash";
            folder = Path.Combine(Path.GetTempPath(), "hdc-build-report-" + System.Guid.NewGuid().ToString("N"));
        }

        [TearDown]
        public void Clean()
        {
            Object.DestroyImmediate(settings);
            if (Directory.Exists(folder))
                Directory.Delete(folder, true);
        }

        [Test]
        public void TheReportHasEverySection()
        {
            string text = HDCBuildReport.Text(Result(), HDCBuildRules.Defaults(HDCBuildPreset.Release), settings,
                new[] { new HDCBuildCheck("Keystore", HDCCheckLevel.Warning, "Enter the passwords.") }, "Pixel 6a Android 14", new[] { "error one" });

            foreach (string section in new[] { "1. Overview", "2. Player settings", "3. Preset rules", "4. Machine and device", "5. Change log", "6. Validation scan", "7. Build errors" })
                StringAssert.Contains(section, text);
            StringAssert.Contains("Build name: SAHDC_release_1.0.0_12_AAB", text);
            StringAssert.Contains("Size: 25.0 MB", text);
            StringAssert.Contains("Duration: 2m 14s", text);
            StringAssert.Contains("App bundle (AAB): ON", text);
            StringAssert.Contains("Debug panel: OFF", text);
            StringAssert.Contains("Device: Pixel 6a Android 14", text);
            StringAssert.Contains("Fix the splash", text);
            StringAssert.Contains("[WARNING] Keystore: Enter the passwords.", text);
            StringAssert.Contains("error one", text);
        }

        [Test]
        public void TheReportIsWrittenNextToTheBuild()
        {
            HDCBuildResult result = Result();
            result.outputPath = Path.Combine(folder, "SAHDC_release_1.0.0_12_AAB.aab");

            string path = HDCBuildReport.Write(result, HDCBuildRules.Defaults(HDCBuildPreset.Release), settings, new HDCBuildCheck[0], "", new string[0]);

            Assert.AreEqual(Path.Combine(folder, "SAHDC_release_1.0.0_12_AAB_buildreport.txt"), path);
            Assert.IsTrue(File.Exists(path));
            StringAssert.DoesNotContain("7. Build errors", File.ReadAllText(path));
        }

        [Test]
        public void AnIosReportGoesIntoTheExportFolder()
        {
            HDCBuildResult result = Result();
            result.platform = HDCBuildPlatform.Ios;
            result.buildName = "SAHDC_release_1.0.0_12_Xcode";
            result.outputPath = folder + "/";

            string path = HDCBuildReport.Write(result, HDCBuildRules.Defaults(HDCBuildPreset.Release), settings, new HDCBuildCheck[0], "", new string[0]);

            Assert.AreEqual(Path.Combine(folder, "SAHDC_release_1.0.0_12_Xcode_buildreport.txt"), path);
            Assert.IsTrue(File.Exists(path));
        }

        [TestCase("Phone", "Run on: iPhone", "Target SDK: Device SDK")]
        [TestCase("Mac", "Run on: My Mac", "Target SDK: Device SDK")]
        [TestCase("Simulator", "Run on: Simulator", "Target SDK: Simulator SDK")]
        public void AnIosReportNamesTheSdkOfItsRunTarget(string target, string runOn, string sdk)
        {
            HDCBuildResult result = Result();
            result.platform = HDCBuildPlatform.Ios;
            result.iosTarget = (HDCAppleTarget)System.Enum.Parse(typeof(HDCAppleTarget), target);

            string text = HDCBuildReport.Text(result, HDCBuildRules.Defaults(HDCBuildPreset.Release), settings, new HDCBuildCheck[0], "", new string[0]);

            StringAssert.Contains(runOn, text);
            StringAssert.Contains(sdk, text);
        }

        [Test]
        public void TheLastResultReadsBackForItsPlatform()
        {
            string before = SessionState.GetString("HDC.Build.Last.Android", "");
            try
            {
                HDCBuildState.SetLastResult(Result());

                HDCBuildResult last = HDCBuildState.LastResult(HDCBuildPlatform.Android);

                Assert.AreEqual("SAHDC_release_1.0.0_12_AAB", last.buildName);
                Assert.IsTrue(last.Succeeded);
                Assert.AreEqual(HDCBuildPreset.Release, last.preset);
            }
            finally
            {
                SessionState.SetString("HDC.Build.Last.Android", before);
            }
        }

        [Test]
        public void TheLastBuildLineSaysWhatHappened() =>
            Assert.AreEqual("Succeeded SAHDC_release_1.0.0_12_AAB in 2m 14s, 25.0 MB at 10:42. Installed and launched on Pixel 6a",
                HDCBuildAreaSection.Describe(Result()));

        [Test]
        public void TheFooterPillIsShort()
        {
            Assert.AreEqual("Last build 10:42 · 25.0 MB · 2m 14s", HDCBuildAreaSection.Short(Result(), "Last build"));

            HDCBuildResult failed = Result();
            failed.outcome = HDCBuildResult.FailedOutcome;
            failed.sizeBytes = 0;
            Assert.AreEqual("Last export failed 10:42 · 2m 14s", HDCBuildAreaSection.Short(failed, "Last export"));
        }

        [Test]
        public void ApiLevelsReadLikeTheirNumbers()
        {
            Assert.AreEqual("API 23", HDCBuildSummarySection.ApiLevel(AndroidSdkVersions.AndroidApiLevel23));
            Assert.AreEqual("Automatic (highest installed)", HDCBuildSummarySection.ApiLevel(AndroidSdkVersions.AndroidApiLevelAuto));
        }

        private static HDCBuildResult Result() => new HDCBuildResult
        {
            platform = HDCBuildPlatform.Android,
            preset = HDCBuildPreset.Release,
            outcome = HDCBuildResult.SucceededOutcome,
            message = "Installed and launched on Pixel 6a",
            buildName = "SAHDC_release_1.0.0_12_AAB",
            outputPath = "/tmp/SAHDC_release_1.0.0_12_AAB.aab",
            seconds = 134d,
            sizeBytes = 25L * 1048576L,
            finishedAt = "10:42",
        };
    }
}
