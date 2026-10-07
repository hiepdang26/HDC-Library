using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace HDC.Ads.Tests
{
    public class HDCCustomConfigTests : HDCPlayModeTest
    {
        private const string Key = "hdc_test_custom";

        [SetUp]
        public void Clean()
        {
            PlayerPrefs.DeleteKey(HDCCustomConfig.SavedPrefix + Key);
            HDCCustomConfig.Reset();
        }

        [TearDown]
        public void CleanUp()
        {
            PlayerPrefs.DeleteKey(HDCCustomConfig.SavedPrefix + Key);
            HDCCustomConfig.Reset();
        }

        [Test]
        public void AValueComesFromRemoteThenSavedThenDefault()
        {
            HDCCustomConfig.Declare(new Dictionary<string, string> { { Key, "default" } });
            Assert.AreEqual("default", HDCCustomConfig.Get(Key));
            Assert.IsFalse(HDCCustomConfig.IsReady);

            PlayerPrefs.SetString(HDCCustomConfig.SavedPrefix + Key, "saved");
            Assert.AreEqual("saved", HDCCustomConfig.Get(Key), "the last fetched value beats the default");

            int updates = 0;
            HDCCustomConfig.Updated += () => updates++;
            HDCCustomConfig.Apply(new Dictionary<string, (string, string)> { { Key, ("remote", HDCCustomConfig.RemoteSource) } }, true);

            Assert.AreEqual("remote", HDCCustomConfig.Get(Key));
            Assert.AreEqual(HDCCustomConfig.RemoteSource, HDCCustomConfig.SourceOf(Key));
            Assert.AreEqual("remote", PlayerPrefs.GetString(HDCCustomConfig.SavedPrefix + Key), "kept for the next launch");
            Assert.IsTrue(HDCCustomConfig.IsReady);
            Assert.AreEqual(1, updates);
            CollectionAssert.AreEqual(new[] { Key }, HDCCustomConfig.Keys.ToArray());
        }

        [Test]
        public void AnUndeclaredKeyReadsEmptyAndWarnsOnce()
        {
            HDCCustomConfig.Declare(new Dictionary<string, string> { { Key, "default" } });
            LogAssert.Expect(LogType.Warning, new Regex("custom config 'missing' is not declared"));

            Assert.IsFalse(HDCCustomConfig.TryGet("missing", out string value));
            Assert.AreEqual("", value);
            Assert.AreEqual("", HDCCustomConfig.Get("missing"));
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void AFailingHandlerDoesNotStopTheOthers()
        {
            HDCCustomConfig.Declare(new Dictionary<string, string> { { Key, "default" } });
            int calls = 0;
            HDCCustomConfig.Updated += () => throw new InvalidOperationException("a game handler failed");
            HDCCustomConfig.Updated += () => calls++;
            LogAssert.Expect(LogType.Exception, new Regex("a game handler failed"));

            HDCCustomConfig.Apply(new Dictionary<string, (string, string)> { { Key, ("default", HDCCustomConfig.DefaultSource) } }, false);

            Assert.AreEqual(1, calls);
            Assert.AreEqual(HDCCustomConfig.DefaultSource, HDCCustomConfig.SourceOf(Key));
        }

        [Test]
        public void KeyRulesNameEveryKeyThatCannotWork()
        {
            List<string> problems = HDCAdsSettings.CustomKeyProblems(new[] { "level_config", "", "level_config", "ads_config", "1st", "with-dash" }).ToList();

            Assert.AreEqual(5, problems.Count, string.Join("\n", problems));
            StringAssert.Contains("#2", problems[0]);
            StringAssert.Contains("more than once", problems[1]);
            StringAssert.Contains("'ads_config'", problems[2]);
            StringAssert.Contains("'1st'", problems[3]);
            StringAssert.Contains("'with-dash'", problems[4]);
        }

        [Test]
        public void TheSettingsGiveEachKeyItsPlatformValue()
        {
            var settings = ScriptableObject.CreateInstance<HDCAdsSettings>();
            try
            {
                var serialized = new SerializedObject(settings);
                SerializedProperty keys = serialized.FindProperty("customKeys");
                keys.arraySize = 3;
                Set(keys.GetArrayElementAtIndex(0), " both ", "a", "i");
                Set(keys.GetArrayElementAtIndex(1), "android_only", "a", "");
                Set(keys.GetArrayElementAtIndex(2), "both", "duplicate", "duplicate");
                serialized.ApplyModifiedPropertiesWithoutUndo();

                Dictionary<string, string> defaults = settings.CustomDefaults();

                CollectionAssert.AreEquivalent(new[] { "both", "android_only" }, defaults.Keys);
#if UNITY_IOS
                Assert.AreEqual("i", defaults["both"]);
#else
                Assert.AreEqual("a", defaults["both"]);
#endif
                Assert.AreEqual("a", defaults["android_only"], "iOS uses the Android value when its own is empty");
            }
            finally
            {
                Object.DestroyImmediate(settings);
            }
        }

#if HDC_FIREBASE
        [UnityTest]
        public IEnumerator TheEditorUsesTheDefaultsAndMarksThemReady()
        {
            yield return new EnterPlayMode();
            HDCCustomConfig.Declare(new Dictionary<string, string> { { Key, "default" } });
            PlayerPrefs.SetString(HDCCustomConfig.SavedPrefix + Key, "saved on a device");
            bool loaded = false;
            HDCRemoteConfig.Fetch("{}", new Dictionary<string, string>(), (ads, core) => loaded = true);
            yield return WaitFor(() => loaded, 5f);

            Assert.IsTrue(HDCCustomConfig.IsReady);
            Assert.AreEqual("default", HDCCustomConfig.Get(Key), "the Editor reads the project defaults, as for ads_config");
            Assert.AreEqual(HDCCustomConfig.DefaultSource, HDCCustomConfig.SourceOf(Key));
            Assert.AreEqual("saved on a device", PlayerPrefs.GetString(HDCCustomConfig.SavedPrefix + Key), "the Editor saves nothing");
            yield return new ExitPlayMode();
        }
#endif

        private static void Set(SerializedProperty custom, string key, string android, string ios)
        {
            custom.FindPropertyRelative("key").stringValue = key;
            custom.FindPropertyRelative("android").stringValue = android;
            custom.FindPropertyRelative("ios").stringValue = ios;
        }
    }
}
