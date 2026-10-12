using HDC.Ads;
using HDC.Tool.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HDC.Tool.Tests
{
    public class HDCOrientationTests
    {
        [TestCase(UIOrientation.Portrait, false, false, true, true, "Portrait")]
        [TestCase(UIOrientation.LandscapeLeft, true, false, false, false, "Landscape")]
        [TestCase(UIOrientation.AutoRotation, true, true, false, false, "Portrait")]
        [TestCase(UIOrientation.AutoRotation, false, false, true, true, "Landscape")]
        [TestCase(UIOrientation.AutoRotation, false, false, false, true, "Landscape")]
        [TestCase(UIOrientation.AutoRotation, true, false, true, true, "Other")]
        public void TheOrientationReadsFromTheStartSideAndTheAllowedSides(UIOrientation start, bool portrait, bool upsideDown, bool left, bool right, string expected) =>
            Assert.AreEqual(expected, HDCOrientations.Of(start, portrait, upsideDown, left, right).ToString());

        [Test]
        public void PickingAnOrientationSetsThePlayerSettings()
        {
            UIOrientation start = PlayerSettings.defaultInterfaceOrientation;
            bool portrait = PlayerSettings.allowedAutorotateToPortrait;
            bool upsideDown = PlayerSettings.allowedAutorotateToPortraitUpsideDown;
            bool left = PlayerSettings.allowedAutorotateToLandscapeLeft;
            bool right = PlayerSettings.allowedAutorotateToLandscapeRight;
            try
            {
                HDCOrientations.Apply(HDCOrientation.Landscape);
                Assert.AreEqual(HDCOrientation.Landscape, HDCOrientations.Current);
                Assert.AreEqual(UIOrientation.AutoRotation, PlayerSettings.defaultInterfaceOrientation);
                Assert.IsTrue(PlayerSettings.allowedAutorotateToLandscapeLeft && PlayerSettings.allowedAutorotateToLandscapeRight);
                Assert.IsFalse(PlayerSettings.allowedAutorotateToPortrait || PlayerSettings.allowedAutorotateToPortraitUpsideDown);

                HDCOrientations.Apply(HDCOrientation.Portrait);
                Assert.AreEqual(HDCOrientation.Portrait, HDCOrientations.Current);
                Assert.AreEqual(UIOrientation.Portrait, PlayerSettings.defaultInterfaceOrientation);
            }
            finally
            {
                Restore(start, portrait, upsideDown, left, right);
            }
        }

        private static void Restore(UIOrientation start, bool portrait, bool upsideDown, bool left, bool right)
        {
            bool any = portrait || upsideDown || left || right;
            if (portrait || !any)
                PlayerSettings.allowedAutorotateToPortrait = true;
            if (upsideDown)
                PlayerSettings.allowedAutorotateToPortraitUpsideDown = true;
            if (left)
                PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            if (right)
                PlayerSettings.allowedAutorotateToLandscapeRight = true;
            if (!portrait && any)
                PlayerSettings.allowedAutorotateToPortrait = false;
            if (!upsideDown)
                PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            if (!left)
                PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            if (!right)
                PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.defaultInterfaceOrientation = start;
        }

        [TestCase("debugLog")]
        [TestCase("googleTestAds")]
        [TestCase("googleTestAdUnits")]
        public void TheBuildToolSwitchesAreHiddenOnHDCAdsSetup(string field) =>
            Assert.IsTrue(typeof(HDCAdsSetup).GetField(field, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .IsDefined(typeof(HideInInspector), false));

        [Test]
        public void TheOtherSetupFieldsStayVisible() =>
            Assert.IsFalse(typeof(HDCAdsSetup).GetField("nextScene", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .IsDefined(typeof(HideInInspector), false));
    }
}
