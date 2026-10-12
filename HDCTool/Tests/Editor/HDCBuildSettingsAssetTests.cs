using System.IO;
using System.Text.RegularExpressions;
using HDC.Tool.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace HDC.Tool.Tests
{
    public class HDCBuildSettingsAssetTests
    {
        private const string Folder = "Assets/HDCBuildSettingsAssetTests";
        private const string AssetPath = Folder + "/HDCBuildSettings.asset";

        [TearDown]
        public void RemoveFolder()
        {
            AssetDatabase.DeleteAsset(Folder);
            FileUtil.DeleteFileOrDirectory(Folder);
            FileUtil.DeleteFileOrDirectory(Folder + ".meta");
            AssetDatabase.Refresh();
        }

        [Test]
        public void AFileUnityCannotLoadIsNeverReplaced()
        {
            Directory.CreateDirectory(Folder);
            File.WriteAllText(AssetPath, "the team's presets");
            LogAssert.Expect(LogType.Warning, new Regex("left untouched"));

            Assert.IsNull(HDCBuildSettings.CreateAt(AssetPath));
            Assert.AreEqual("the team's presets", File.ReadAllText(AssetPath));
        }

        [Test]
        public void AMissingAssetIsCreatedOnceAndThenKept()
        {
            HDCBuildSettings created = HDCBuildSettings.CreateAt(AssetPath);
            Assert.IsNotNull(created);
            Assert.AreEqual(created, AssetDatabase.LoadAssetAtPath<HDCBuildSettings>(AssetPath));
            string saved = File.ReadAllText(AssetPath);
            LogAssert.Expect(LogType.Warning, new Regex("left untouched"));

            Assert.IsNull(HDCBuildSettings.CreateAt(AssetPath));
            Assert.AreEqual(saved, File.ReadAllText(AssetPath));
        }

        [Test]
        public void TheMessageSaysNothingWasChanged()
        {
            StringAssert.Contains(HDCBuildSettings.AssetPath, HDCBuildSettings.NotReady);
            StringAssert.Contains("Nothing was changed", HDCBuildSettings.NotReady);
        }
    }
}
