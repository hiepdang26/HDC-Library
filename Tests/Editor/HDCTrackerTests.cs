using System.Collections;
using System.Linq;
using HDC.Ads.DebugUI;
using HDC.Ads.Diagnostics;
using HDC.Ads.Domain;
using HDC.Ads.Infrastructure;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace HDC.Ads.Tests
{
    public class HDCTrackerTests : HDCPlayModeTest
    {
        [UnityTest]
        public IEnumerator TrackerFollowsLoadsFailuresRetriesAndShows()
        {
            yield return new EnterPlayMode();
            bool ready = false;
            HDCAdsSdk.Initialize(() => ready = true);
            yield return WaitFor(() => ready, 10f);

            Assert.IsTrue(HDCAdsSdk.LoadInterstitial("t_fail", new[] { "unit-fail" }));
            Assert.IsTrue(HDCAdsSdk.LoadFullscreen("t_ok", new[] { "unit-ok" }));
            HDCAdRecord failing = HDCAdsTracker.Find(HDCAdFormat.Interstitial, "t_fail");
            Assert.AreEqual(HDCAdState.Loading, failing.State);
            Assert.AreEqual(1, failing.Requests);
            yield return new WaitForSecondsRealtime(1f);

            Assert.AreEqual(HDCAdState.LoadFailed, failing.State);
            Assert.AreEqual(3, failing.LastLoadError.Code);
            Assert.Greater(failing.RetryAt, Time.realtimeSinceStartup, "a retry waits");
            Assert.AreEqual(1, failing.RetryAttempt);
            Assert.Greater(failing.LoadSeconds, 0f);

            HDCAdRecord loaded = HDCAdsTracker.Find(HDCAdFormat.Fullscreen, "t_ok");
            Assert.AreEqual(HDCAdState.Loaded, loaded.State);
            Assert.AreEqual("Editor simulation", loaded.AdSource);
            Assert.IsTrue(HDCAdsSdk.ShowFullscreen("t_ok"));
            yield return null;
            yield return null;
            Assert.AreEqual(HDCAdState.Showing, loaded.State);
            Assert.AreEqual(1, loaded.Impressions);
            Assert.Greater(loaded.Revenue, -1d);

            Assert.IsFalse(HDCAdsSdk.ShowFullscreen("t_ok"), "already showing");
            yield return null;
            yield return null;
            Assert.AreEqual(1, loaded.ShowFailures);
            StringAssert.Contains("-1 · HDC", HDCAdErrorGuide.Title(loaded.LastShowError, true));
            StringAssert.Contains("chưa load xong", HDCAdErrorGuide.Explain(loaded.LastShowError, true));

            HDCAdsSdk.DestroyInterstitial("t_fail");
            Assert.AreEqual(HDCAdState.Destroyed, failing.State);
            Assert.IsTrue(HDCAdsTracker.Events.Any(tracked => tracked.Event.type == HDCAdEventType.LoadFailed && tracked.Event.id == "t_fail"));
            yield return new ExitPlayMode();
        }

        [Test]
        public void GuideExplainsKnownAndUnknownCodes()
        {
            StringAssert.Contains("NO_FILL", HDCAdErrorGuide.Title(new HDCAdError { Code = 3 }, false));
            StringAssert.Contains("NETWORK_ERROR", HDCAdErrorGuide.Title(new HDCAdError { Code = 2 }, false));
            StringAssert.Contains("AD_REUSED", HDCAdErrorGuide.Title(new HDCAdError { Code = 1 }, true));
            StringAssert.Contains("UNKNOWN", HDCAdErrorGuide.Title(new HDCAdError { Code = 77 }, false));
            StringAssert.Contains("Gợi ý", HDCAdErrorGuide.Explain(new HDCAdError { Code = 0 }, false));
            StringAssert.Contains("Popup chưa ở trạng thái", HDCAdErrorGuide.Explain(new HDCAdError { Code = -1, Message = "Popup not displayable: Loading" }, true));
        }
    }
}
