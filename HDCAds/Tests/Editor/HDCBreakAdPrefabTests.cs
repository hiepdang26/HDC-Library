using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace HDC.Ads.Tests
{
    public class HDCBreakAdPrefabTests : HDCPlayModeTest
    {
        private const string Core = @"{
  ""forceAdLayoutConfig"": { ""layoutGroups"": [ { ""groupName"": ""cls"", ""layouts"": [ { ""layout"": ""fs_single_cls_01"", ""layoutTime"": 5 } ] } ] },
  ""break_ads_config"": { ""adUnitId"": ""native-ok"", ""layoutGroup"": ""cls"", ""showBreakAdTime"": 3, ""notiBreakAdTime"": 2 } }";

        [Test]
        public void ThePrefabIsWiredAndNeverTakesTouches()
        {
            GameObject prefab = HDCTestPaths.BreakAdPrefab();
            var view = prefab.GetComponent<HDCBreakAdView>();
            Assert.IsNotNull(view);
            var serialized = new SerializedObject(view);
            foreach (string field in new[] { "notice", "message", "countdown", "ring", "seconds", "loading", "spinner" })
                Assert.IsNotNull(serialized.FindProperty(field).objectReferenceValue, field);
            Assert.AreEqual(10f, serialized.FindProperty("loadingTimeout").floatValue);

            Assert.AreEqual(RenderMode.ScreenSpaceOverlay, prefab.GetComponent<Canvas>().renderMode);
            Assert.IsNull(prefab.GetComponent<GraphicRaycaster>(), "the game keeps every touch");
            Assert.IsTrue(prefab.GetComponentsInChildren<Graphic>(true).All(graphic => !graphic.raycastTarget));
            Assert.IsFalse(prefab.transform.Find("Notice").gameObject.activeSelf, "hidden until a break comes");
            Assert.IsTrue(prefab.GetComponentsInChildren<Image>(true).All(image => image.sprite != null));
        }

        [UnityTest]
        public IEnumerator ThePrefabCountsDownShowsTheAdAndStopsWithTheScene()
        {
            yield return new EnterPlayMode();
            bool ready = false;
            HDCAds.Initialize("{}", Core, () => ready = true);
            yield return WaitFor(() => ready, 10f);

            GameObject view = Object.Instantiate(HDCTestPaths.BreakAdPrefab());
            Transform notice = view.transform.Find("Notice");
            Assert.IsTrue(HDCAds.BreakAd.IsRunning, "the prefab starts the break ad");
            Assert.IsFalse(notice.gameObject.activeSelf);

            yield return WaitFor(() => HDCAds.BreakAd.State == HDCBreakAdState.Countdown, 5f);
            yield return null;
            Assert.IsTrue(notice.gameObject.activeSelf);
            Assert.IsTrue(notice.Find("Countdown").gameObject.activeSelf);
            Assert.IsFalse(notice.Find("Loading").gameObject.activeSelf);
            Assert.AreEqual("BREAK AD...", notice.Find("Message").GetComponent<Text>().text);
            StringAssert.IsMatch("^[12]$", notice.Find("Countdown/Seconds").GetComponent<Text>().text, "seconds left of the 2 s countdown");
            Assert.That(notice.Find("Countdown").GetComponent<Image>().fillAmount, Is.InRange(0.01f, 1f));

            yield return WaitFor(() => HDCAds.BreakAd.State == HDCBreakAdState.Showing, 5f);
            Assert.IsFalse(notice.gameObject.activeSelf, "the notice goes away once the ad shows");
            yield return WaitFor(() => HDCAds.BreakAd.State == HDCBreakAdState.Waiting, 5f);

            Object.Destroy(view);
            yield return null;
            Assert.IsFalse(HDCAds.BreakAd.IsRunning, "the last break ad view stops it");
            Assert.AreEqual(HDCBreakAdState.Off, HDCAds.BreakAd.State);
            yield return new ExitPlayMode();
        }
    }
}
