using System;
using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HDC.Ads.Tests
{
    /// <summary>
    /// Base for tests that enter Play Mode. Domain and scene reloads are off while they run: the library must
    /// work without them (the default of new Unity 6.6 projects), and Play Mode starts faster.
    /// </summary>
    public abstract class HDCPlayModeTest
    {
        private bool savedOptionsEnabled;
        private EnterPlayModeOptions savedOptions;

        [SetUp]
        public void SkipReloads()
        {
            savedOptionsEnabled = EditorSettings.enterPlayModeOptionsEnabled;
            savedOptions = EditorSettings.enterPlayModeOptions;
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload | EnterPlayModeOptions.DisableSceneReload;
        }

        [TearDown]
        public void RestoreReloads()
        {
            EditorSettings.enterPlayModeOptionsEnabled = savedOptionsEnabled;
            EditorSettings.enterPlayModeOptions = savedOptions;
        }

        protected static IEnumerator WaitFor(Func<bool> condition, float seconds)
        {
            float end = Time.realtimeSinceStartup + seconds;
            while (!condition() && Time.realtimeSinceStartup < end)
                yield return null;
            Assert.IsTrue(condition(), "timed out");
        }
    }
}
