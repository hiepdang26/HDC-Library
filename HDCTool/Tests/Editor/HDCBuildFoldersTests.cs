using System;
using System.IO;
using HDC.Tool.Editor;
using NUnit.Framework;
using UnityEditor;

namespace HDC.Tool.Tests
{
    public class HDCBuildFoldersTests
    {
        private const string FolderKey = "HDC.Build.Folder";
        private string saved;
        private string scratch;

        [SetUp]
        public void Save()
        {
            saved = EditorUserSettings.GetConfigValue(FolderKey) ?? "";
            scratch = Path.Combine(Path.GetTempPath(), "hdc-build-folders-" + Guid.NewGuid().ToString("N"));
        }

        [TearDown]
        public void Restore()
        {
            EditorUserSettings.SetConfigValue(FolderKey, saved);
            if (Directory.Exists(scratch))
                Directory.Delete(scratch, true);
        }

        [TestCase("/Users/me/Documents/Workspace/UnityProject/SampleAd-HDC", "/Users/me/Documents/Workspace/Build/SampleAd-HDC")]
        [TestCase("/Users/me/Documents/Workspace/UnityProject/SampleAd-HDC/", "/Users/me/Documents/Workspace/Build/SampleAd-HDC")]
        public void TheDefaultIsTheWorkspaceBuildFolderOfTheProject(string projectRoot, string expected) =>
            Assert.AreEqual(expected, HDCBuildFolders.DefaultBase(projectRoot));

        [Test]
        public void TheDefaultIsOutsideTheProject() =>
            Assert.IsFalse(HDCBuildFolders.IsInside(HDCBuildFolders.DefaultBase(HDCBuildFolders.ProjectRoot), HDCBuildFolders.ProjectRoot));

        [Test]
        public void IosExportsIntoTheFolderAndAndroidIntoItsAndroidFolder()
        {
            Assert.AreEqual("/Work/Build/Game", HDCBuildFolders.For("/Work/Build/Game", HDCBuildPlatform.Ios));
            Assert.AreEqual(Path.Combine("/Work/Build/Game", "Android"), HDCBuildFolders.For("/Work/Build/Game", HDCBuildPlatform.Android));
        }

        [Test]
        public void SimulatorExportsGetTheirOwnFolder()
        {
            Assert.AreEqual("/Work/Build/Game", HDCBuildFolders.IosExport("/Work/Build/Game", HDCAppleTarget.Phone));
            Assert.AreEqual("/Work/Build/Game", HDCBuildFolders.IosExport("/Work/Build/Game", HDCAppleTarget.Mac));
            Assert.AreEqual(Path.Combine("/Work/Build/Game", "Simulator"), HDCBuildFolders.IosExport("/Work/Build/Game", HDCAppleTarget.Simulator));
        }

        [TestCase("/Work/UnityProject/Game", true)]
        [TestCase("/Work/UnityProject/Game/Builds/iOS", true)]
        [TestCase("/Work/UnityProject/game/Builds", true)]
        [TestCase("/Work/UnityProject/Game2", false)]
        [TestCase("/Work/Build/Game", false)]
        public void FoldersInsideTheProjectAreRecognised(string folder, bool inside) =>
            Assert.AreEqual(inside, HDCBuildFolders.IsInside(folder, "/Work/UnityProject/Game"));

        [Test]
        public void AFolderInsideTheProjectIsRefused()
        {
            EditorUserSettings.SetConfigValue(FolderKey, "");

            string problem = HDCBuildFolders.Choose(Path.Combine(HDCBuildFolders.ProjectRoot, "Builds"));

            StringAssert.Contains("inside the Unity project", problem);
            Assert.IsTrue(HDCBuildFolders.IsDefault);
            Assert.AreEqual(HDCBuildFolders.DefaultBase(HDCBuildFolders.ProjectRoot), HDCBuildFolders.Base);
        }

        [Test]
        public void AChosenFolderIsKeptForThisComputerUntilTheDefaultIsUsed()
        {
            Assert.IsNull(HDCBuildFolders.Choose(scratch));
            Assert.AreEqual(Path.GetFullPath(scratch), HDCBuildFolders.Base);
            Assert.IsFalse(HDCBuildFolders.IsDefault);

            HDCBuildFolders.UseDefault();

            Assert.AreEqual(HDCBuildFolders.DefaultBase(HDCBuildFolders.ProjectRoot), HDCBuildFolders.Base);
        }

        [Test]
        public void AStoredFolderInsideTheProjectFallsBackToTheDefault()
        {
            EditorUserSettings.SetConfigValue(FolderKey, Path.Combine(HDCBuildFolders.ProjectRoot, "Builds"));

            Assert.AreEqual(HDCBuildFolders.DefaultBase(HDCBuildFolders.ProjectRoot), HDCBuildFolders.Base);
        }

        [Test]
        public void AReplaceExportKeepsTheAndroidBuildsAndReports()
        {
            string export = Path.Combine(scratch, "Game");
            Directory.CreateDirectory(Path.Combine(export, "Android"));
            File.WriteAllText(Path.Combine(export, "Android", "Game_APK.apk"), "apk");
            Directory.CreateDirectory(Path.Combine(export, "Simulator"));
            File.WriteAllText(Path.Combine(export, "Simulator", "Info.plist"), "simulator");
            File.WriteAllText(Path.Combine(export, "Game_Xcode" + HDCBuildReport.Suffix), "report");
            File.WriteAllText(Path.Combine(export, "Info.plist"), "xcode");

            string kept = HDCBuildRunner.KeepAside(export);
            Directory.Delete(export, true);
            HDCBuildRunner.Restore(kept, export);

            Assert.AreEqual("apk", File.ReadAllText(Path.Combine(export, "Android", "Game_APK.apk")));
            Assert.AreEqual("simulator", File.ReadAllText(Path.Combine(export, "Simulator", "Info.plist")));
            Assert.AreEqual("report", File.ReadAllText(Path.Combine(export, "Game_Xcode" + HDCBuildReport.Suffix)));
            Assert.IsFalse(Directory.Exists(kept), "the kept folder is removed once everything is back");
            Assert.IsNull(HDCBuildRunner.KeepAside(Path.Combine(scratch, "Missing")));
        }
    }
}
