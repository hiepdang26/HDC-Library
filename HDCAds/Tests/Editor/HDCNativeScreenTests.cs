using HDC.Ads.Domain;
using HDC.Ads.Infrastructure;
using NUnit.Framework;
using UnityEngine;

namespace HDC.Ads.Tests
{
    public class HDCNativeScreenTests
    {
        private static readonly Vector2 Mrec = new Vector2(300f, 250f);

        [Test]
        public void OnAnIphonePlusTheMrecCentresWhereUnityDrawsTheCentre()
        {
            HDCNativeScreen screen = Ios(1080f, 1920f, "414.000|736.000|0.000|0.000");
            Assert.AreEqual(new Vector2Int(57, 243), screen.CustomPosition(new Vector2(540f, 960f), Mrec), "the screen centre is (207, 368) pt");
            Vector2 size = screen.ToPixels(Mrec);
            Assert.AreEqual(782.6f, size.x, 0.1f, "Unity draws 2.61 px per pt there, not the 3 of UIScreen.scale");
            Assert.AreEqual(652.2f, size.y, 0.1f);

            HDCNativeScreen byDeviceScale = HDCNativeScreen.Scaled(new Vector2(1080f, 1920f), 3f, Vector2.zero);
            Assert.AreEqual(new Vector2Int(30, 195), byDeviceScale.CustomPosition(new Vector2(540f, 960f), Mrec),
                "the device scale alone put it 27 pt left and 48 pt up");
        }

        [Test]
        public void AnIphoneMiniAndDisplayZoomAreMeasuredFromTheView()
        {
            HDCNativeScreen mini = Ios(1080f, 2340f, "375.000|812.000|0.000|50.000");
            Assert.AreEqual(new Vector2Int(38, 231), mini.CustomPosition(new Vector2(540f, 1170f), Mrec), "counted from the safe area like GMA");

            HDCNativeScreen zoomed = Ios(1179f, 2556f, "375.000|812.000|0.000|0.000");
            Assert.AreEqual(new Vector2Int(38, 281), zoomed.CustomPosition(new Vector2(589.5f, 1278f), Mrec));
            Assert.AreEqual(new Vector2Int(46, 301), HDCNativeScreen.Scaled(new Vector2(1179f, 2556f), 3f, Vector2.zero).CustomPosition(new Vector2(589.5f, 1278f), Mrec),
                "with Display Zoom the device scale put it right and down instead");
        }

        [Test]
        public void ALandscapeNotchMovesTheOriginLikeGma()
        {
            HDCNativeScreen screen = Ios(2556f, 1179f, "852.000|393.000|59.000|0.000");
            Assert.AreEqual(new Vector2Int(217, 72), screen.CustomPosition(new Vector2(1278f, 589.5f), Mrec), "GMA adds the 59 pt left inset back");
        }

        [Test]
        public void AndroidCountsDpFromTheWindowAtAnyUnityResolution()
        {
            HDCNativeScreen full = HDCNativeScreens.FromAndroid(new Vector2(1080f, 2400f), new Vector2(1080f, 2400f), 2.625f).Value;
            Assert.AreEqual(new Vector2Int(56, 332), full.CustomPosition(new Vector2(540f, 1200f), Mrec));

            HDCNativeScreen fixedDpi = HDCNativeScreens.FromAndroid(new Vector2(720f, 1600f), new Vector2(1080f, 2400f), 2.625f).Value;
            Assert.AreEqual(new Vector2Int(56, 332), fixedDpi.CustomPosition(new Vector2(360f, 800f), Mrec), "a lower Unity resolution lands on the same dp");
            Vector2 size = fixedDpi.ToPixels(Mrec);
            Assert.AreEqual(525f, size.x, 0.01f, "and the MREC covers fewer Unity pixels");
            Assert.AreEqual(437.5f, size.y, 0.01f);
        }

        [Test]
        public void TheEditorFollowsTheCanvasOfTheGmaPlaceholder()
        {
            HDCNativeScreen screen = HDCNativeScreens.EditorPlaceholder(new Vector2(1080f, 1920f));
            Assert.AreEqual(1920f / HDCNativeScreens.GmaEditorCanvasHeight, screen.PixelsPerPoint.y, 0.0001f, "the placeholder canvas is 1080 units high");
            Assert.AreEqual(new Vector2Int(154, 415), screen.CustomPosition(new Vector2(540f, 960f), Mrec));
        }

        [Test]
        public void BadMeasurementsAreRefused()
        {
            Assert.IsNull(HDCNativeScreens.FromIos(new Vector2(1080f, 1920f), ""));
            Assert.IsNull(HDCNativeScreens.FromIos(new Vector2(1080f, 1920f), "a|b|c|d"));
            Assert.IsNull(HDCNativeScreens.FromIos(new Vector2(1080f, 1920f), "0|0|0|0"));
            Assert.IsNull(HDCNativeScreens.FromAndroid(new Vector2(1080f, 1920f), new Vector2(1080f, 1920f), 0f));
            Assert.IsNull(HDCNativeScreens.FromAndroid(new Vector2(1080f, 1920f), Vector2.zero, 2f), "a content view not laid out yet");
            Assert.AreEqual(Vector2.one, default(HDCNativeScreen).PixelsPerPoint);
        }

        private static HDCNativeScreen Ios(float width, float height, string viewMetrics)
        {
            HDCNativeScreen? screen = HDCNativeScreens.FromIos(new Vector2(width, height), viewMetrics);
            Assert.IsTrue(screen.HasValue, viewMetrics);
            return screen.Value;
        }
    }
}
