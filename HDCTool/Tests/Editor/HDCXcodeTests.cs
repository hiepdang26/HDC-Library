using System.Collections.Generic;
using HDC.Tool.Editor;
using NUnit.Framework;

namespace HDC.Tool.Tests
{
    public class HDCXcodeTests
    {
        [Test]
        public void ALaunchedRunIsASuccess()
        {
            HDCXcode.Outcome outcome = HDCXcode.Read(0, "succeeded|\n", "building and running on Test iPhone");

            Assert.IsTrue(outcome.Ok);
            StringAssert.Contains("launched", outcome.Message);
        }

        [Test]
        public void AFailedRunCarriesXcodesMessage()
        {
            HDCXcode.Outcome outcome = HDCXcode.Read(0, "error occurred|Signing for \"Unity-iPhone\" requires a development team.", "");

            Assert.IsFalse(outcome.Ok);
            Assert.AreEqual("Xcode run error occurred: Signing for \"Unity-iPhone\" requires a development team.", outcome.Message);
        }

        [Test]
        public void AScriptErrorReadsWithoutTheAppleScriptNoise()
        {
            HDCXcode.Outcome outcome = HDCXcode.Read(1,
                "/tmp/HDCXcode.applescript:812:1004: execution error: Xcode has no run destination for Test iPhone. Unlock the iPhone. (-2700)", "");

            Assert.IsFalse(outcome.Ok);
            Assert.AreEqual("Xcode: Xcode has no run destination for Test iPhone. Unlock the iPhone.", outcome.Message);
        }

        [Test]
        public void ADeniedAutomationSaysWhereToAllowIt()
        {
            HDCXcode.Outcome outcome = HDCXcode.Read(1, "execution error: Not authorized to send Apple events to Xcode. (-1743)", "");

            StringAssert.Contains("Privacy & Security > Automation", outcome.Message);
        }

        [Test]
        public void OpeningOnlySelectsTheDevice() => Assert.IsTrue(HDCXcode.Read(0, "opened|", "").Ok);

        [Test]
        public void ASameNamedWorkspaceLeavesTheChoiceToTheUser()
        {
            HDCXcode.Outcome outcome = HDCXcode.Read(0, "opened-only|/Builds/Old/Unity-iPhone.xcworkspace/", "");

            Assert.IsTrue(outcome.Ok);
            StringAssert.Contains("(/Builds/Old/Unity-iPhone.xcworkspace)", outcome.Message);
            StringAssert.Contains("press Run in Xcode yourself", outcome.Message);
        }

        [Test]
        public void TheScriptRunsTheUnitySchemeOnTheChosenDevice()
        {
            StringAssert.Contains("set active scheme of item i of workspace documents to scheme \"Unity-iPhone\" of item i of workspace documents", HDCXcode.Script);
            StringAssert.Contains("set active run destination of item i of workspace documents to run destination chosen of item i of workspace documents", HDCXcode.Script);
            StringAssert.Contains("tell application \"Xcode\" to run item i of workspace documents", HDCXcode.Script);
            StringAssert.Contains("set other to my sameNamedElsewhere(workspacePath)", HDCXcode.Script, "a workspace with the same name elsewhere stops the script");
            StringAssert.Contains("starts with \"My Mac\" then set chosen to k", HDCXcode.Script, "My Mac is found by its destination name");
            StringAssert.Contains("does not contain \"Missing package product\"", HDCXcode.Script, "a run retries while Xcode still resolves the Swift packages");
        }

        [Test]
        public void TheScriptCompiles()
        {
            if (UnityEngine.Application.platform != UnityEngine.RuntimePlatform.OSXEditor)
                Assert.Ignore("AppleScript needs macOS.");
            string folder = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "hdc-xcode-" + System.Guid.NewGuid().ToString("N"));
            System.IO.Directory.CreateDirectory(folder);
            try
            {
                string source = System.IO.Path.Combine(folder, "run.applescript");
                System.IO.File.WriteAllText(source, HDCXcode.Script);

                HDCProcess.Output output = HDCProcess.Run("/usr/bin/osacompile", $"-o \"{System.IO.Path.Combine(folder, "run.scpt")}\" \"{source}\"", 60000);

                Assert.IsTrue(output.Ok, output.All);
            }
            finally
            {
                System.IO.Directory.Delete(folder, true);
            }
        }

