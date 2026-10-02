using System.Collections;
using HDC.Ads.Diagnostics;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace HDC.Ads.Tests
{
    public class HDCPopupLifecycleTests : HDCPlayModeTest
    {
        private const string Position = "popup";

        [UnityTest]
        public IEnumerator HideKeepsThePopupAndAClosedOneLoadsAgain()
        {
            yield return new EnterPlayMode();
            bool ready = false;
            HDCAds.Initialize(HDCTestConfigs.Ads, HDCTestConfigs.Core.Replace("popup-fail-unit", "popup-ok-unit"), () => ready = true);
            yield return WaitFor(() => ready, 10f);
            HDCAds.Popup.Move(Position, new Rect(0f, 0f, 300f, 250f));
            HDCAds.Popup.Initialize(Position);
            yield return WaitFor(() => HDCAds.Popup.CanShow(Position), 5f);

            Assert.IsTrue(HDCAds.Popup.Show(Position));
            yield return WaitFor(() => Record().State == HDCAdState.Showing, 2f);

            HDCAds.Popup.Hide(Position);
            yield return WaitFor(() => Record().State == HDCAdState.Closed, 2f);
            Assert.IsTrue(HDCAds.Popup.CanShow(Position), "a hidden popup keeps its ad");
            Assert.IsTrue(HDCAds.Popup.Show(Position));
            yield return WaitFor(() => Record().State == HDCAdState.Showing, 2f);
            Assert.AreEqual(1, Record().Requests, "showing a hidden popup again loads nothing");

            yield return WaitFor(() => Record().State == HDCAdState.Closed, 5f);
            Assert.IsFalse(HDCAds.Popup.CanShow(Position), "a closed popup has no ad left");
            Assert.IsTrue(HDCAds.Popup.Show(Position), "a closed popup loads a new ad and shows it");
            yield return WaitFor(() => Record().Requests == 2 && Record().State == HDCAdState.Showing, 3f);
            Assert.AreEqual(0, Record().ShowFailures);
            yield return new ExitPlayMode();
        }

        private static HDCAdRecord Record() => HDCAdsTracker.Find(HDCAdFormat.Popup, "pu_popup") ?? new HDCAdRecord(HDCAdFormat.Popup, "pu_popup");
    }
}
