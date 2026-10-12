using System.Collections.Generic;
using System.Linq;
using HDC.Tool.Editor;
using NUnit.Framework;

namespace HDC.Tool.Tests
{
    public class HDCSdkVersionsTests
    {
        private const string Plugin =
            "<dependencies><androidPackages><androidPackage spec=\"com.google.android.gms:play-services-ads:25.4.0\">" +
            "</androidPackage></androidPackages><iosPods><iosPod name=\"Google-Mobile-Ads-SDK\" version=\"~> 13.7\"></iosPod>" +
            "<iosPod name=\"FBAudienceNetwork\" addToAllTargets=\"true\"/></iosPods></dependencies>";

        private const string Library =
            "<dependencies><androidPackages><androidPackage spec=\"com.hdc.adsmultiplatform:hdc-ads-android:0.3.5\" /></androidPackages>" +
            "<iosPods><iosPod name=\"Google-Mobile-Ads-SDK\" version=\"~> 13.9\" minTargetSdk=\"15.0\" /></iosPods></dependencies>";

        [Test]
        public void TheHighestDeclaredVersionWins()
        {
            var android = new Dictionary<string, string>();
            var ios = new Dictionary<string, string>();

            HDCSdkVersions.Collect(Library, android, ios);
            HDCSdkVersions.Collect(Plugin, android, ios);

            Assert.AreEqual("25.4.0", android["com.google.android.gms:play-services-ads"]);
            Assert.AreEqual("0.3.5", android["com.hdc.adsmultiplatform:hdc-ads-android"]);
            Assert.AreEqual("~> 13.9", ios["Google-Mobile-Ads-SDK"]);
            Assert.IsFalse(ios.ContainsKey("FBAudienceNetwork"), "a pod without a version is not listed");
        }

        [TestCase("13.9", "13.10", -1)]
        [TestCase("~> 13.9", "13.9.0", 0)]
        [TestCase("6.22.0.0", "6.20.0.0", 1)]
        public void VersionsCompareByTheirNumbers(string left, string right, int sign) =>
            Assert.AreEqual(sign, System.Math.Sign(HDCSdkVersions.Compare(left, right)));

        [TestCase("/HDCLib/HDCAds/Editor/Templates~/HDCAdsDependencies.xml", true)]
        [TestCase("/.hidden/Dependencies.xml", true)]
        [TestCase("/GoogleMobileAds/Editor/GoogleMobileAdsDependencies.xml", false)]
        public void FoldersUnityDoesNotImportAreSkipped(string path, bool ignored) => Assert.AreEqual(ignored, HDCSdkVersions.IsIgnored(path));

        private const string PodfileLock =
            "PODS:\n  - Adjust (5.4.3):\n    - Adjust/Adjust (= 5.4.3)\n  - FBAudienceNetwork (6.22.0)\n  - Google-Mobile-Ads-SDK (13.10.0):\n    - GoogleUserMessagingPlatform (>= 1.1)\n" +
            "  - GoogleMobileAdsMediationFacebook (6.22.0.0):\n    - FBAudienceNetwork (= 6.22.0)\n  - GoogleUserMessagingPlatform (3.1.0)\n\n" +
            "DEPENDENCIES:\n  - Google-Mobile-Ads-SDK (~> 13.7)\n  - Adjust (= 5.4.3)\n\nCOCOAPODS: 1.16.2\n";

        private const string PackageResolved =
            "{\n  \"pins\" : [\n    {\n      \"identity\" : \"firebase-ios-sdk\",\n      \"kind\" : \"remoteSourceControl\",\n" +
            "      \"state\" : {\n        \"revision\" : \"abc\",\n        \"version\" : \"12.16.0\"\n      }\n    },\n" +
            "    {\n      \"identity\" : \"nanopb\",\n      \"state\" : {\n        \"version\" : \"2.30910.1\"\n      }\n    }\n  ]\n}";

        [Test]
        public void TheLockFileGivesTheResolvedPods()
        {
            Dictionary<string, string> pods = HDCSdkVersions.LockedPods(PodfileLock);

            Assert.AreEqual("13.10.0", pods["Google-Mobile-Ads-SDK"]);
            Assert.AreEqual("6.22.0", pods["FBAudienceNetwork"]);
            Assert.AreEqual("6.22.0.0", pods["GoogleMobileAdsMediationFacebook"]);
            Assert.IsFalse(pods.ContainsKey("Adjust/Adjust"), "nested requirements are not pods");
            Assert.AreEqual(5, pods.Count, "the DEPENDENCIES section is not read");
        }

