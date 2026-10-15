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
        public IEnumerator HideAndCloseKeepThePopupForTheNextShow()
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
            Assert.IsTrue(HDCAds.Popup.CanShow(Position), "a closed popup keeps its ad");
            Assert.IsTrue(HDCAds.Popup.Show(Position), "a closed popup shows its ad again");
            yield return WaitFor(() => Record().State == HDCAdState.Showing, 3f);
            Assert.AreEqual(1, Record().Requests, "showing a closed popup again loads nothing");
            Assert.AreEqual(0, Record().ShowFailures);
            yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator ReinitializeKeepsAnAdThatHasNotShownAndReplacesOneThatHas()
        {
            yield return new EnterPlayMode();
            bool ready = false;
            HDCAds.Initialize(HDCTestConfigs.Ads, HDCTestConfigs.Core.Replace("popup-fail-unit", "popup-ok-unit"), () => ready = true);
            yield return WaitFor(() => ready, 10f);
            HDCAds.Popup.Move(Position, new Rect(0f, 0f, 300f, 250f));
            HDCAds.Popup.Initialize(Position);
            yield return WaitFor(() => HDCAds.Popup.CanShow(Position), 5f);

            Assert.IsTrue(HDCAds.Popup.Reinitialize(Position));
            Assert.AreEqual(1, Record().Requests, "an ad that has not shown yet is kept");
            Assert.IsTrue(HDCAds.Popup.CanShow(Position));

            Assert.IsTrue(HDCAds.Popup.Show(Position));
            yield return WaitFor(() => Record().State == HDCAdState.Showing, 2f);
            HDCAds.Popup.Hide(Position);
            yield return WaitFor(() => Record().State == HDCAdState.Closed, 2f);
            Assert.IsTrue(HDCAds.Popup.Reinitialize(Position));
            Assert.AreEqual(2, Record().Requests, "an ad that has shown is replaced by a new one");
            yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator ReinitializeKeepsTheLoadInFlight()
        {
            yield return new EnterPlayMode();
            bool ready = false;
            HDCAds.Initialize(HDCTestConfigs.Ads, HDCTestConfigs.Core.Replace("popup-fail-unit", "popup-slow-unit"), () => ready = true);
            yield return WaitFor(() => ready, 10f);
            HDCAds.Popup.Move(Position, new Rect(0f, 0f, 300f, 250f));
            HDCAds.Popup.Initialize(Position);
            yield return null;

            Assert.IsTrue(HDCAds.Popup.Reinitialize(Position));
            Assert.AreEqual(1, Record().Requests, "the request already on its way goes on");
            yield return WaitFor(() => HDCAds.Popup.CanShow(Position), 5f);
            Assert.AreEqual(1, Record().Requests);
            yield return new ExitPlayMode();
        }

        private static HDCAdRecord Record() => HDCAdsTracker.Find(HDCAdFormat.Popup, "pu_popup") ?? new HDCAdRecord(HDCAdFormat.Popup, "pu_popup");
    }
}
