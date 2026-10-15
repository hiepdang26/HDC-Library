using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HDC.Tool.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HDC.Tool.Tests
{
    public class HDCBuildToolSpeedTests
    {
        private const string TempScene = "Assets/HDCToolSpeedTestScene0.unity";

        [Test]
        public void TheSimulatorLibrariesAreScannedOnceUntilTheProjectChanges()
        {
            HDCSimulatorSupport.Rescan();
            HDCSimulatorSupport.DeviceOnlyLibraries();
            int scans = HDCSimulatorSupport.Scans;
            for (int i = 0; i < 20; i++)
            {
                HDCSimulatorSupport.DeviceOnlyLibraries();
                HDCSimulatorSupport.BlockingLibraries();
            }

            Assert.AreEqual(scans, HDCSimulatorSupport.Scans, "drawing the window does not scan the plugins again");
            HDCSimulatorSupport.Rescan();
            HDCSimulatorSupport.BlockingLibraries();
            Assert.AreEqual(scans + 1, HDCSimulatorSupport.Scans, "a project change scans them once more");
        }

        [Test]
        public void SceneZeroIsInspectedAgainOnlyAfterItChanges()
        {
            EditorBuildSettingsScene[] saved = EditorBuildSettings.scenes;
            string source = saved.Select(scene => scene.path).FirstOrDefault(File.Exists);
            if (source == null)
                Assert.Ignore("The project has no build scene to copy.");
            try
            {
                Assert.IsTrue(AssetDatabase.CopyAsset(source, TempScene));
                EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(TempScene, true) };

                HDCScene0 inspected = HDCScene0.Inspect();
                Assert.IsTrue(inspected.Inspected);
                Assert.IsTrue(HDCScene0.IsCurrent(inspected), "an unchanged scene 0 is not opened again");
                File.SetLastWriteTimeUtc(TempScene, DateTime.UtcNow.AddMinutes(1));
                Assert.IsFalse(HDCScene0.IsCurrent(inspected), "a saved change is inspected again");
                Assert.IsFalse(HDCScene0.IsCurrent(null));
            }
            finally
            {
                EditorBuildSettings.scenes = saved;
                AssetDatabase.DeleteAsset(TempScene);
            }
        }

        [Test]
        public void FirebaseFilesAreFoundLikeTheFolderWalkFoundThem()
        {
            foreach (string file in new[] { "GoogleService-Info.plist", "google-services.json" })
            {
                bool walked = Directory.EnumerateFiles(Application.dataPath, file, SearchOption.AllDirectories)
                    .Any(path => !path.Substring(Application.dataPath.Length).Split('/', '\\').Any(part => part.EndsWith("~") || part.StartsWith(".")));
                Assert.AreEqual(walked, HDCBuildChecks.HasAsset(file), file);
            }

            Assert.IsFalse(HDCBuildChecks.HasAsset("no-such-file-anywhere.json"));
        }

        [Test]
        public void AScanFromTheWindowReusesTheAssetLookupsButABuildDoesNot()
        {
            HDCBuildSettings settings = HDCBuildSettings.Find();
            if (settings == null)
                Assert.Ignore("The project has no HDCBuildSettings asset.");
            HDCBuildRules rules = settings.Rules(HDCBuildPreset.TestDebug);
            var scene0 = new HDCScene0();
            string fresh = string.Join("\n", HDCBuildChecks.Run(HDCBuildPlatform.Android, HDCBuildPreset.TestDebug, rules, settings, scene0).Select(check => check.Name + check.Level));
            string cached = string.Join("\n", HDCBuildChecks.Run(HDCBuildPlatform.Android, HDCBuildPreset.TestDebug, rules, settings, scene0, true).Select(check => check.Name + check.Level));
            Assert.AreEqual(fresh, cached, "the cached scan finds the same problems");
        }

        [Test]
        public void TheSdkVersionsAreReadOffTheMainThread()
        {
            string export = HDCBuildFolders.IosExport(HDCAppleTarget.Phone);
            string dataPath = Application.dataPath;
            string root = HDCSdkVersions.HDCLibRoot();
            var read = Task.Run(() => HDCSdkVersions.Read(export, dataPath, root));
            Assert.IsTrue(read.Wait(TimeSpan.FromSeconds(30)));
            Assert.AreEqual(HDCSdkVersions.Read(export).Count, read.Result.Count);
        }
    }
}
