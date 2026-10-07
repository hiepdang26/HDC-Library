using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace HDC.Ads.Tests
{
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

        private static readonly Rule UnityLog = new Rule("Unity log (use IAdsLog)", @"\bDebug\.Log");

        private static readonly (string Folder, Rule[] Forbidden)[] Layers =
        {
            ("Runtime/Api", Sdks.Concat(new[] { UnityLog, Uses("Infrastructure") }).ToArray()),
            ("Runtime/Logic", Sdks.Concat(new[] { UnityLog, Uses("Infrastructure"), Uses("Composition") }).ToArray()),
            ("Runtime/Domain", Sdks.Concat(new[] { UnityLog, Uses("Logic"), Uses("Diagnostics"), Uses("Ports"), Uses("Infrastructure"), Uses("Composition") }).ToArray()),
            ("Runtime/Diagnostics", Sdks.Concat(new[] { UnityLog, Uses("Logic"), Uses("Composition") }).ToArray()),
            ("Runtime/Ports", Sdks.Concat(new[] { UnityLog, Uses("Logic"), Uses("Diagnostics"), Uses("Infrastructure"), Uses("Composition") }).ToArray()),
            ("Runtime/Infrastructure", new[] { Uses("Logic"), Uses("Composition") }),
            ("Runtime/Composition", Sdks),
        };

        private static readonly string[] KnownDebt =
        {
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
