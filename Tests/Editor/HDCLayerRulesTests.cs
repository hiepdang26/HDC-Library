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
    /// A folder is a layer. The API (what game code calls) and the logic under it (the channels, their configs and
    /// their state) decide when ads load and show; they reach ad networks, Firebase, storage and native code only
    /// through the classes under them, so they never name them. Roadmap phase 2 splits Logic into Application and
    /// Domain: the rule then covers those folders, and the known debt below must be gone.
    /// </summary>
    public class HDCLayerRulesTests
    {
        private static readonly string[] LayerFolders = { "Runtime/Api", "Runtime/Logic" };

        private static readonly (string Name, Regex Pattern)[] Forbidden =
        {
            ("Google Mobile Ads", new Regex(@"\bGoogleMobileAds\b")),
            ("Firebase", new Regex(@"(?<![\w.])Firebase\s*\.|\busing\s+(static\s+)?Firebase\s*;")),
            ("PlayerPrefs", new Regex(@"\bPlayerPrefs\b")),
            ("JNI", new Regex(@"\bAndroidJava(Object|Class|Proxy|Runnable)\b|\bAndroidJNI(Helper)?\b")),
            ("DllImport", new Regex(@"\bDllImport\b")),
        };

        /// <summary>
        /// Uses that predate the rule, as "file: rule". The test fails once one is fixed, so the list only shrinks:
        /// take the entry out in the same commit.
        /// </summary>
        private static readonly string[] KnownDebt =
        {
            // The ads removed flag and the impression counters: phase 3 puts them behind a key-value store.
            "Runtime/Api/HDCAds.cs: PlayerPrefs",
            "Runtime/Logic/HDCAdsLog.cs: PlayerPrefs",
            // MobileAds.Utils.GetDeviceScale, which turns a popup's pixels into dp: phase 3 asks the adapter.
            "Runtime/Logic/HDCPopupAds.cs: Google Mobile Ads",
        };

        [Test]
        public void ApiAndLogicNameNoAdSdkFirebaseStorageOrNativeCode()
        {
            var found = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (string folder in LayerFolders)
            {
                string path = Path.GetFullPath(HDCTestPaths.Root + "/" + folder);
                Assert.IsTrue(Directory.Exists(path), "no folder " + folder);
                foreach (string file in Directory.GetFiles(path, "*.cs", SearchOption.AllDirectories).OrderBy(file => file, StringComparer.Ordinal))
                {
                    string relative = folder + file.Substring(path.Length).Replace('\\', '/');
                    string[] lines = HDCCSharpText.CodeOnly(File.ReadAllText(file)).Split('\n');
                    for (int line = 0; line < lines.Length; line++)
                    {
                        foreach ((string name, Regex pattern) in Forbidden)
                        {
                            string key = relative + ": " + name;
                            if (!found.ContainsKey(key) && pattern.IsMatch(lines[line]))
                                found[key] = $"{relative}:{line + 1} uses {name}";
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
                message.AppendLine("The API and logic layers must reach these through the classes under them (Internal, Firebase):");
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
    }
}
