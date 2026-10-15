using HDC.Ads.Infrastructure;
using NUnit.Framework;
using UnityEngine;

namespace HDC.Ads.Tests
{
    public class HDCPopupPlacementTests
    {
        [Test]
        public void AnAreaAcrossTheWholeScreenAsksForTheWholeWidth()
        {
            Assert.AreEqual(HDCNativePopupAd.WiderThanAnyScreen, HDCNativePopupAd.WidthOf(new Rect(0f, 0f, 1080f, 144f), 1080f, 3f), "1080 px at scale 3 is 360 pt, but an iPhone 8 Plus screen is 414 pt wide");
            Assert.AreEqual(HDCNativePopupAd.WiderThanAnyScreen, HDCNativePopupAd.WidthOf(Rect.MinMaxRect(0.4f, 0f, 2555.6f, 220f), 2556f, 3f), "rounding at the edges still counts as the whole width");
            Assert.AreEqual(HDCNativePopupAd.WiderThanAnyScreen, HDCNativePopupAd.WidthOf(Rect.MinMaxRect(-20f, 0f, 1100f, 144f), 1080f, 3f), "an area wider than the screen");
        }

        [Test]
        public void ANarrowerAreaKeepsItsOwnWidthInPoints()
        {
            Assert.AreEqual(300f, HDCNativePopupAd.WidthOf(new Rect(90f, 500f, 900f, 750f), 1080f, 3f), 0.001f);
            Assert.AreEqual(355f, HDCNativePopupAd.WidthOf(new Rect(0f, 0f, 1065f, 144f), 1080f, 3f), 0.001f, "touching only the left edge");
            Assert.AreEqual(355f, HDCNativePopupAd.WidthOf(Rect.MinMaxRect(15f, 0f, 1080f, 144f), 1080f, 3f), 0.001f, "touching only the right edge");
        }
    }
}
