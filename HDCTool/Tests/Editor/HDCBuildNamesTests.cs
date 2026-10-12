using HDC.Tool.Editor;
using NUnit.Framework;

namespace HDC.Tool.Tests
{
    public class HDCBuildNamesTests
    {
        [TestCase("SampleAd-HDC", "SAHDC")]
        [TestCase("my game", "MYG")]
        [TestCase("9 lives", "9LI")]
        [TestCase("", "APP")]
        [TestCase("  ", "APP")]
        public void TheProductCodeUsesTheCapitalsOrTheFirstLetters(string productName, string code) =>
            Assert.AreEqual(code, HDCBuildNames.ProductCode(productName));

        [Test]
        public void TheBuildNameHasThePresetVersionNumberAndArtifact()
        {
            Assert.AreEqual("SAHDC_testDebug_1.0.0_12_APK", HDCBuildNames.BuildName("SampleAd-HDC", HDCBuildPreset.TestDebug, "1.0.0", "12", "APK", false));
            Assert.AreEqual("SAHDC_release_1.0.0_12_AAB_dev", HDCBuildNames.BuildName("SampleAd-HDC", HDCBuildPreset.Release, "1.0.0", "12", "AAB", true));
        }

        [Test]
        public void UnsafeCharactersAndEmptyPartsAreReplaced() =>
            Assert.AreEqual("SAHDC_release_1.0-beta_0_Xcode", HDCBuildNames.BuildName("SampleAd-HDC", HDCBuildPreset.Release, "1.0 beta", "", "Xcode", false));

        [Test]
        public void TheArtifactFollowsThePlatformAndTheBundleRule()
        {
            Assert.AreEqual("Xcode", HDCBuildNames.Artifact(HDCBuildPlatform.Ios, true));
            Assert.AreEqual("AAB", HDCBuildNames.Artifact(HDCBuildPlatform.Android, true));
            Assert.AreEqual("APK", HDCBuildNames.Artifact(HDCBuildPlatform.Android, false));
        }

        [TestCase("12", "13")]
        [TestCase("1.0.12", "1.0.13")]
        [TestCase("", "1")]
        [TestCase("abc", "abc")]
        [TestCase("1.0.", "1.0.")]
        public void TheNextBuildNumberRaisesTheLastPart(string number, string next) => Assert.AreEqual(next, HDCBuildNames.NextNumber(number));

        [Test]
        public void TheCommandLinePicksThePreset()
        {
            Assert.AreEqual(HDCBuildPreset.Release, HDCBuildRunner.PresetFrom(new[] { "Unity", "-hdcPreset", "release" }));
            Assert.AreEqual(HDCBuildPreset.TestDebug, HDCBuildRunner.PresetFrom(new[] { "Unity", "-hdcPreset", "TestDebug" }));
            Assert.AreEqual(HDCBuildPreset.TestDebug, HDCBuildRunner.PresetFrom(new[] { "Unity", "-hdcPreset" }));
            Assert.AreEqual(HDCBuildPreset.TestDebug, HDCBuildRunner.PresetFrom(new[] { "Unity", "-hdcPreset", "9" }));
        }
    }
}