        [Test]
        public void SwiftPackagesGiveTheirResolvedVersions()
        {
            Dictionary<string, string> pins = HDCSdkVersions.Pins(PackageResolved);

            Assert.AreEqual("12.16.0", pins["firebase-ios-sdk"]);
            Assert.AreEqual("2.30910.1", pins["nanopb"]);
        }

        [TestCase("6.20.0.0", "6.20.0")]
        [TestCase("13.3.0.1", "13.3.0")]
        [TestCase("7.2.0.6.0", "7.2.0.6")]
        [TestCase("13.9", "-")]
        [TestCase("-", "-")]
        public void TheNetworkSdkComesFromTheAdapterVersion(string adapter, string sdk) => Assert.AreEqual(sdk, HDCSdkVersions.NetworkSdk(adapter));

        [Test]
        public void EveryLibraryIsListedInItsGroup()
        {
            List<HDCSdkVersions.Entry> entries = HDCSdkVersions.Build(Sample());

            HDCSdkVersions.Entry ads = Named(entries, "Mobile Ads SDK");
            Assert.AreEqual(HDCSdkVersions.GoogleGroup, ads.Group);
            Assert.AreEqual("25.4.0", ads.Android);
            Assert.AreEqual("13.10.0", ads.Ios);
            Assert.IsFalse(ads.IosDeclared);
            StringAssert.Contains("declared ~> 13.9", ads.IosTip);
            Assert.AreEqual("com.google.android.gms:play-services-ads", ads.AndroidTip);

            Assert.AreEqual("0.3.5", Named(entries, "hdc-ads-android").Android);
            Assert.AreEqual("built 2026-10-09", Named(entries, "HDCAds.xcframework").Ios);
            HDCSdkVersions.Entry analytics = Named(entries, "Firebase Analytics");
            Assert.AreEqual("23.2.0", analytics.Android);
            Assert.AreEqual("12.16.0", analytics.Ios);
            Assert.AreEqual("23.1.0", Named(entries, "Firebase Remote Config").Android);
            CollectionAssert.AreEqual(new[] { HDCSdkVersions.HdcGroup, HDCSdkVersions.GoogleGroup, HDCSdkVersions.MediationGroup, HDCSdkVersions.AnalyticsGroup },
                entries.Select(entry => entry.Group).Distinct().ToArray());
        }

        [Test]
        public void EachNetworkShowsItsAdapterAndItsSdk()
        {
            List<HDCSdkVersions.Entry> rows = HDCSdkVersions.Build(Sample()).Where(entry => entry.Group == HDCSdkVersions.MediationGroup).ToList();

            CollectionAssert.AreEqual(new[] { "Meta Audience Network", "Adapter", "SDK" }, rows.Select(row => row.Name).ToArray());
            Assert.IsTrue(rows[0].Network);
            Assert.AreEqual("6.20.0.0", rows[1].Android);
            Assert.AreEqual("6.22.0.0", rows[1].Ios);
            Assert.AreEqual("6.20.0", rows[2].Android);
            Assert.AreEqual("6.22.0", rows[2].Ios);
            StringAssert.Contains("FBAudienceNetwork, resolved", rows[2].IosTip);
        }

        [Test]
        public void IosVersionsWithoutAnExportAreMarkedDeclared()
        {
            var sources = new HDCSdkVersions.Sources();
            HDCSdkVersions.Collect(Plugin + Library, sources.Android, sources.Ios);
            sources.Ios["GoogleMobileAdsMediationFacebook"] = "6.22.0.0";

            List<HDCSdkVersions.Entry> entries = HDCSdkVersions.Build(sources);

            HDCSdkVersions.Entry ads = Named(entries, "Mobile Ads SDK");
            Assert.AreEqual("~> 13.9", ads.Ios);
            Assert.IsTrue(ads.IosDeclared);
            StringAssert.Contains("Export once", ads.IosTip);
            HDCSdkVersions.Entry sdk = entries.Single(entry => entry.Group == HDCSdkVersions.MediationGroup && entry.Name == "SDK");
            Assert.AreEqual("6.22.0", sdk.Ios);
            Assert.IsTrue(sdk.IosDeclared);
        }

