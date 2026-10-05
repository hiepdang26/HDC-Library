using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;

namespace HDC.Ads.Tests
{
    public class HDCIosLocalBuildTests
    {
        private const BindingFlags Statics = BindingFlags.NonPublic | BindingFlags.Static;
        private const string EmbedPhase = "HDC Embed Dynamic Pods";
        private const string ComposePhase = "HDC Copy Compose Resources";
        private static readonly string[] Keys = { "HDC.iOS.TeamId", "HDC.iOS.BundleId", "HDC.iOS.MacRun" };

        private string[] saved;

        [SetUp]
        public void SaveSettings() => saved = Array.ConvertAll(Keys, key => EditorUserSettings.GetConfigValue(key));

        [TearDown]
        public void RestoreSettings()
        {
            for (int i = 0; i < Keys.Length; i++)
                EditorUserSettings.SetConfigValue(Keys[i], saved[i] ?? "");
        }

        [Test]
        public void LocalSigningIsKeptForThisProjectAndClearsTogether()
        {
            Type settings = Settings();
            Set(settings, "TeamId", " ABCDE12345 ");
            Set(settings, "BundleId", "com.example.local");

            Assert.AreEqual("ABCDE12345", Get(settings, "TeamId"));
            Assert.AreEqual("ABCDE12345", EditorUserSettings.GetConfigValue("HDC.iOS.TeamId"), "kept in UserSettings");
            Assert.IsTrue((bool)Get(settings, "HasSigning"));
            StringAssert.Contains("bundle ID com.example.local", (string)Get(settings, "Summary"));

            settings.GetMethod("ClearSigning", Statics).Invoke(null, null);
            Assert.AreEqual("", Get(settings, "TeamId"));
            Assert.AreEqual("", Get(settings, "BundleId"));
        }

        [Test]
        public void TheMacSettingStartsAtTheXcodeDefault()
        {
            Type settings = Settings();
            EditorUserSettings.SetConfigValue("HDC.iOS.MacRun", "");
            Assert.AreEqual("XcodeDefault", Get(settings, "MacRun").ToString());
            EditorUserSettings.SetConfigValue("HDC.iOS.MacRun", "7");
            Assert.AreEqual("XcodeDefault", Get(settings, "MacRun").ToString(), "an unknown value");

            Set(settings, "MacRun", MacRun("Off"));
            Assert.AreEqual("Off", EditorUserSettings.GetConfigValue("HDC.iOS.MacRun"));
            Assert.IsTrue((bool)Get(settings, "IsSet"));
        }

        [TestCase("WZBU5SK92Q", true)]
        [TestCase("wzbu5sk92q", false)]
        [TestCase("ABC123", false)]
        public void TeamIdsHaveTenCapitalLettersAndDigits(string value, bool valid) =>
            Assert.AreEqual(valid, (bool)Settings().GetMethod("IsTeamId", Statics).Invoke(null, new object[] { value }));

        [TestCase("com.example.game", true)]
        [TestCase("com.example-studio.game2", true)]
        [TestCase("game", false)]
        [TestCase("com..game", false)]
        [TestCase("com.example.game.", false)]
        [TestCase("com.example game", false)]
        public void BundleIdsAreReverseDns(string value, bool valid) =>
            Assert.AreEqual(valid, (bool)Settings().GetMethod("IsBundleId", Statics).Invoke(null, new object[] { value }));

        [Test]
        public void LocalSigningSignsBothTargetsAndRenamesOnlyTheApp()
        {
            XcodeProject before = new XcodeProject(Trampoline());
            XcodeProject after = new XcodeProject(WithLocalBuild(Trampoline(), "ABCDE12345", "com.example.local", "XcodeDefault", false));

            foreach (string target in new[] { after.Main, after.Framework })
            {
                Assert.AreEqual("ABCDE12345", after.Property(target, "DEVELOPMENT_TEAM"));
                Assert.AreEqual("Automatic", after.Property(target, "CODE_SIGN_STYLE"));
                Assert.AreEqual("Apple Development", after.Property(target, "CODE_SIGN_IDENTITY"));
                Assert.AreEqual("", after.Property(target, "PROVISIONING_PROFILE_SPECIFIER"), "a manual profile would clash with automatic signing");
            }

            Assert.AreEqual("com.example.local", after.Property(after.Main, "PRODUCT_BUNDLE_IDENTIFIER"));
            Assert.AreEqual(before.Property(before.Framework, "PRODUCT_BUNDLE_IDENTIFIER"), after.Property(after.Framework, "PRODUCT_BUNDLE_IDENTIFIER"));
            Assert.IsNull(after.Property(after.Main, "SUPPORTS_MAC_DESIGNED_FOR_IPHONE_IPAD"), "the Mac setting stays at the Xcode default");
        }

        [Test]
        public void EmptySettingsLeaveTheProjectAsUnityExportedIt()
        {
            XcodeProject before = new XcodeProject(Trampoline());
            XcodeProject after = new XcodeProject(WithLocalBuild(Trampoline(), "", "", "XcodeDefault", false));

            foreach (string property in new[] { "DEVELOPMENT_TEAM", "CODE_SIGN_STYLE", "PRODUCT_BUNDLE_IDENTIFIER", "SUPPORTS_MAC_DESIGNED_FOR_IPHONE_IPAD" })
                Assert.AreEqual(before.Property(before.Main, property), after.Property(after.Main, property), property);
        }

