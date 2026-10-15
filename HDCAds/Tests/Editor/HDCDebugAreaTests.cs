using System.Collections;
using System.Linq;
using HDC.Ads.DebugUI;
using HDC.Ads.Diagnostics;
using HDC.Ads.Domain;
using HDC.Ads.Infrastructure;
using HDC.Ads.Logic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using static HDC.Ads.Tests.HDCTestUi;
using Object = UnityEngine.Object;

namespace HDC.Ads.Tests
{
    public class HDCDebugAreaTests : HDCPlayModeTest
    {
        private const float Pixel = 1.01f;

        [Test]
        public void ThePopupAreaKeepsTheSizeTheNativeLayoutsNeed()
        {
            Assert.IsTrue(HDCAdLayouts.PopupUsesMedia("popup_single_manual_01"));
            foreach (string compact in new[] { "popup_single_manual_10", "popup_single_manual_11", "popup_single_manual_13", "popup_single_manual_14", "mrec_single_manual_11" })
                Assert.IsFalse(HDCAdLayouts.PopupUsesMedia(compact), compact);
            Assert.IsTrue(HDCAdLayouts.PopupUsesMedia(null), "no layout shows the default, which has media");
            Assert.IsTrue(HDCAdLayouts.PopupUsesMedia("nonsense"));

            Assert.AreEqual(66f, HDCAdLayouts.PopupMinHeightPoints(new[] { "popup_single_manual_11" }));
            Assert.AreEqual(250f, HDCAdLayouts.PopupMinHeightPoints(new[] { "popup_single_manual_11", "popup_single_manual_02" }), "an ad source layout with media");
            Assert.AreEqual(250f, HDCAdLayouts.PopupMinHeightPoints(new string[0]));
            Assert.AreEqual(300f, HDCAdLayouts.PopupMinWidthPoints);
        }

        [Test]
        public void PresetsSitInsideTheSafeAreaLikeGma()
        {
            var safe = new Rect(30f, 100f, 1000f, 2000f);
            var size = new Vector2(900f, 750f);
            Assert.AreEqual(new Rect(30f, 1350f, 900f, 750f), HDCDebugArea.PresetRect(HDCAdPosition.TopLeft, size, safe));
            Assert.AreEqual(new Rect(80f, 1350f, 900f, 750f), HDCDebugArea.PresetRect(HDCAdPosition.Top, size, safe));
            Assert.AreEqual(new Rect(130f, 1350f, 900f, 750f), HDCDebugArea.PresetRect(HDCAdPosition.TopRight, size, safe));
            Assert.AreEqual(new Rect(80f, 725f, 900f, 750f), HDCDebugArea.PresetRect(HDCAdPosition.Center, size, safe));
            Assert.AreEqual(new Rect(30f, 100f, 900f, 750f), HDCDebugArea.PresetRect(HDCAdPosition.BottomLeft, size, safe));
            Assert.AreEqual(new Rect(80f, 100f, 900f, 750f), HDCDebugArea.PresetRect(HDCAdPosition.Bottom, size, safe));
            Assert.AreEqual(new Rect(130f, 100f, 900f, 750f), HDCDebugArea.PresetRect(HDCAdPosition.BottomRight, size, safe));
        }

        [Test]
        public void AnAreaFindsItsAdByPlacementThenCenterThenPreset()
        {
            var safe = new Rect(0f, 0f, 1000f, 2000f);
            var area = new HDCDebugArea("MREC", new Vector2(300f, 250f), false, null);
            Assert.IsNull(area.Locate(2f, safe));
            area.Preset = HDCAdPosition.TopLeft;
            Assert.AreEqual(new Rect(0f, 1500f, 600f, 500f), area.Locate(2f, safe));
            area.Center = new Vector2(500f, 1000f);
            Assert.AreEqual(new Rect(200f, 750f, 600f, 500f), area.Locate(2f, safe));
            area.Placed = new Rect(1f, 2f, 3f, 4f);
            Assert.AreEqual(new Rect(1f, 2f, 3f, 4f), area.Locate(2f, safe));
            Assert.IsNull(area.Place(Rect.zero), "an area with nothing to place says so");
        }

