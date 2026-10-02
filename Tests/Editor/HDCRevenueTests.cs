using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace HDC.Ads.Tests
{
    public class HDCRevenueTests : HDCPlayModeTest
    {
        private readonly List<HDCAdRevenue> revenues = new List<HDCAdRevenue>();

        [UnityTest]
        public IEnumerator RevenueNamesTheChannelAndPosition()
        {
            yield return new EnterPlayMode();
            yield return InitializeAds();
            HDCAds.ForceAd.Initialize("native_ui");
            HDCAds.Banner.Initialize(HDCBannerSlot.FullBottom);
            yield return WaitFor(() => HDCAds.ForceAd.CanShow("ui") && HDCAds.Banner.CanShow(), 5f);

            Assert.IsTrue(HDCAds.ForceAd.Show("ui"));
            Assert.IsTrue(HDCAds.Banner.Show());
            yield return WaitFor(() => revenues.Count >= 2, 3f);

            HDCAdRevenue forceAd = revenues.Single(revenue => revenue.Channel == HDCAdChannel.ForceAd);
            Assert.AreEqual("ui", forceAd.Position);
            Assert.AreEqual(HDCAdFormat.Fullscreen, forceAd.Format);
            Assert.AreEqual(HDCAdRevenue.AdMob, forceAd.Network);
            Assert.AreEqual("Editor simulation", forceAd.AdSource);
            Assert.AreEqual("native-ok", forceAd.AdUnitId);
            Assert.AreEqual("USD", forceAd.Currency);

            HDCAdRevenue banner = revenues.Single(revenue => revenue.Channel == HDCAdChannel.Banner);
            Assert.AreEqual("FullBottom", banner.Position);
            Assert.AreEqual(HDCAdFormat.Banner, banner.Format);
            StringAssert.StartsWith("Banner FullBottom banner 0 USD", banner.ToString());
            yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator RevenueReachesEveryHandlerEvenFromAnUnknownAd()
        {
            yield return new EnterPlayMode();
            yield return InitializeAds();
            Action<HDCAdRevenue> failing = _ => throw new InvalidOperationException("a game handler failed");
            HDCAds.Revenue -= Record;
            HDCAds.Revenue += failing;
            HDCAds.Revenue += Record;
            LogAssert.Expect(LogType.Exception, new Regex("a game handler failed"));

            HDCAdsSdk.Emit(new HDCAdEvent
            {
                id = "made_elsewhere", format = HDCAdFormat.Interstitial, type = HDCAdEventType.Paid,
                valueMicros = 1500, currency = "EUR", precision = 3,
            });

            HDCAdRevenue revenue = revenues.Single();
            Assert.AreEqual(HDCAdChannel.Unknown, revenue.Channel);
            Assert.AreEqual("", revenue.Position);
            Assert.AreEqual(0.0015, revenue.Value, 1e-9);
            Assert.AreEqual("EUR", revenue.Currency);
            Assert.AreEqual(3, revenue.Precision);
            HDCAds.Revenue -= failing;
            yield return new ExitPlayMode();
        }

        private IEnumerator InitializeAds()
        {
            revenues.Clear();
            bool ready = false;
            HDCAds.Initialize(HDCTestConfigs.Ads, HDCTestConfigs.Core, () => ready = true);
            HDCAds.Revenue += Record;
            yield return WaitFor(() => ready, 10f);
        }

        private void Record(HDCAdRevenue revenue) => revenues.Add(revenue);
    }
}
