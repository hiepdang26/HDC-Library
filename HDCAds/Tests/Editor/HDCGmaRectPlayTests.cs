using System.Collections;
using System.Collections.Generic;
using HDC.Ads.Domain;
using HDC.Ads.Infrastructure;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace HDC.Ads.Tests
{
    public class HDCGmaRectPlayTests : HDCPlayModeTest
    {
        private const string Ads = @"{ ""bannerChannel"": { ""isEnabled"": true, ""fullBottom"": { ""isEnabled"": false } },
  ""mrecChannel"": { ""isEnabled"": true, ""autoInit"": false } }";

        private const string Core = @"{
  ""bannerUnit"": { ""fullBottom"": { ""mediationPriority"": 0, ""admobUnit"": { ""id"": ""ca-app-pub-3940256099942544/6300978111"" } } },
  ""mrecUnit"": { ""mediationPriority"": 0, ""admobUnit"": { ""id"": ""ca-app-pub-3940256099942544/6300978111"" } } }";

        [UnityTest]
        public IEnumerator TheBannerAndTheMrecShowOnceTheirAdLoads()
        {
            yield return new EnterPlayMode();
            bool ready = false;
            HDCAds.Initialize(Ads, Core, () => ready = true);
            yield return WaitFor(() => ready, 10f);
            var events = new List<string>();
            HDCAdsSdk.AdEvent += adEvent => events.Add(adEvent.format + " " + adEvent.type);

            Assert.IsFalse(HDCAds.Banner.Show(), "the banner has no ad yet");
            yield return WaitFor(() => events.Contains(HDCAdFormat.BannerView + " " + HDCAdEventType.Shown), 10f);
            Assert.Less(events.IndexOf(HDCAdFormat.BannerView + " " + HDCAdEventType.Loaded), events.IndexOf(HDCAdFormat.BannerView + " " + HDCAdEventType.Shown),
                "shown only after it loaded");
            Assert.IsTrue(HDCAds.Banner.CanShow());
            Assert.IsTrue(HDCAds.Banner.Show());
            HDCAds.Banner.Hide();
            Assert.Contains(HDCAdFormat.BannerView + " " + HDCAdEventType.Closed, events);

            HDCAds.Mrec.Move(HDCAdPosition.Top);
            Assert.IsFalse(HDCAds.Mrec.Show(), "the MREC has no ad yet");
            yield return WaitFor(() => events.Contains(HDCAdFormat.Mrec + " " + HDCAdEventType.Shown), 10f);
            Assert.IsTrue(HDCAds.Mrec.CanShow);
            Assert.IsTrue(HDCAds.Mrec.Show());
            HDCAds.Mrec.Hide();
            Assert.Contains(HDCAdFormat.Mrec + " " + HDCAdEventType.Closed, events);
            yield return new ExitPlayMode();
        }
    }
}
