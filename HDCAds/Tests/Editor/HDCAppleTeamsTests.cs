using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;

namespace HDC.Ads.Tests
{
    public class HDCAppleTeamsTests
    {
        private const BindingFlags Statics = BindingFlags.NonPublic | BindingFlags.Static;
        private const BindingFlags Members = BindingFlags.NonPublic | BindingFlags.Instance;

        private const string Preferences =
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<plist version=\"1.0\">\n<dict>\n" +
            "\t<key>IDEProvisioningTeamByIdentifier</key>\n\t<dict>\n\t\t<key>33016968</key>\n\t\t<array>\n\t\t\t<dict>\n" +
            "\t\t\t\t<key>isFreeProvisioningTeam</key>\n\t\t\t\t<true/>\n\t\t\t\t<key>teamID</key>\n\t\t\t\t<string>ABCDE12345</string>\n" +
            "\t\t\t\t<key>teamName</key>\n\t\t\t\t<string>Test &amp; Co (Personal Team)</string>\n\t\t\t</dict>\n" +
            "\t\t\t<dict>\n\t\t\t\t<key>isFreeProvisioningTeam</key>\n\t\t\t\t<false/>\n\t\t\t\t<key>teamID</key>\n\t\t\t\t<string>ZYXWV98765</string>\n" +
            "\t\t\t\t<key>teamName</key>\n\t\t\t\t<string>Studio</string>\n\t\t\t</dict>\n\t\t</array>\n\t</dict>\n" +
            "\t<key>IDEProvisioningTeams</key>\n\t<dict>\n\t\t<key>account</key>\n\t\t<array>\n\t\t\t<dict>\n" +
            "\t\t\t\t<key>teamID</key>\n\t\t\t\t<string>ABCDE12345</string>\n\t\t\t</dict>\n\t\t</array>\n\t</dict>\n</dict>\n</plist>\n";

        [Test]
        public void TheTeamsComeFromTheXcodeAccounts()
        {
            IList teams = Parse(Preferences);

            Assert.AreEqual(2, teams.Count, "a team listed twice is kept once");
            Assert.AreEqual("ABCDE12345", Field(teams[0], "Id"));
            Assert.AreEqual("Test & Co (Personal Team)", Field(teams[0], "Name"));
            Assert.IsTrue((bool)Field(teams[0], "Free"));
            Assert.AreEqual("ZYXWV98765", Field(teams[1], "Id"));
            Assert.IsFalse((bool)Field(teams[1], "Free"));
            Assert.AreEqual("Studio (ZYXWV98765)", teams[1].GetType().GetProperty("Title", Members).GetValue(teams[1]));
            Assert.AreEqual(0, Parse("").Count);
        }

        private static IList Parse(string plist)
        {
            Type type = Type.GetType("HDC.Ads.Editor.HDCAppleTeams, HDC.Ads.Editor");
            Assert.IsNotNull(type, "HDCAppleTeams is gone");
            return (IList)type.GetMethod("Parse", Statics).Invoke(null, new object[] { plist });
        }

        private static object Field(object team, string name) => team.GetType().GetField(name, Members).GetValue(team);
    }
}
