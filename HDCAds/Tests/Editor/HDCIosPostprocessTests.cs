using System;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace HDC.Ads.Tests
{
    public class HDCIosPostprocessTests
    {
        private const string FrameRateKey = "CADisableMinimumFrameDurationOnPhone";
        private const string TrackingKey = "NSUserTrackingUsageDescription";

        private const string ExportedPlist = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<!DOCTYPE plist PUBLIC ""-//Apple//DTD PLIST 1.0//EN"" ""http://www.apple.com/DTDs/PropertyList-1.0.dtd"">
<plist version=""1.0"">
<dict>
  <key>CADisableMinimumFrameDurationOnPhone</key>
  <false/>
  <key>GADApplicationIdentifier</key>
  <string>ca-app-pub-3940256099942544~1458002511</string>
</dict>
</plist>
";

        private string folder;

        [SetUp]
        public void MakeFolder()
        {
            folder = Path.Combine(Path.GetTempPath(), "hdc-plist-test-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
        }

        [TearDown]
        public void DeleteFolder() => Directory.Delete(folder, true);

        [Test]
        public void PostprocessAllowsComposeFrameRates()
        {
            string plist = Path.Combine(folder, "Info.plist");
            File.WriteAllText(plist, ExportedPlist);

            AllowHighFrameRates(folder);

            string text = File.ReadAllText(plist);
            StringAssert.IsMatch($@"<key>{FrameRateKey}</key>\s*<true\s*/>", text);
            Assert.AreEqual(1, Regex.Matches(text, FrameRateKey).Count);
            StringAssert.Contains("GADApplicationIdentifier", text, "the rest of the plist is kept");
        }

        [Test]
        public void PostprocessAddsTheFrameRateKeyWhenMissing()
        {
            string plist = Path.Combine(folder, "Info.plist");
            File.WriteAllText(plist, Regex.Replace(ExportedPlist, $@"\s*<key>{FrameRateKey}</key>\s*<false/>", ""));

            AllowHighFrameRates(folder);

            StringAssert.IsMatch($@"<key>{FrameRateKey}</key>\s*<true\s*/>", File.ReadAllText(plist));
        }

        [Test]
        public void PostprocessDescribesTheTrackingQuestionWhenNothingElseDid()
        {
            string plist = Path.Combine(folder, "Info.plist");
            File.WriteAllText(plist, ExportedPlist);

            DescribeTracking(folder);

            StringAssert.IsMatch($@"<key>{TrackingKey}</key>\s*<string>This uses device info for more personalized ads and content</string>", File.ReadAllText(plist));
        }

        [Test]
        public void PostprocessKeepsATrackingDescriptionSetElsewhere()
        {
            string plist = Path.Combine(folder, "Info.plist");
            File.WriteAllText(plist, ExportedPlist.Replace("</dict>", $"  <key>{TrackingKey}</key>\n  <string>Our own words</string>\n</dict>"));

            DescribeTracking(folder);

            string text = File.ReadAllText(plist);
            StringAssert.Contains("<string>Our own words</string>", text);
            Assert.AreEqual(1, Regex.Matches(text, TrackingKey).Count);
        }

        private static void AllowHighFrameRates(string buildPath) => Run("AllowHighFrameRates", buildPath);

        private static void DescribeTracking(string buildPath) => Run("DescribeTracking", buildPath);

        private static void Run(string step, string buildPath)
        {
            Type type = Type.GetType("HDC.Ads.Editor.HDCAdsIosPostprocess, HDC.Ads.Editor");
            if (type == null)
                Assert.Ignore("The iOS postprocess compiles only with iOS as the build target.");
            MethodInfo method = type.GetMethod(step, BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(method, "HDCAdsIosPostprocess." + step + " is gone");
            method.Invoke(null, new object[] { buildPath });
        }
    }
}
