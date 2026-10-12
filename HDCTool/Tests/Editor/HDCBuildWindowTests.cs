using HDC.Tool.Editor;
using NUnit.Framework;
using UnityEngine;

namespace HDC.Tool.Tests
{
    public class HDCBuildWindowTests
    {
        [Test]
        public void ANewWindowTakesTheRightHalfAndTheFullHeight() =>
            Assert.AreEqual(new Rect(756f, 25f, 756f, 920f), HDCBuildToolWindow.DefaultPosition(new Rect(0f, 25f, 1512f, 920f)));

        [Test]
        public void ANarrowScreenStillGetsTheMinimumWidth()
        {
            Assert.AreEqual(new Rect(440f, 0f, HDCBuildToolWindow.MinWidth, 700f), HDCBuildToolWindow.DefaultPosition(new Rect(0f, 0f, 1000f, 700f)));
            Assert.AreEqual(new Rect(0f, 0f, 500f, 700f), HDCBuildToolWindow.DefaultPosition(new Rect(0f, 0f, 500f, 700f)), "never wider than the screen");
        }

        [Test]
        public void ASecondScreenKeepsItsOffset() =>
            Assert.AreEqual(new Rect(-960f, 0f, 960f, 1055f), HDCBuildToolWindow.DefaultPosition(new Rect(-1920f, 0f, 1920f, 1055f)));

        [Test]
        public void ShiftBTogglesTheBuildTool()
        {
            const System.Reflection.BindingFlags statics = System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic;
            var item = (UnityEditor.MenuItem)System.Attribute.GetCustomAttribute(typeof(HDCBuildToolWindow).GetMethod("Toggle", statics), typeof(UnityEditor.MenuItem));

            Assert.AreEqual("HDC/Build Tool #b", item.menuItem);
            Assert.IsNull(System.Attribute.GetCustomAttribute(typeof(HDCBuildToolWindow).GetMethod("Open", statics), typeof(UnityEditor.MenuItem)),
                "Open only opens, so buttons that call it never close the tool");
        }

        [Test]
        public void FooterButtonsKeepTheirWidthWhenTheyFit() =>
            CollectionAssert.AreEqual(new[] { 250f, 170f, 140f }, HDCBuildAreaSection.FitInto(872f, 250f, 170f, 140f));

        [Test]
        public void FooterButtonsShrinkTogetherInANarrowWindow()
        {
            float[] widths = HDCBuildAreaSection.FitInto(532f, 250f, 170f, 140f);

            Assert.LessOrEqual(widths[0] + widths[1] + widths[2] + 4f * 4f, 532f);
            Assert.AreEqual(250f / 140f, widths[0] / widths[2], 0.02f);
        }
    }
}