        [TestCase("On", false, "YES")]
        [TestCase("On", true, null)]
        [TestCase("Off", false, "NO")]
        [TestCase("Off", true, "NO")]
        public void RunningOnMyMacFollowsTheSetting(string mode, bool simulator, string expected)
        {
            XcodeProject after = new XcodeProject(WithLocalBuild(Trampoline(), "", "", mode, simulator));

            foreach (string target in new[] { after.Main, after.Framework })
            {
                Assert.AreEqual(expected, after.Property(target, "SUPPORTS_MAC_DESIGNED_FOR_IPHONE_IPAD"));
                Assert.AreEqual(expected == "YES" ? "NO" : null, after.Property(target, "SUPPORTS_MACCATALYST"));
            }
        }

        [TestCase(false, TestName = "AnExportOverAnOlderOneDropsTheOldHdcPhases(written by Unity)")]
        [TestCase(true, TestName = "AnExportOverAnOlderOneDropsTheOldHdcPhases(written by CocoaPods)")]
        public void AnExportOverAnOlderOneDropsTheOldHdcPhases(bool namedComments)
        {
            var project = new XcodeProject(Trampoline());
            int phases = project.PhaseCount(project.Main);
            string embed = project.AddPhase(project.Main, EmbedPhase, "echo old embed");
            string compose = project.AddPhase(project.Main, ComposePhase, "echo compose");
            project.AddPhase(project.Main, "Game Phase", "echo game");
            string text = project.Text;
            if (namedComments)
            {
                text = text.Replace(embed + " /* ShellScript */", embed + " /* " + EmbedPhase + " */")
                    .Replace(compose + " /* ShellScript */", compose + " /* " + ComposePhase + " */");
                StringAssert.Contains("/* " + EmbedPhase + " */ = {", text);
            }

            string cleaned = (string)Postprocess().GetMethod("WithoutPhases", Statics)
                .Invoke(null, new object[] { text, new[] { ComposePhase, EmbedPhase } });

            StringAssert.DoesNotContain(embed, cleaned);
            StringAssert.DoesNotContain(compose, cleaned);
            var reread = new XcodeProject(cleaned);
            Assert.AreEqual(phases + 1, reread.PhaseCount(reread.Main), "only the game's own phase is left");
            StringAssert.Contains("echo game", cleaned);
            Assert.AreEqual(text, (string)Postprocess().GetMethod("WithoutPhases", Statics)
                .Invoke(null, new object[] { text, new[] { "No Such Phase" } }), "nothing to remove keeps the text");
        }

        private static Type Settings()
        {
            Type type = Type.GetType("HDC.Ads.Editor.HDCIosLocalBuild, HDC.Ads.Editor");
            Assert.IsNotNull(type, "HDCIosLocalBuild is gone");
            return type;
        }

        private static object MacRun(string name) =>
            Enum.Parse(Type.GetType("HDC.Ads.Editor.HDCMacRun, HDC.Ads.Editor"), name);

        private static object Get(Type type, string property) => type.GetProperty(property, Statics).GetValue(null);

        private static void Set(Type type, string property, object value) => type.GetProperty(property, Statics).SetValue(null, value);

        private static Type Postprocess()
        {
            Type type = Type.GetType("HDC.Ads.Editor.HDCAdsIosPostprocess, HDC.Ads.Editor");
            if (type == null)
                Assert.Ignore("The iOS postprocess compiles only with iOS as the build target.");
            return type;
        }

        private static string WithLocalBuild(string projectText, string teamId, string bundleId, string macRun, bool simulator) =>
            (string)Postprocess().GetMethod("WithLocalBuild", Statics)
                .Invoke(null, new[] { projectText, teamId, bundleId, MacRun(macRun), simulator });

        private static string Trampoline()
        {
            Postprocess();
            string path = Path.Combine(BuildPipeline.GetPlaybackEngineDirectory(BuildTarget.iOS, BuildOptions.None),
                "Trampoline", "Unity-iPhone.xcodeproj", "project.pbxproj");
            if (!File.Exists(path))
                Assert.Ignore("The iOS build support has no Xcode project template at " + path);
            return File.ReadAllText(path);
        }

        private sealed class XcodeProject
        {
            private readonly Type type = Type.GetType("UnityEditor.iOS.Xcode.PBXProject, UnityEditor.iOS.Extensions.Xcode");
            private readonly object project;

            internal XcodeProject(string text)
            {
                Assert.IsNotNull(type, "the Xcode API of the iOS build support is missing");
                project = Activator.CreateInstance(type);
                Call("ReadFromString", text);
            }

            internal string Main => (string)Call("GetUnityMainTargetGuid");

            internal string Framework => (string)Call("GetUnityFrameworkTargetGuid");

            internal string Text => (string)Call("WriteToString");

            internal string Property(string target, string name) => (string)Call("GetBuildPropertyForAnyConfig", target, name);

            internal int PhaseCount(string target) => ((string[])Call("GetAllBuildPhasesForTarget", target)).Length;

            internal string AddPhase(string target, string name, string script) => (string)Call("AddShellScriptBuildPhase", target, name, "/bin/sh", script);

            private object Call(string method, params object[] arguments)
            {
                Type[] types = Array.ConvertAll(arguments, argument => argument.GetType());
                MethodInfo info = type.GetMethod(method, types);
                Assert.IsNotNull(info, "PBXProject." + method + " is missing");
                return info.Invoke(project, arguments);
            }
        }
    }
}