        [Test]
        public void TheSwiftPackagesAreReadyBeforeXcodeBuilds()
        {
            StringAssert.Contains("xcodebuild -resolvePackageDependencies -workspace \" & quoted form of workspacePath & \" -scheme \" & quoted form of \"Unity-iPhone\"",
                HDCXcode.Script);
            StringAssert.Contains("if packages is \"resolve\" then my resolvePackages(workspacePath)\n\tset wasOpen to", HDCXcode.Script,
                "packages resolve before Xcode opens the workspace");
            StringAssert.Contains("then exit repeat\n\t\tmy resolvePackages(workspacePath)\n\t\tdelay 3", HDCXcode.Script,
                "a retry waits for the packages instead of starting another build that cancels their download");
            StringAssert.Contains("if wasOpen then", HDCXcode.Script, "a workspace Xcode just opened is not stopped, which would cancel its package download");
        }

        [Test]
        public void OnlyARunAsksForThePackages()
        {
            var device = new HDCAppleDevice { Kind = HDCAppleTarget.Simulator, Name = "iPhone 17", Udid = "F5061B51" };

            StringAssert.EndsWith("\"F5061B51\" \"iPhone 17\" run resolve", HDCXcode.Arguments("s.applescript", "/b/Unity-iPhone.xcworkspace", device, true, true));
            StringAssert.EndsWith("\"iPhone 17\" run", HDCXcode.Arguments("s.applescript", "/b/Unity-iPhone.xcworkspace", device, true, false));
            StringAssert.EndsWith("\"\" \"\" open", HDCXcode.Arguments("s.applescript", "/b/Unity-iPhone.xcworkspace", null, false, false));
        }

