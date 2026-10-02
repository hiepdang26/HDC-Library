using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.Build;

namespace HDC.Ads.Tests
{
    public class HDCAdsActivationTests
    {
        private const BindingFlags Statics = BindingFlags.NonPublic | BindingFlags.Static;

        // A build must not resolve the native versions an older HDCLib wrote into the dependencies file: in batch
        // mode it starts before the refresh that follows a script reload.
        [Test]
        public void ABuildBringsAStaleDependenciesFileUpToDate()
        {
            Type activation = Type.GetType("HDC.Ads.Editor.HDCAdsActivation, HDC.Ads.Editor");
            Assert.IsNotNull(activation, "HDCAdsActivation is gone");
            if (!(bool)activation.GetProperty("IsPartlyEnabled", Statics).GetValue(null))
                Assert.Ignore("HDC ads are off in this project, so there is no dependencies file.");

            string file = (string)activation.GetProperty("DependenciesFile", Statics).GetValue(null);
            string expected = (string)activation.GetMethod("ExpectedDependencies", Statics).Invoke(null, null);
            File.WriteAllText(file, expected.Replace("hdc-ads-android:", "hdc-ads-android:0.0.0-stale-"));

            var step = (IPreprocessBuildWithReport)Activator.CreateInstance(activation.GetNestedType("BuildRefresh", BindingFlags.NonPublic));
            step.OnPreprocessBuild(null);

            Assert.AreEqual(expected, File.ReadAllText(file));
            Assert.AreEqual(int.MinValue, step.callbackOrder, "before the dependency manager resolves");
        }
    }
}
