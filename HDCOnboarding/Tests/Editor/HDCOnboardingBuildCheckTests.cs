using System.Linq;
using System.Text.RegularExpressions;
using HDC.Ads;
using HDC.Onboarding.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.TestTools;

namespace HDC.Onboarding.Tests
{
    public class HDCOnboardingBuildCheckTests
    {
        private const string Splash = "{\"firstOpenNextScene\":\"Intro\",\"nextScene\":\"Game\"}";
        private const string Intro = "{\"nextSceneName\":\"Game\"}";
        private static readonly string[] Scenes = { "Assets/Scenes/Splash.unity", "Assets/Scenes/Intro.unity", "Assets/Scenes/Game.unity" };

        private HDCAdsSettings settings;

        [SetUp]
        public void CreateSettings() => settings = ScriptableObject.CreateInstance<HDCAdsSettings>();

        [TearDown]
        public void DestroySettings() => Object.DestroyImmediate(settings);

        [Test]
        public void AConfigWhoseScenesAreInTheBuildHasNoProblems() =>
            CollectionAssert.IsEmpty(HDCOnboardingBuildCheck.Problems(HDCOnboardingConfigKeys.OnboardingConfig(Splash, Intro), true, true, Scenes));

        [Test]
        public void AnEmptyDefaultIsReported()
        {
            string[] problems = HDCOnboardingBuildCheck.Problems(" ", true, true, Scenes).ToArray();

            Assert.AreEqual(1, problems.Length);
            StringAssert.Contains("no default for this platform", problems[0]);
        }

        [Test]
        public void EmptyScenesAndScenesOutsideTheBuildAreReported()
        {
            string config = HDCOnboardingConfigKeys.OnboardingConfig("{\"firstOpenNextScene\":\"Intro\",\"nextScene\":\"\"}", "{\"nextSceneName\":\"Gameplay\"}");

            string[] problems = HDCOnboardingBuildCheck.Problems(config, true, true, Scenes).ToArray();

            Assert.AreEqual(2, problems.Length, string.Join("\n", problems));
            StringAssert.Contains("splash_config.nextScene is empty", problems[0]);
            StringAssert.Contains("intro_config.nextSceneName is 'Gameplay', which is not an enabled scene", problems[1]);
        }

        [Test]
        public void TheIntroSceneNameCountsOnlyWhenTheBuildHasTheIntro()
        {
            string config = HDCOnboardingConfigKeys.OnboardingConfig(Splash, "{\"nextSceneName\":\"\"}");

            CollectionAssert.IsEmpty(HDCOnboardingBuildCheck.Problems(config, true, false, Scenes));
        }

        [Test]
        public void TheDeclaredValueFollowsThePlatformLikeTheRuntime()
        {
            Assert.IsNull(HDCOnboardingConfigKeys.DeclaredValue(settings, BuildTarget.Android));

            Declare("android", "");
            Assert.AreEqual("android", HDCOnboardingConfigKeys.DeclaredValue(settings, BuildTarget.Android));
            Assert.AreEqual("android", HDCOnboardingConfigKeys.DeclaredValue(settings, BuildTarget.iOS));

            Declare("android", "ios");
            Assert.AreEqual("ios", HDCOnboardingConfigKeys.DeclaredValue(settings, BuildTarget.iOS));
        }

        [Test]
        public void ABuildWithTheSplashSceneFailsWithoutTheKey()
        {
            var failure = Assert.Throws<BuildFailedException>(() =>
                HDCOnboardingBuildCheck.Check(settings, BuildTarget.Android, new[] { HDCOnboardingPaths.SplashScene }));

            StringAssert.Contains("HDC > Onboarding > Add config key", failure.Message);
        }

        [Test]
        public void ABuildWithoutTheOnboardingScenesIsNotChecked() =>
            Assert.DoesNotThrow(() => HDCOnboardingBuildCheck.Check(settings, BuildTarget.Android, new string[0]));

