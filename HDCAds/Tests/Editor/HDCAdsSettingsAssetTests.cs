using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace HDC.Ads.Tests
{
    public class HDCAdsSettingsAssetTests
    {
        private const string Folder = "Assets/HDCSettingsAssetTests";
        private const string AssetPath = Folder + "/HDCAdsSettings.asset";

        private static Type Asset => AppDomain.CurrentDomain.GetAssemblies().Select(assembly => assembly.GetType("HDC.Ads.Editor.HDCAdsSettingsAsset", false))
            .First(type => type != null);

        private static HDCAdsSettings CreateAt(string path) =>
            (HDCAdsSettings)Asset.GetMethod("CreateAt", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { path });

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
            File.WriteAllText(AssetPath, "the game's settings");
            LogAssert.Expect(LogType.Warning, new Regex("left untouched"));

            Assert.IsNull(CreateAt(AssetPath));
            Assert.AreEqual("the game's settings", File.ReadAllText(AssetPath));
        }

        [Test]
        public void AMissingAssetIsCreatedOnceAndThenKept()
        {
            HDCAdsSettings created = CreateAt(AssetPath);
            Assert.IsNotNull(created);
            Assert.AreEqual(created, AssetDatabase.LoadAssetAtPath<HDCAdsSettings>(AssetPath));
            string saved = File.ReadAllText(AssetPath);
            LogAssert.Expect(LogType.Warning, new Regex("left untouched"));

            Assert.IsNull(CreateAt(AssetPath));
            Assert.AreEqual(saved, File.ReadAllText(AssetPath));
        }

        [Test]
        public void TheDefaultPathIsTheOneBuildsRead()
        {
            var path = (string)Asset.GetField("DefaultPath", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);

            Assert.AreEqual("Assets/HDCAds/Resources/" + HDCAdsSettings.ResourceName + ".asset", path);
        }
    }
}