        [Test]
        public void OnIosTheMrecCountsFromTheSafeAreaLikeGma()
        {
            var screenCenter = new Vector2(1311f, 603f);
            Vector2 mrec = HDCGmaBannerView.MrecPoints;
            var landscape = new Vector2(2622f, 1206f);
            Vector2Int ios = HDCNativeScreen.Scaled(landscape, 3f, new Vector2(186f, 0f)).CustomPosition(screenCenter, mrec);
            Assert.AreEqual(new Vector2Int(225, 76), ios, "GMA adds the 62 pt left inset back, which centres the MREC");
            Vector2Int fromWindow = HDCNativeScreen.Scaled(landscape, 3f, Vector2.zero).CustomPosition(screenCenter, mrec);
            Assert.AreEqual(new Vector2Int(287, 76), fromWindow, "Android counts from the window");
            Assert.AreEqual(new Vector2Int(100, 50), HDCNativeScreen.Scaled(new Vector2(1170f, 2532f), 3f, new Vector2(0f, 141f)).CustomPosition(new Vector2(750f, 1866f), mrec),
                "a portrait top inset is taken off too");
        }

        [Test]
        public void ResizingKeepsTheMinimumAndStaysOnScreen()
        {
            var bounds = new Rect(0f, 0f, 1000f, 1000f);
            var min = new Vector2(300f, 250f);
            var start = new Rect(100f, 100f, 400f, 300f);
            Assert.AreEqual(new Rect(100f, 100f, 300f, 250f), HDCDebugAreaEditor.Resized(start, Vector2.one, new Vector2(-300f, -250f), min, bounds));
            Assert.AreEqual(new Rect(100f, 100f, 900f, 900f), HDCDebugAreaEditor.Resized(start, Vector2.one, new Vector2(2000f, 2000f), min, bounds));
            Assert.AreEqual(new Rect(200f, 150f, 300f, 250f), HDCDebugAreaEditor.Resized(start, -Vector2.one, new Vector2(1000f, 1000f), min, bounds));
            Assert.AreEqual(new Rect(0f, 0f, 500f, 400f), HDCDebugAreaEditor.Resized(start, -Vector2.one, new Vector2(-500f, -500f), min, bounds));
            Assert.AreEqual(new Rect(700f, 100f, 300f, 300f), HDCDebugAreaEditor.Resized(new Rect(800f, 100f, 200f, 300f), Vector2.one, Vector2.zero, min, bounds),
                "too narrow by the right edge: it grows to the minimum and moves left");
            Assert.AreEqual(new Rect(700f, 0f, 300f, 250f), HDCDebugAreaEditor.Clamp(new Rect(900f, -50f, 300f, 250f), bounds));
            Assert.AreEqual(new Rect(0f, 0f, 1000f, 1000f), HDCDebugAreaEditor.Clamp(new Rect(-10f, -10f, 2000f, 3000f), bounds));
        }