        [Test]
        public void PackagesThatXcodeAlreadyCheckedOutAreNotFetchedAgain()
        {
            string derivedData = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "hdc-dd-" + System.Guid.NewGuid().ToString("N"));
            const string workspace = "/Users/me/Build/Game & Co/Simulator/Unity-iPhone.xcworkspace";
            string folder = System.IO.Path.Combine(derivedData, "Unity-iPhone-abc");
            System.IO.Directory.CreateDirectory(folder);
            try
            {
                System.IO.File.WriteAllText(System.IO.Path.Combine(folder, "info.plist"),
                    "<plist version=\"1.0\">\n<dict>\n\t<key>WorkspacePath</key>\n\t<string>/Users/me/Build/Game &amp; Co/Simulator/Unity-iPhone.xcworkspace</string>\n</dict>\n</plist>\n");
                Assert.IsFalse(HDCXcode.PackagesReady(workspace, derivedData), "no checkouts yet");

                System.IO.Directory.CreateDirectory(System.IO.Path.Combine(folder, "SourcePackages", "checkouts", "firebase-ios-sdk"));
                Assert.IsTrue(HDCXcode.PackagesReady(workspace, derivedData));
                Assert.IsTrue(HDCXcode.PackagesReady(workspace + "/", derivedData));
                Assert.IsFalse(HDCXcode.PackagesReady("/Users/me/Build/Game & Co/Unity-iPhone.xcworkspace", derivedData), "the device export is another workspace");
                Assert.IsFalse(HDCXcode.PackagesReady(workspace, System.IO.Path.Combine(derivedData, "missing")));
            }
            finally
            {
                System.IO.Directory.Delete(derivedData, true);
            }
        }

        [Test]
        public void SwiftPackagesAreFoundInTheExportedProject()
        {
            string folder = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "hdc-spm-" + System.Guid.NewGuid().ToString("N"));
            string project = System.IO.Path.Combine(folder, "Unity-iPhone.xcodeproj");
            System.IO.Directory.CreateDirectory(project);
            try
            {
                string workspace = System.IO.Path.Combine(folder, "Unity-iPhone.xcworkspace");
                System.IO.File.WriteAllText(System.IO.Path.Combine(project, "project.pbxproj"), "isa = PBXNativeTarget;");
                Assert.IsFalse(HDCXcode.UsesSwiftPackages(workspace));

                System.IO.File.WriteAllText(System.IO.Path.Combine(project, "project.pbxproj"), "isa = XCRemoteSwiftPackageReference;");
                Assert.IsTrue(HDCXcode.UsesSwiftPackages(workspace));
                Assert.IsTrue(HDCXcode.UsesSwiftPackages(workspace + "/"));
                Assert.IsFalse(HDCXcode.UsesSwiftPackages(System.IO.Path.Combine(folder, "missing", "Unity-iPhone.xcworkspace")));
            }
            finally
            {
                System.IO.Directory.Delete(folder, true);
            }
        }

        [Test]
        public void TheBuildErrorsBecomeTheMessage()
        {
            HDCXcode.Outcome outcome = HDCXcode.Read(0, "failed|Missing package product 'FirebaseCore'; Missing package product 'FirebaseAnalytics'; ", "");

            Assert.AreEqual("Xcode run failed: Missing package product 'FirebaseCore'; Missing package product 'FirebaseAnalytics'", outcome.Message);
        }

        [Test]
        public void EachRunTargetSaysWhatIsMissing()
        {
            var none = new List<string>();
            StringAssert.StartsWith("No iPhone is connected", HDCBuildRunner.IosRunProblem(null, HDCAppleTarget.Phone, () => true, () => none));
            StringAssert.StartsWith("No iOS simulator", HDCBuildRunner.IosRunProblem(null, HDCAppleTarget.Simulator, () => true, () => none));
            StringAssert.StartsWith("Running on My Mac needs a Mac with Apple silicon", HDCBuildRunner.IosRunProblem(null, HDCAppleTarget.Mac, () => true, () => none));
        }

        [Test]
        public void DevicesAndMyMacNeedATeamButSimulatorsDoNot()
        {
            var none = new List<string>();
            var phone = new HDCAppleDevice { Kind = HDCAppleTarget.Phone, Name = "Test iPhone" };
            var mac = new HDCAppleDevice { Kind = HDCAppleTarget.Mac, Name = HDCAppleDevice.MacName };
            var simulator = new HDCAppleDevice { Kind = HDCAppleTarget.Simulator, Name = "iPhone 17" };

            StringAssert.Contains("needs a team", HDCBuildRunner.IosRunProblem(phone, HDCAppleTarget.Phone, () => false, () => none));
            StringAssert.Contains("My Mac signs the app", HDCBuildRunner.IosRunProblem(mac, HDCAppleTarget.Mac, () => false, () => none));
            Assert.IsNull(HDCBuildRunner.IosRunProblem(simulator, HDCAppleTarget.Simulator, () => false, () => none));
            Assert.IsNull(HDCBuildRunner.IosRunProblem(phone, HDCAppleTarget.Phone, () => true, () => none));
        }

        [Test]
        public void ASimulatorRunStopsOnlyOnLibrariesTheToolCannotMark()
        {
            var simulator = new HDCAppleDevice { Kind = HDCAppleTarget.Simulator, Name = "iPhone 17" };

            string problem = HDCBuildRunner.IosRunProblem(simulator, HDCAppleTarget.Simulator, () => true, () => new List<string> { "libOld.a", "libBitcode.a" });

            Assert.AreEqual("The simulator build cannot link: libOld.a, libBitcode.a have arm64 code that the tool cannot mark for the simulator. " +
                            "Run on an iPhone or My Mac instead.", problem);
            Assert.IsNull(HDCBuildRunner.IosRunProblem(simulator, HDCAppleTarget.Simulator, () => true, () => new List<string>()));
        }

        [Test]
        public void TheSimulatorNoteNamesTheMarkedLibraries() =>
            StringAssert.StartsWith("libFirebaseCppApp.a, libFirebaseCppAnalytics.a have arm64 code built for devices only. The simulator export marks",
                HDCBuildRunner.SimulatorNote(new[] { "libFirebaseCppApp.a", "libFirebaseCppAnalytics.a" }));

        [Test]
        public void TheCommandLineExportsForAnIPhoneUnlessAskedOtherwise()
        {
            Assert.AreEqual(HDCAppleTarget.Phone, HDCBuildRunner.IosTargetFrom(new[] { "Unity" }));
            Assert.AreEqual(HDCAppleTarget.Simulator, HDCBuildRunner.IosTargetFrom(new[] { "Unity", "-hdcIosTarget", "simulator" }));
            Assert.AreEqual(HDCAppleTarget.Mac, HDCBuildRunner.IosTargetFrom(new[] { "Unity", "-hdcIosTarget", "Mac" }));
            Assert.AreEqual(HDCAppleTarget.Phone, HDCBuildRunner.IosTargetFrom(new[] { "Unity", "-hdcIosTarget", "tv" }));
        }
    }
}
