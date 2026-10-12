using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using HDC.Ads.DebugUI;
using NUnit.Framework;
using UnityEngine;

namespace HDC.Ads.Tests
{
    public class HDCAppDataTests
    {
        private string root;

        [SetUp]
        public void MakeFolder()
        {
            root = Path.Combine(Path.GetTempPath(), "hdc-app-data-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
        }

        [TearDown]
        public void RemoveFolder()
        {
            if (Directory.Exists(root))
                Directory.Delete(root, true);
        }

        [Test]
        public void EmptyingAFolderRemovesAllButWhatIsKept()
        {
            string data = Path.Combine(root, "data");
            Write(data, "a.txt");
            Write(data, "shared_prefs/com.game.v2.playerprefs.xml");
            Write(data, "shared_prefs/sdk.xml");
            Write(data, "files/deep/nested/b.bin");
            Write(data, "lib/libgame.so");

            int removed = HDCAppData.Empty(data, path =>
                path.EndsWith(".v2.playerprefs.xml", StringComparison.Ordinal) || Path.GetFileName(path) == "lib");

            Assert.AreEqual(3, removed);
            Assert.IsTrue(Directory.Exists(data), "the folder itself stays");
            CollectionAssert.AreEquivalent(
                new[] { "lib/libgame.so", "shared_prefs/com.game.v2.playerprefs.xml" },
                Directory.GetFiles(data, "*", SearchOption.AllDirectories).Select(path => path.Substring(data.Length + 1).Replace('\\', '/')));
            Assert.IsFalse(Directory.Exists(Path.Combine(data, "files")), "emptied folders go too");
        }

        [Test]
        public void EmptyingAFolderDoesNotFollowLinks()
        {
            Assume.That(Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.LinuxEditor, "symbolic links via libc");
            string data = Path.Combine(root, "data");
            string outside = Path.Combine(root, "outside");
            Write(data, "a.txt");
            string kept = Write(outside, "native.so");
            Assert.AreEqual(0, symlink(outside, Path.Combine(data, "link")), "symlink");

            HDCAppData.Empty(data, null);

            Assert.IsTrue(File.Exists(kept), "a linked folder is not emptied");
            Assert.IsFalse(File.Exists(Path.Combine(data, "a.txt")));
        }

        [Test]
        public void AMissingFolderIsNothingToDo()
        {
            Assert.AreEqual(0, HDCAppData.Empty(Path.Combine(root, "missing"), null));
            Assert.AreEqual(0, HDCAppData.Empty(null, null));
        }

        private static string Write(string folder, string relative)
        {
            string path = Path.Combine(folder, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, "x");
            return path;
        }

        [DllImport("libc", SetLastError = true)]
        private static extern int symlink(string target, string linkPath);
    }
}
