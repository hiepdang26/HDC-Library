using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace HDC.Ads.Tests
{
    /// <summary>
    /// A folder under Runtime is a layer, with the namespace of its name:
    /// <list type="bullet">
    /// <item>Api (HDC.Ads): what game code calls.</item>
    /// <item>Logic: the channels and their groups, which decide when ads load and show.</item>
    /// <item>Domain: the configs, options and events everything else passes around.</item>
    /// <item>Diagnostics: what the debug panel reads.</item>
    /// <item>Infrastructure: the SDK, the native bridges and the Google Mobile Ads plugin.</item>
    /// </list>
    /// Only Infrastructure names ad networks, Firebase, storage and native code, and nothing depends on a layer
    /// above it. Each line of <see cref="Layers"/> says what a layer may not name.
    /// </summary>
    public class HDCLayerRulesTests
    {
        private static readonly Rule[] Sdks =
        {
            new Rule("Google Mobile Ads", @"\bGoogleMobileAds\b"),
            new Rule("Firebase", @"(?<![\w.])Firebase\s*\.|\busing\s+(static\s+)?Firebase\s*;"),
            new Rule("PlayerPrefs", @"\bPlayerPrefs\b"),
            new Rule("JNI", @"\bAndroidJava(Object|Class|Proxy|Runnable)\b|\bAndroidJNI(Helper)?\b"),
            new Rule("DllImport", @"\bDllImport\b"),
        };

        private static readonly (string Folder, Rule[] Forbidden)[] Layers =
        {
            ("Runtime/Api", Sdks.Append(Uses("Infrastructure")).ToArray()),
            ("Runtime/Logic", Sdks.Append(Uses("Infrastructure")).ToArray()),
            ("Runtime/Domain", Sdks.Concat(new[] { Uses("Logic"), Uses("Diagnostics"), Uses("Infrastructure") }).ToArray()),
            ("Runtime/Diagnostics", Sdks.Append(Uses("Logic")).ToArray()),
            ("Runtime/Infrastructure", new[] { Uses("Logic") }),
        };

        /// <summary>
        /// Uses that predate the rules, as "file: rule". The test fails once one is fixed, so the list only
        /// shrinks: take the entry out in the same commit.
        /// </summary>
        private static readonly string[] KnownDebt =
        {
            // The ads removed flag and the impression counters: phase 3 puts them behind a key-value store.
            "Runtime/Api/HDCAds.cs: PlayerPrefs",
            "Runtime/Logic/HDCAdsLog.cs: PlayerPrefs",
            // MobileAds.Utils.GetDeviceScale, which turns a popup's pixels into dp: phase 3 asks the adapter.
            "Runtime/Logic/Channels/HDCPopupAds.cs: Google Mobile Ads",
            // The facade and the channels call the SDK, the main thread and the plugin's ads directly: phase 3 puts
            // them behind ports that Infrastructure implements.
            "Runtime/Api/HDCAds.cs: uses Infrastructure",
            "Runtime/Logic/Channels/HDCAppLaunchAds.cs: uses Infrastructure",
            "Runtime/Logic/Channels/HDCAppResumeAds.cs: uses Infrastructure",
            "Runtime/Logic/Channels/HDCForceAds.cs: uses Infrastructure",
            "Runtime/Logic/Channels/HDCMrecAds.cs: uses Infrastructure",
            "Runtime/Logic/Channels/HDCPopupAds.cs: uses Infrastructure",
            "Runtime/Logic/Groups/HDCFullscreenSource.cs: uses Infrastructure",
            "Runtime/Logic/Groups/HDCNativeBannerSource.cs: uses Infrastructure",
            "Runtime/Logic/Groups/HDCNativeFullscreenSource.cs: uses Infrastructure",
            "Runtime/Logic/Groups/HDCNativeInterstitialSource.cs: uses Infrastructure",
            "Runtime/Logic/Groups/HDCPluginFullscreenSource.cs: uses Infrastructure",
            "Runtime/Logic/Groups/HDCPluginRectSource.cs: uses Infrastructure",
            "Runtime/Logic/Groups/HDCRectSource.cs: uses Infrastructure",
            "Runtime/Logic/HDCAdsLog.cs: uses Infrastructure",
        };

        [Test]
        public void LayersNameOnlyWhatTheyMay()
        {
            var found = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach ((string folder, Rule[] forbidden) in Layers)
            {
                string path = Path.GetFullPath(HDCTestPaths.Root + "/" + folder);
                Assert.IsTrue(Directory.Exists(path), "no folder " + folder);
                foreach (string file in Directory.GetFiles(path, "*.cs", SearchOption.AllDirectories).OrderBy(file => file, StringComparer.Ordinal))
                {
                    string relative = folder + file.Substring(path.Length).Replace('\\', '/');
                    string[] lines = HDCCSharpText.CodeOnly(File.ReadAllText(file)).Split('\n');
                    for (int line = 0; line < lines.Length; line++)
                    {
                        foreach (Rule rule in forbidden)
                        {
                            string key = relative + ": " + rule.Name;
                            if (!found.ContainsKey(key) && rule.Pattern.IsMatch(lines[line]))
                                found[key] = $"{relative}:{line + 1} {rule.Name}";
                        }
                    }
                }
            }

            List<string> added = found.Keys.Except(KnownDebt).Select(key => found[key]).ToList();
            List<string> paidOff = KnownDebt.Except(found.Keys).ToList();
            if (added.Count == 0 && paidOff.Count == 0)
                return;

            var message = new StringBuilder();
            if (added.Count > 0)
            {
                message.AppendLine("These break a layer rule (see HDCLayerRulesTests.Layers):");
                foreach (string use in added)
                    message.Append("  ").AppendLine(use);
            }

            if (paidOff.Count > 0)
            {
                message.AppendLine("Fixed, take them out of HDCLayerRulesTests.KnownDebt:");
                foreach (string debt in paidOff)
                    message.Append("  ").AppendLine(debt);
            }

            Assert.Fail(message.ToString());
        }

        [Test]
        public void CodeOnlyBlanksCommentsAndStrings()
        {
            const string source = "// PlayerPrefs\nvar a = \"PlayerPrefs\"; /* Firebase.App\n*/ var b = @\"x \"\" PlayerPrefs\";\nPlayerPrefs.Save(); char c = '\"';";
            string[] lines = HDCCSharpText.CodeOnly(source).Split('\n');
            Assert.AreEqual(4, lines.Length, "line breaks stay");
            Assert.IsFalse(lines[0].Contains("PlayerPrefs"));
            Assert.IsFalse(lines[1].Contains("PlayerPrefs") || lines[1].Contains("Firebase"));
            Assert.IsFalse(lines[2].Contains("PlayerPrefs"));
            StringAssert.Contains("var b =", lines[2]);
            StringAssert.StartsWith("PlayerPrefs.Save();", lines[3]);
        }

        // A layer's namespace, in a using directive or a full name.
        private static Rule Uses(string layer) => new Rule("uses " + layer, $@"\bHDC\.Ads\.{layer}\b");

        private sealed class Rule
        {
            internal Rule(string name, string pattern)
            {
                Name = name;
                Pattern = new Regex(pattern);
            }

            internal string Name { get; }
            internal Regex Pattern { get; }
        }
    }
}