        [UnityTest]
        public IEnumerator HoldingThePopupAreaMovesItAndThePopup()
        {
            yield return new EnterPlayMode();
            ForgetSavedAreas();
            GameObject panelObject = null;
            try
            {
                yield return Panel(go => panelObject = go);
                Transform window = panelObject.transform.Find("Safe Area/Window");
                Click(window.Find("Pages/Ads Page/Channel Tabs/PU"));
                yield return null;

                Transform areaObject = panelObject.transform.Find("Safe Area/Ad Area");
                var area = areaObject.GetComponent<HDCDebugAreaEditor>();
                Assert.IsTrue(areaObject.gameObject.activeSelf);
                Assert.IsTrue(areaObject.Find("Handle Top Right").gameObject.activeSelf, "a popup can change size");
                StringAssert.StartsWith("Popup area · ", TextOf(areaObject.Find("Caption/Text")));
                area.HoldSeconds = 0.2f;
                var group = window.GetComponent<CanvasGroup>();

                Rect start = area.ScreenRect();
                var pointer = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left, position = start.center };
                ExecuteEvents.Execute(areaObject.gameObject, pointer, ExecuteEvents.pointerDownHandler);
                pointer.position = start.center + new Vector2(60f, 40f);
                ExecuteEvents.Execute(areaObject.gameObject, pointer, ExecuteEvents.beginDragHandler);
                ExecuteEvents.Execute(areaObject.gameObject, pointer, ExecuteEvents.dragHandler);
                ExecuteEvents.Execute(areaObject.gameObject, pointer, ExecuteEvents.pointerUpHandler);
                if (pointer.pointerDrag != null && pointer.pointerDrag != areaObject.gameObject)
                    ExecuteEvents.Execute(pointer.pointerDrag, pointer, ExecuteEvents.endDragHandler);
                yield return new WaitForSecondsRealtime(0.3f);
                Assert.AreEqual(start, area.ScreenRect(), "a drag without a hold scrolls the panel instead");
                Assert.IsFalse(area.IsEditing);

                pointer = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left, position = start.center };
                ExecuteEvents.Execute(areaObject.gameObject, pointer, ExecuteEvents.pointerDownHandler);
                yield return new WaitForSecondsRealtime(0.3f);
                Assert.IsTrue(area.IsEditing, "held long enough");
                Assert.Less(group.alpha, 0.5f, "the panel fades so the game shows");
                Vector2 step = new Vector2(Mathf.Min(60f, Screen.width - start.xMax), -Mathf.Min(50f, start.yMin));
                pointer.position = start.center + step;
                ExecuteEvents.Execute(areaObject.gameObject, pointer, ExecuteEvents.beginDragHandler);
                ExecuteEvents.Execute(areaObject.gameObject, pointer, ExecuteEvents.dragHandler);
                ExecuteEvents.Execute(areaObject.gameObject, pointer, ExecuteEvents.pointerUpHandler);
                ExecuteEvents.Execute(areaObject.gameObject, pointer, ExecuteEvents.endDragHandler);
                Assert.IsFalse(area.IsEditing);
                Assert.AreEqual(1f, group.alpha);

                Rect moved = area.ScreenRect();
                Assert.AreEqual(start.x + step.x, moved.x, Pixel);
                Assert.AreEqual(start.y + step.y, moved.y, Pixel);
                Assert.AreEqual(start.size.x, moved.size.x, Pixel);
                yield return new WaitForSecondsRealtime(0.4f);
                StringAssert.Contains("Popup.Move(\"popup\", popup area)", TextOf(window.Find("Pages/Ads Page/Body/Viewport/Content/Actions Card/Last Call")));
                Rect? placed = Channel("PU").Diagnostics.Area("popup", "popup").Placed;
                Assert.IsTrue(placed.HasValue, "the popup took the new place");
                Assert.AreEqual(moved.center.x, placed.Value.center.x, Pixel);
                Assert.IsTrue(SavedKeys().Any(PlayerPrefs.HasKey), "kept for the next launch");

                pointer = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left, position = moved.center };
                ExecuteEvents.Execute(areaObject.gameObject, pointer, ExecuteEvents.pointerDownHandler);
                yield return new WaitForSecondsRealtime(0.3f);
                pointer.position = moved.center + new Vector2(5000f, 5000f);
                ExecuteEvents.Execute(areaObject.gameObject, pointer, ExecuteEvents.dragHandler);
                ExecuteEvents.Execute(areaObject.gameObject, pointer, ExecuteEvents.pointerUpHandler);
                Rect pushed = area.ScreenRect();
                Assert.AreEqual(Screen.width, pushed.xMax, Pixel, "stays on the screen");
                Assert.AreEqual(Screen.height, pushed.yMax, Pixel);

