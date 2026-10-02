using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace HDC.Ads.Tests
{
    public class HDCTestAdUnitsTests : HDCPlayModeTest
    {
        [TearDown]
        public void UseConfiguredUnits() => HDCAdsSdk.UseTestAdUnits = false;

        [UnityTest]
        public IEnumerator TestAdUnitsReplaceEveryConfiguredUnit()
        {
            yield return new EnterPlayMode();
            HDCAdsSdk.UseTestAdUnits = true;
            bool ready = false;
            HDCAds.Initialize(HDCTestConfigs.Ads, HDCTestConfigs.Core, () => ready = true);
            yield return WaitFor(() => ready, 10f);
            HDCAds.ForceAd.Initialize("native_gameplay");
            HDCAds.ForceAd.Initialize("native_ui");
            HDCAds.Popup.Initialize("popup");
            HDCAds.Banner.Initialize(HDCBannerSlot.FullBottom);
            HDCAds.Mrec.Initialize();
            yield return new WaitForSecondsRealtime(3f);

            List<HDCAdRecord> records = HDCAdsTracker.Records.Where(record => !string.IsNullOrEmpty(record.AdUnitId)).ToList();
            Assert.GreaterOrEqual(records.Count, 3);
            foreach (HDCAdRecord record in records)
                StringAssert.StartsWith("ca-app-pub-3940256099942544/", record.AdUnitId, record.Format + "/" + record.Id);
            Assert.AreEqual(HDCAdState.Loaded, HDCAdsTracker.Find(HDCAdFormat.Fullscreen, "fa_native_gameplay").State, "the failing unit is replaced");
            Assert.AreEqual(HDCAdState.Loaded, HDCAdsTracker.Find(HDCAdFormat.Popup, "pu_popup").State);
            yield return new ExitPlayMode();
        }
    }
}
