using System.IO;
using HDC.Tool.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace HDC.Tool.Tests
{
    public class HDCIconsTests
    {
        private const string Folder = "Assets/HDCIconsTests";
        private const string TexturePath = Folder + "/icon.png";

        private Texture2D texture;

        [SetUp]
        public void CreateTexture()
        {
            Directory.CreateDirectory(Folder);
            var source = new Texture2D(4, 4);
            File.WriteAllBytes(TexturePath, source.EncodeToPNG());
            Object.DestroyImmediate(source);
            AssetDatabase.ImportAsset(TexturePath);
            texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
        }

        [TearDown]
        public void RemoveTexture() => AssetDatabase.DeleteAsset(Folder);

        [Test]
        public void TheDefaultIconIsThePlayerSettingsOne()
        {
            Texture2D[] before = PlayerSettings.GetIcons(NamedBuildTarget.Unknown, IconKind.Any);
            try
            {
                HDCIcons.DefaultIcon = texture;

                Assert.AreEqual(texture, PlayerSettings.GetIcons(NamedBuildTarget.Unknown, IconKind.Any)[0]);
                Assert.AreEqual(texture, HDCIcons.DefaultIcon);
            }
            finally
            {
                PlayerSettings.SetIcons(NamedBuildTarget.Unknown, before, IconKind.Any);
            }
        }
    }
}
