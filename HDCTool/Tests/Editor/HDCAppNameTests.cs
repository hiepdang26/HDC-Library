using HDC.Tool.Editor;
using NUnit.Framework;

namespace HDC.Tool.Tests
{
    public class HDCAppNameTests
    {
        private const string Plist =
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<plist version=\"1.0\">\n<dict>\n\t<key>CFBundleName</key>\n\t<string>${PRODUCT_NAME}</string>\n</dict>\n</plist>\n";

        private const string Strings =
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<resources>\n  <string name=\"app_name\">HDC</string>\n  <string name=\"game_view_content_description\">Game view</string>\n</resources>";

        [Test]
        public void IosGetsADisplayNameUnderTheIcon()
        {
            string named = HDCAppName.WithIosName(Plist, "Phone Case & DIY");

            StringAssert.Contains("<dict>\n\t<key>CFBundleDisplayName</key>\n\t<string>Phone Case &amp; DIY</string>", named);
            StringAssert.Contains("<string>${PRODUCT_NAME}</string>", named, "the bundle name stays the product name");
        }

        [Test]
        public void AnExistingIosDisplayNameIsReplaced()
        {
            string once = HDCAppName.WithIosName(Plist, "First");

            string twice = HDCAppName.WithIosName(once, "Second");

            StringAssert.Contains("<string>Second</string>", twice);
            StringAssert.DoesNotContain("First", twice);
            Assert.AreEqual(1, twice.Split(new[] { "CFBundleDisplayName" }, System.StringSplitOptions.None).Length - 1);
        }

        [Test]
        public void AndroidGetsTheNameAsItsLabel()
        {
            string named = HDCAppName.WithAndroidName(Strings, "Phone Case DIY");

            StringAssert.Contains("<string name=\"app_name\">Phone Case DIY</string>", named);
            StringAssert.Contains("<string name=\"game_view_content_description\">Game view</string>", named);
        }

        [TestCase("Bob's \"Case\" & more", "Bob\\'s \\\"Case\\\" &amp; more")]
        [TestCase("@home", "\\@home")]
        [TestCase("<b>", "&lt;b&gt;")]
        public void AndroidNamesAreEscapedForResources(string name, string expected) => Assert.AreEqual(expected, HDCAppName.AndroidText(name));
    }
}
