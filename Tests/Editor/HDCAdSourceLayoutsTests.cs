using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HDC.Ads.Diagnostics;
using HDC.Ads.Domain;
using HDC.Ads.Infrastructure;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace HDC.Ads.Tests
{
    public class HDCAdSourceLayoutsTests : HDCPlayModeTest
    {
        private const string PickerCore = @"{ ""forceAdLayoutConfig"": { ""layoutGroups"": [
    { ""groupName"": ""g"", ""layouts"": [ { ""layout"": ""fs_single_universal_03"" } ],
      ""adSourceGroups"": [ { ""adSourceIds"": [ ""meta"", ""pangle"" ], ""layouts"": [ { ""layout"": ""fs_single_cls_02"" } ] },
                           { ""adSourceIds"": [ ""bare"" ], ""layouts"": [] } ] },
    { ""groupName"": ""sources_only"", ""layouts"": [],
      ""adSourceGroups"": [ { ""adSourceIds"": [ ""meta"" ], ""layouts"": [ { ""layout"": ""fs_single_nav_01"" } ] } ] } ] } }";

        private const string Ads = @"{ ""selectedAdCoreName"": ""core"",
  ""forceAdChannel"": { ""isEnabled"": true, ""positionConfigs"": [ { ""positionName"": ""ui"", ""canShow"": true, ""cappingTime"": 0 } ] },
  ""popupChannel"": { ""isEnabled"": true, ""positionConfigs"": [ { ""isEnabled"": true, ""positionName"": ""popup"" } ] } }";

        private const string Core = @"{
  ""forceAdLayoutConfig"": { ""layoutGroups"": [ { ""groupName"": ""cls"", ""layouts"": [ { ""layout"": ""fs_single_universal_03"" } ],
      ""adSourceGroups"": [ { ""adSourceIds"": [ ""editor"" ], ""layouts"": [ { ""layout"": ""fs_single_cls_02"" } ] } ] } ] },
  ""forceAdGroups"": [ { ""mediationPriority"": 1, ""groupName"": ""native_ui"", ""positionNames"": [ ""ui"" ],
      ""androidUnit"": { ""id"": ""native-ok"", ""layoutGroupName"": ""cls"" } } ],
  ""popupGroups"": [ { ""groupName"": ""popup"", ""positionNames"": [ ""popup"" ], ""androidUnit"": { ""id"": ""popup-ok"",
      ""layout"": ""popup_single_manual_01"", ""timeShow"": 3,
      ""adSourceLayouts"": [ { ""adSources"": [ ""editor"" ], ""layout"": ""mrec_single_manual_13"" } ] } } ] }";

        [Test]
        public void AFullscreenAdShowsALayoutOfItsAdSource()
        {
            HDCAdCoreConfig core = HDCAdCoreConfig.Parse(PickerCore);
            var picker = new HDCLayoutPicker(core, "g");

            Assert.AreEqual("fs_single_cls_02", Layout(picker.Next("pangle")));
            Assert.AreEqual("fs_single_universal_03", Layout(picker.Next("admob")), "a source without a group shows the group's own layouts");
            Assert.AreEqual("fs_single_universal_03", Layout(picker.Next("")), "an unknown source shows the group's own layouts");
            Assert.AreEqual("fs_single_universal_03", Layout(picker.Next("bare")), "a source group without layouts is skipped");
            Assert.AreEqual("fs_single_nav_01", Layout(new HDCLayoutPicker(core, "sources_only").Next("admob")),
                "without own layouts the group uses its first ad source group");
        }

        [Test]
        public void PopupSourceLayoutsKeepOnlyUsableEntriesWithCurrentNames()
        {
            HDCPopupOptions.SourceLayout[] layouts = HDCNativePopupAd.SourceLayouts(new[]
            {
                new HDCAdCoreConfig.AdSourceLayout { adSources = new[] { " 123 ", "" }, layout = "mrec_single_manual_13" },
                new HDCAdCoreConfig.AdSourceLayout { adSources = new string[0], layout = "popup_single_manual_02" },
                new HDCAdCoreConfig.AdSourceLayout { adSources = new[] { "456" }, layout = "" },
                null,
            });

            Assert.AreEqual(1, layouts.Length);
            CollectionAssert.AreEqual(new[] { "123" }, layouts[0].adSources);
            Assert.AreEqual("popup_single_manual_13", layouts[0].layout);
        }

        [Test]
        public void ConfigCheckNamesLayoutsOfAdSourcesThatDoNotExist()
        {
            const string core = @"{
  ""forceAdLayoutConfig"": { ""layoutGroups"": [ { ""groupName"": ""cls"", ""layouts"": [ { ""layout"": ""fs_single_cls_01"" } ],
      ""adSourceGroups"": [ { ""adSourceIds"": [ ""meta"" ], ""layouts"": [ { ""layout"": ""fs_single_fancy_09"" } ] } ] } ] },
  ""forceAdGroups"": [ { ""groupName"": ""g"", ""positionNames"": [ ""ui"" ], ""androidUnit"": { ""id"": ""n"", ""layoutGroupName"": ""cls"" } } ],
  ""popupGroups"": [ { ""groupName"": ""popup"", ""positionNames"": [ ""popup"" ], ""androidUnit"": { ""id"": ""p"",
      ""adSourceLayouts"": [ { ""adSources"": [ ""meta"" ], ""layout"": ""popup_tall"" } ] } } ] }";
            var ads = new HDCFakeAds();
            List<string> findings = ads.Channels.All
                .SelectMany(channel => channel.ConfigRule.Check(HDCAdsConfig.Parse(Ads), HDCAdCoreConfig.Parse(core)))
                .Select(finding => finding.Text)
                .ToList();

            Assert.IsTrue(findings.Any(text => text.StartsWith("FA:") && text.Contains("'fs_single_fancy_09'")), string.Join("\n", findings));
            Assert.IsTrue(findings.Any(text => text.StartsWith("PU:") && text.Contains("'popup_tall'")), string.Join("\n", findings));
        }

        [UnityTest]
        public IEnumerator EachNativeAdShowsTheLayoutOfItsAdSource()
        {
            yield return new EnterPlayMode();
            bool ready = false;
            HDCAds.Initialize(Ads, Core, () => ready = true);
            yield return WaitFor(() => ready, 10f);

            HDCAds.ForceAd.Initialize("native_ui");
            yield return WaitFor(() => HDCAds.ForceAd.CanShow("ui"), 5f);
            Assert.IsTrue(HDCAds.ForceAd.Show("ui"));
            yield return WaitFor(() => HDCAdsTracker.Find(HDCAdFormat.Fullscreen, "fa_native_ui")?.Layout != null, 3f);
            Assert.AreEqual("fs_single_cls_02", HDCAdsTracker.Find(HDCAdFormat.Fullscreen, "fa_native_ui").Layout);

            HDCAds.Popup.Move("popup", new Rect(0f, 0f, 300f, 300f));
            HDCAds.Popup.Initialize("popup");
            yield return WaitFor(() => HDCAds.Popup.CanShow("popup"), 5f);
            Assert.IsTrue(HDCAds.Popup.Show("popup"));
            yield return WaitFor(() => HDCAdsTracker.Find(HDCAdFormat.Popup, "pu_popup")?.Layout != null, 3f);
            Assert.AreEqual("popup_single_manual_13", HDCAdsTracker.Find(HDCAdFormat.Popup, "pu_popup").Layout,
                "the popup of an ad from the listed source shows that source's layout, under its current name");
            yield return new ExitPlayMode();
        }

        private static string Layout(HDCFullscreenOptions options) => options.layoutNames.Single();
    }
}