                Click(window.Find("Header/Close Button"));
                Assert.IsFalse(areaObject.gameObject.activeSelf, "closing the panel hides the area");
                Object.Destroy(panelObject);
                panelObject = null;
                yield return null;
                yield return Panel(go => panelObject = go);
                Click(panelObject.transform.Find("Safe Area/Window/Pages/Ads Page/Channel Tabs/PU"));
                yield return null;
                Rect reopened = panelObject.transform.Find("Safe Area/Ad Area").GetComponent<HDCDebugAreaEditor>().ScreenRect();
                Assert.AreEqual(pushed.center.x, reopened.center.x, Pixel, "the area is where it was left");
                Assert.AreEqual(pushed.center.y, reopened.center.y, Pixel);

                Transform reopenedArea = panelObject.transform.Find("Safe Area/Ad Area");
                reopenedArea.GetComponent<HDCDebugAreaEditor>().HoldSeconds = 0.2f;
                pointer = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left, position = reopened.center };
                ExecuteEvents.Execute(reopenedArea.gameObject, pointer, ExecuteEvents.pointerDownHandler);
                yield return new WaitForSecondsRealtime(0.3f);
                Capture(panelObject, "hdc-debug-area-moving.png");
                ExecuteEvents.Execute(reopenedArea.gameObject, pointer, ExecuteEvents.pointerUpHandler);
            }
            finally
            {
                if (panelObject != null)
                    Object.Destroy(panelObject);
                ForgetSavedAreas();
            }

            yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator ACornerResizesThePopupAreaDownToTheLayoutMinimum()
        {
            yield return new EnterPlayMode();
            ForgetSavedAreas();
            GameObject panelObject = null;
            try
            {
                yield return Panel(go => panelObject = go);
                Click(panelObject.transform.Find("Safe Area/Window/Pages/Ads Page/Channel Tabs/PU"));
                yield return null;
                Transform areaObject = panelObject.transform.Find("Safe Area/Ad Area");
                var area = areaObject.GetComponent<HDCDebugAreaEditor>();
                Rect start = area.ScreenRect();
                GameObject handle = areaObject.Find("Handle Top Right").gameObject;

                var pointer = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left, position = new Vector2(start.xMax, start.yMax) };
                ExecuteEvents.Execute(handle, pointer, ExecuteEvents.pointerDownHandler);
                Assert.IsTrue(area.IsEditing, "a handle needs no hold");
                pointer.position = new Vector2(start.xMin - 5000f, start.yMin - 5000f);
                ExecuteEvents.Execute(handle, pointer, ExecuteEvents.dragHandler);
                ExecuteEvents.Execute(handle, pointer, ExecuteEvents.pointerUpHandler);

                Rect smallest = area.ScreenRect();
                float scale = HDCAdsSdk.PixelsPerPoint;
                Assert.AreEqual(Mathf.Min(300f * scale, Screen.width), smallest.width, Pixel, "popup_single_manual_01 is at least 300 pt wide");
                Assert.AreEqual(Mathf.Min(250f * scale, Screen.height), smallest.height, Pixel, "and 250 pt high");
                Assert.AreEqual(start.xMin, smallest.xMin, Pixel, "the opposite corner stays");
                Assert.AreEqual(start.yMin, smallest.yMin, Pixel);
                yield return new WaitForSecondsRealtime(0.4f);
                StringAssert.Contains("Popup.Move(\"popup\", popup area)", AllText(panelObject.transform.Find("Safe Area/Window/Pages/Ads Page/Body/Viewport/Content/Actions Card")));
                Capture(panelObject, "hdc-debug-area-popup.png");
            }
            finally
            {
                if (panelObject != null)
                    Object.Destroy(panelObject);
                ForgetSavedAreas();
            }

            yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator TheMrecAreaShowsWhereTheMrecIsAndMovesIt()
        {
            yield return new EnterPlayMode();
            ForgetSavedAreas();
            GameObject panelObject = null;
            try
            {
                yield return Panel(go => panelObject = go);
                Transform window = panelObject.transform.Find("Safe Area/Window");
                Click(window.Find("Pages/Ads Page/Channel Tabs/MREC"));
                yield return null;
                Transform areaObject = panelObject.transform.Find("Safe Area/Ad Area");
                var area = areaObject.GetComponent<HDCDebugAreaEditor>();
                area.HoldSeconds = 0.2f;
                Assert.IsTrue(areaObject.gameObject.activeSelf);
                Assert.IsFalse(areaObject.Find("Handle Top Right").gameObject.activeSelf, "the MREC has one size");
                float scale = HDCAdsSdk.PixelsPerPoint;
                Rect safe = Screen.safeArea;

                Rect bottomRight = area.ScreenRect();
                Assert.AreEqual(Mathf.Min(300f * scale, Screen.width), bottomRight.width, Pixel);
                Assert.AreEqual(safe.xMax, bottomRight.xMax, Pixel, "the MREC starts bottom right");
                Assert.AreEqual(safe.yMin, bottomRight.yMin, Pixel);

                HDCAds.Mrec.Move(HDCAdPosition.TopLeft);
                Click(window.Find("Header/Refresh Button"));
                yield return null;
                Rect topLeft = area.ScreenRect();
                Assert.AreEqual(safe.xMin, topLeft.xMin, Pixel, "the area follows the MREC's position");
                Assert.AreEqual(Mathf.Min(safe.yMax, Screen.height), topLeft.yMax, Pixel);

                var pointer = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left, position = topLeft.center };
                ExecuteEvents.Execute(areaObject.gameObject, pointer, ExecuteEvents.pointerDownHandler);
                yield return new WaitForSecondsRealtime(0.3f);
                Vector2 step = new Vector2(Mathf.Min(40f, Screen.width - topLeft.xMax), -Mathf.Min(30f, topLeft.yMin));
                pointer.position = topLeft.center + step;
                ExecuteEvents.Execute(areaObject.gameObject, pointer, ExecuteEvents.dragHandler);
                ExecuteEvents.Execute(areaObject.gameObject, pointer, ExecuteEvents.pointerUpHandler);

                Rect moved = area.ScreenRect();
                Assert.AreEqual(topLeft.center.x + step.x, moved.center.x, Pixel);
                yield return null;
                StringAssert.Contains("Mrec.Move(new Vector2(", TextOf(window.Find("Pages/Ads Page/Body/Viewport/Content/Actions Card/Last Call")));
                Vector2? center = Channel("MREC").Diagnostics.Area("", "").Center;
                Assert.IsTrue(center.HasValue, "the MREC moved to the area");
                Assert.AreEqual(moved.center.x, center.Value.x, Pixel);
                Assert.AreEqual(moved.center.y, center.Value.y, Pixel);
                Capture(panelObject, "hdc-debug-area-mrec.png");
            }
            finally
            {
                if (panelObject != null)
                    Object.Destroy(panelObject);
                ForgetSavedAreas();
            }

            yield return new ExitPlayMode();
        }

        private static IEnumerator Panel(System.Action<GameObject> made)
        {
            if (!HDCAds.IsInitialized)
            {
                bool ready = false;
                HDCAds.Initialize(HDCTestConfigs.Ads, HDCTestConfigs.Core, () => ready = true);
                yield return WaitFor(() => ready, 10f);
            }

            GameObject panelObject = Object.Instantiate(HDCTestPaths.DebugPanelPrefab());
            made(panelObject);
            yield return null;
            panelObject.GetComponent<HDCAdsDebugPanel>().Open();
            yield return null;
            yield return null;
        }

        private static IAdChannel Channel(string key) => HDCAds.Channels.All.First(each => each.Key == key);

        private static string[] SavedKeys() =>
            new[] { "PU", "MREC" }.SelectMany(key => new[] { ".Portrait", ".Landscape" }.Select(side => HDCDebugAreaEditor.SavedKeyPrefix + key + side)).ToArray();

        private static void ForgetSavedAreas()
        {
            foreach (string key in SavedKeys())
                PlayerPrefs.DeleteKey(key);
        }
    }
}
