using System;
using System.Collections.Generic;
using System.Linq;
using HDC.Tool.Editor;
using NUnit.Framework;
using UnityEditor;

namespace HDC.Tool.Tests
{
    public class HDCBuildFoldTests
    {
        private readonly List<string> keys = new List<string>();
        private Dictionary<string, bool> saved;

        [SetUp]
        public void Remember() => saved = HDCBuildGui.CardKeys.ToDictionary(key => key, key => HDCBuildGui.IsOpen(key));

        [TearDown]
        public void Restore()
        {
            foreach (string key in keys)
            {
                HDCBuildGui.Forget(key);
                EditorPrefs.DeleteKey(HDCBuildGui.FoldoutKey(key));
            }

            keys.Clear();
            foreach (KeyValuePair<string, bool> card in saved)
                HDCBuildGui.SetOpen(card.Key, card.Value);
        }

        [Test]
        public void ExpandAllAndCollapseAllReachEveryCard()
        {
            string open = Card(true);
            string closed = Card(false);
            string other = Card(true);
            Assert.IsFalse(HDCBuildGui.AllOpen);
            Assert.IsFalse(HDCBuildGui.AllClosed);

            HDCBuildGui.SetAll(false);
            Assert.IsTrue(HDCBuildGui.AllClosed);
            Assert.IsFalse(HDCBuildGui.AllOpen);
            foreach (string key in new[] { open, closed, other })
                Assert.IsFalse(EditorPrefs.GetBool(HDCBuildGui.FoldoutKey(key), true), key + " is saved closed");

            HDCBuildGui.SetAll(true);
            Assert.IsTrue(HDCBuildGui.AllOpen);
            Assert.IsFalse(HDCBuildGui.AllClosed);
            foreach (string key in new[] { open, closed, other })
                Assert.IsTrue(EditorPrefs.GetBool(HDCBuildGui.FoldoutKey(key), false), key + " is saved open");
        }

        [Test]
        public void ACardKeepsItsStateAfterAReload()
        {
            string key = Card(true);
            HDCBuildGui.SetOpen(key, false);
            HDCBuildGui.Forget(key);

            HDCBuildGui.Register(key, true);
            Assert.IsFalse(HDCBuildGui.IsOpen(key), "the saved state wins over the card's default");
        }

        [Test]
        public void EachCardIsListedOnce()
        {
            string key = Card(true);
            HDCBuildGui.Register(key, false);

            Assert.AreEqual(1, HDCBuildGui.CardKeys.Count(card => card == key));
            Assert.IsTrue(HDCBuildGui.IsOpen(key), "registering again keeps the current state");
        }

        [Test]
        public void AFixThatOpensACardShowsItOpen()
        {
            string key = Card(false);
            HDCBuildChecks.OpenFoldout(key);

            Assert.IsTrue(HDCBuildGui.IsOpen(key));
            Assert.IsTrue(EditorPrefs.GetBool(HDCBuildGui.FoldoutKey(key), false));
        }

        private string Card(bool open)
        {
            string key = "Test." + Guid.NewGuid().ToString("N");
            keys.Add(key);
            HDCBuildGui.Register(key, open);
            return key;
        }
    }
}