        [Test]
        public void EveryMediationNetworkGetsARow()
        {
            HDCSdkVersions.Sources sources = Sample();
            sources.Android["com.google.ads.mediation:applovin"] = "13.3.1.0";
            sources.Ios["GoogleMobileAdsMediationAppLovin"] = "13.3.1.0";
            sources.Android["com.google.ads.mediation:newnetwork"] = "2.1.0.0";

            string[] names = HDCSdkVersions.Mediation(sources).Where(entry => entry.Network).Select(entry => entry.Name).ToArray();

            CollectionAssert.AreEqual(new[] { "AppLovin", "Meta Audience Network", "Newnetwork" }, names);
        }

        [Test]
        public void EachTabListsOnlyItsOwnVersions()
        {
            List<HDCSdkVersions.Entry> entries = HDCSdkVersions.Build(Sample());

            string[] ios = HDCSdkVersions.ForPlatform(entries, true).Select(entry => entry.Name).ToArray();
            string[] android = HDCSdkVersions.ForPlatform(entries, false).Select(entry => entry.Name).ToArray();

            CollectionAssert.Contains(ios, "HDCAds.xcframework");
            CollectionAssert.DoesNotContain(ios, "hdc-ads-android");
            CollectionAssert.Contains(android, "hdc-ads-android");
            CollectionAssert.DoesNotContain(android, "HDCAds.xcframework");
            CollectionAssert.Contains(ios, "Meta Audience Network");
        }

        [Test]
        public void ANetworkWithoutAnAdapterOnThisPlatformIsHidden()
        {
            var sources = new HDCSdkVersions.Sources();
            sources.Android["com.google.ads.mediation:applovin"] = "13.3.1.0";
            List<HDCSdkVersions.Entry> entries = HDCSdkVersions.Build(sources);

            List<HDCSdkVersions.Entry> ios = HDCSdkVersions.ForPlatform(entries, true).Where(entry => entry.Group == HDCSdkVersions.MediationGroup).ToList();
            List<HDCSdkVersions.Entry> android = HDCSdkVersions.ForPlatform(entries, false).Where(entry => entry.Group == HDCSdkVersions.MediationGroup).ToList();

            CollectionAssert.AreEqual(new[] { "No mediation adapter" }, ios.Select(entry => entry.Name).ToArray());
            CollectionAssert.AreEqual(new[] { "AppLovin", "Adapter", "SDK" }, android.Select(entry => entry.Name).ToArray());
            Assert.AreEqual("13.3.1", android[2].Android);
        }

        [Test]
        public void AProjectWithoutAdaptersSaysSo()
        {
            var sources = new HDCSdkVersions.Sources();

            HDCSdkVersions.Entry row = HDCSdkVersions.Build(sources).Single(entry => entry.Group == HDCSdkVersions.MediationGroup);
            Assert.IsFalse(row.Network);

            Assert.AreEqual("No mediation adapter", row.Name);
        }

        [Test]
        public void TheProjectIsReadWithoutAnExport() =>
            Assert.AreEqual("HDCLib", HDCSdkVersions.Read(System.IO.Path.GetTempPath()).First().Name);

        private static HDCSdkVersions.Entry Named(IEnumerable<HDCSdkVersions.Entry> entries, string name) => entries.Single(entry => entry.Name == name);

        private static HDCSdkVersions.Sources Sample()
        {
            var sources = new HDCSdkVersions.Sources { LibraryVersion = "0.5.0", NativeIos = "built 2026-10-09", GmaPlugin = "11.4.0" };
            HDCSdkVersions.Collect(Plugin + Library, sources.Android, sources.Ios);
            sources.Android["com.google.ads.mediation:facebook"] = "6.20.0.0";
            sources.Android["com.google.firebase:firebase-analytics"] = "23.2.0";
            sources.Android["com.google.firebase:firebase-config"] = "23.1.0";
            sources.Android["com.google.firebase:firebase-app-unity"] = "13.14.0";
            sources.Ios["GoogleMobileAdsMediationFacebook"] = "6.22.0.0";
            foreach (KeyValuePair<string, string> pod in HDCSdkVersions.LockedPods(PodfileLock))
                sources.IosResolved[pod.Key] = pod.Value;
            foreach (KeyValuePair<string, string> pin in HDCSdkVersions.Pins(PackageResolved))
                sources.IosResolved["spm:" + pin.Key] = pin.Value;
            return sources;
        }
    }
}