        [Test]
        public void ABuildWithTheKeyWarnsAboutItsScenes()
        {
            Declare(HDCOnboardingConfigKeys.OnboardingConfig(Splash, Intro), "");
            LogAssert.Expect(LogType.Warning, new Regex("splash_config.firstOpenNextScene is 'Intro', which is not an enabled scene"));
            LogAssert.Expect(LogType.Warning, new Regex("splash_config.nextScene is 'Game', which is not an enabled scene"));

            HDCOnboardingBuildCheck.Check(settings, BuildTarget.Android, new[] { HDCOnboardingPaths.SplashScene });

            LogAssert.Expect(LogType.Warning, new Regex("intro_config.nextSceneName is 'Game', which is not an enabled scene"));
            HDCOnboardingBuildCheck.Check(settings, BuildTarget.Android, new[] { HDCOnboardingPaths.IntroScene });
        }

        [Test]
        public void TheBannerConfigIsCheckedAgainstTheBuildScenes()
        {
            CollectionAssert.AreEqual(new[] { HDCOnboardingBuildCheck.BannerNotDeclared }, HDCOnboardingBuildCheck.BannerProblems(null, Scenes).ToArray());
            CollectionAssert.IsEmpty(HDCOnboardingBuildCheck.BannerProblems(HDCOnboardingConfigKeys.NativeBannerConfig("Game"), Scenes));

            string problem = HDCOnboardingBuildCheck.BannerProblems(HDCOnboardingConfigKeys.NativeBannerConfig("Gameplay"), Scenes).Single();

            StringAssert.Contains("activeSceneName is 'Gameplay', which is not an enabled scene", problem);
        }

        [Test]
        public void TheDefaultBannerConfigHasTheOldLoops()
        {
            HDCNativeBannerConfig config = HDCNativeBannerConfig.Parse(HDCOnboardingConfigKeys.NativeBannerConfig("Game"));

            Assert.AreEqual("Game", config.activeSceneName);
            CollectionAssert.AreEqual(new[] { "native_banner", "native_banner_1", "native_banner_2", "native_banner_3", "native_banner_4" },
                config.bannerLoops.Select(loop => loop.groupName).ToArray());
            CollectionAssert.AreEqual(new[] { 2, -1, -1, -1, -1 }, config.bannerLoops.Select(loop => loop.adShowCount).ToArray());
            Assert.AreEqual(60f, config.bannerLoops[0].metaTimeConfig);
        }

        [Test]
        public void ABuildWithTheSplashSceneWarnsWithoutTheBannerKey()
        {
            Declare(HDCOnboardingConfigKeys.OnboardingConfig(Splash, Intro), "");
            LogAssert.Expect(LogType.Warning, new Regex("no first_native_banner_config custom key"));

            HDCOnboardingBuildCheck.Check(settings, BuildTarget.Android, new[] { HDCOnboardingPaths.SplashScene });
        }

        [Test]
        public void TheToolScanReusesTheSceneDependenciesButABuildReadsThemAgain()
        {
            string[] scenes = { HDCOnboardingPaths.SplashScene, HDCOnboardingPaths.IntroScene };
            System.Collections.Generic.HashSet<string> first = HDCOnboardingBuildCheck.Dependencies(scenes, true);
            Assert.AreSame(first, HDCOnboardingBuildCheck.Dependencies(scenes, true), "the Build Tool scan reuses them while nothing changed");
            System.Collections.Generic.HashSet<string> fresh = HDCOnboardingBuildCheck.Dependencies(scenes, false);
            Assert.AreNotSame(first, fresh, "a build reads them again");
            CollectionAssert.AreEquivalent(first, fresh);
            Assert.AreNotSame(fresh, HDCOnboardingBuildCheck.Dependencies(new[] { HDCOnboardingPaths.SplashScene }, true), "another scene list reads them again");
            Assert.IsTrue(fresh.Any(path => path.EndsWith("/HDCSplash.cs")), "the splash scene uses HDCSplash");
        }

        private void Declare(string android, string ios)
        {
            var serialized = new SerializedObject(settings);
            SerializedProperty keys = serialized.FindProperty("customKeys");
            keys.arraySize = 1;
            SerializedProperty key = keys.GetArrayElementAtIndex(0);
            key.FindPropertyRelative("key").stringValue = HDCOnboardingKeys.OnboardingConfig;
            key.FindPropertyRelative("android").stringValue = android;
            key.FindPropertyRelative("ios").stringValue = ios;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
