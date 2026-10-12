using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace HDC.Ads.Tests
{
    public class HDCAdsConfigWindowTests
    {
        private const BindingFlags Statics = BindingFlags.NonPublic | BindingFlags.Static;

        private static Type Window => AppDomain.CurrentDomain.GetAssemblies().Select(assembly => assembly.GetType("HDC.Ads.Editor.HDCAdsConfigWindow", false))
            .First(type => type != null);

        [TestCase(0, 0, 0, 0)]
        [TestCase(0, 1, 0, 1)]
        [TestCase(0, 0, 1, 4)]
        [TestCase(0, 1, 1, 5)]
        [TestCase(1, 0, 0, 2)]
        [TestCase(1, 1, 0, 3)]
        [TestCase(1, 0, 1, 6)]
        [TestCase(1, 1, 1, 7)]
        public void EachTabShowsTheRightSettingsField(int card, int platform, int variant, int page) =>
            Assert.AreEqual(page, Window.GetMethod("PageOf", Statics).Invoke(null, new object[] { card, platform, variant }));

        [Test]
        public void TheMenuPagesStillPointAtTheirTabs()
        {
            Assert.AreEqual(0, Window.GetField("AdsAndroidPage", Statics).GetValue(null));
            Assert.AreEqual(2, Window.GetField("CoreAndroidPage", Statics).GetValue(null));
            Assert.AreEqual(3, Window.GetField("CoreIosPage", Statics).GetValue(null));
            Assert.AreEqual(4, Window.GetField("CountryAdsAndroidPage", Statics).GetValue(null));
        }

        [Test]
        public void TheWindowOpensInTheMiddleOfTheEditor()
        {
            MethodInfo centered = Window.GetMethod("Centered", Statics);

            Assert.AreEqual(new UnityEngine.Rect(306f, 117f, 900f, 736f), centered.Invoke(null, new object[] { new UnityEngine.Rect(0f, 25f, 1512f, 920f), 900f, 736f }));
            Assert.AreEqual(new UnityEngine.Rect(0f, 0f, 800f, 600f), centered.Invoke(null, new object[] { new UnityEngine.Rect(0f, 0f, 800f, 600f), 900f, 736f }),
                "never larger than the screen");
        }

        [Test]
        public void ShiftSemicolonTogglesTheWindow()
        {
            Type menu = AppDomain.CurrentDomain.GetAssemblies().Select(assembly => assembly.GetType("HDC.Ads.Editor.HDCAdsMenu", false)).First(type => type != null);
            var item = (UnityEditor.MenuItem)menu.GetMethod("ToggleAdsSettings", Statics).GetCustomAttribute(typeof(UnityEditor.MenuItem));

            Assert.AreEqual("HDC/Ads Settings #;", item.menuItem);
        }

        [Test]
        public void UnityBindsShiftSemicolonToTheWindow()
        {
            UnityEditor.ShortcutManagement.KeyCombination key = UnityEditor.ShortcutManagement.ShortcutManager.instance
                .GetShortcutBinding("Main Menu/HDC/Ads Settings").keyCombinationSequence.Single();

            Assert.AreEqual(UnityEngine.KeyCode.Semicolon, key.keyCode);
            Assert.AreEqual(UnityEditor.ShortcutManagement.ShortcutModifiers.Shift, key.modifiers);
        }

        [Test]
        public void CommaListsDropBlanks() =>
            CollectionAssert.AreEqual(new[] { "vi", "en" }, (string[])Window.GetMethod("Split", Statics).Invoke(null, new object[] { " vi, en ,, " }));
    }
}
