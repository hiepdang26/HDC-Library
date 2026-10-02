using System;
using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HDC.Ads.Tests
{
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
