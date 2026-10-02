using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using HDC.Ads.Diagnostics;
using HDC.Ads.Domain;
using HDC.Ads.Logic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace HDC.Ads.Tests
{
    public class HDCAdLayoutsTests : HDCPlayModeTest
    {
        private const string PopupAds = @"{ ""selectedAdCoreName"": ""core"",
  ""forceAdChannel"": { ""isEnabled"": true, ""positionConfigs"": [ { ""positionName"": ""pos"" } ] },
  ""popupChannel"": { ""isEnabled"": true, ""positionConfigs"": [ { ""positionName"": ""old"" }, { ""positionName"": ""typo"" } ] } }";

        private const string PopupCore = @"{
  ""forceAdLayoutConfig"": { ""layoutGroups"": [ { ""groupName"": ""cls"", ""layouts"": [
      { ""layout"": ""fs_single_cls_03_left"" }, { ""layout"": ""fs_single_fancy_01"" } ] } ] },
  ""forceAdGroups"": [ { ""groupName"": ""g"", ""positionNames"": [ ""pos"" ], ""androidUnit"": { ""id"": ""native-unit"", ""layoutGroupName"": ""cls"" } } ],
  ""popupGroups"": [
    { ""groupName"": ""old"", ""positionNames"": [ ""old"" ], ""androidUnit"": { ""id"": ""popup-a"", ""layout"": ""mrec_single_manual_13"" } },
    { ""groupName"": ""typo"", ""positionNames"": [ ""typo"" ], ""androidUnit"": { ""id"": ""popup-b"", ""layout"": ""popup_big"" } } ] }";

        private bool savedDebugLog;

        [SetUp]
        public void SaveDebugLog() => savedDebugLog = HDCAds.Testing.DebugLog;

        [TearDown]
        public void RestoreDebugLog() => HDCAds.Testing.DebugLog = savedDebugLog;

        [Test]
        public void APopupShowsTheLayoutItsConfigNames()
        {
            Assert.AreEqual("popup_single_manual_13", HDCAdLayouts.Popup("popup_single_manual_13"));
            Assert.AreEqual("popup_single_manual_13", HDCAdLayouts.Popup("mrec_single_manual_13"), "the old name of the same layout");
            Assert.AreEqual("popup_single_manual_05", HDCAdLayouts.Popup(" MREC_SINGLE_MANUAL_05 "));
            Assert.AreEqual(HDCAdLayouts.PopupDefault, HDCAdLayouts.Popup(""), "no layout in the config");
            Assert.AreEqual(HDCAdLayouts.PopupDefault, HDCAdLayouts.Popup(null));
            Assert.AreEqual(HDCAdLayouts.PopupDefault, HDCAdLayouts.Popup("mrec_single_manual_16"), "no such layout");
            Assert.IsTrue(HDCAdLayouts.IsPopup("mrec_single_manual_01"));
            Assert.IsFalse(HDCAdLayouts.IsPopup("mrec_single_manual_1"));
            Assert.IsFalse(HDCAdLayouts.IsPopup("popup_single_manual_+1"));
            Assert.IsFalse(HDCAdLayouts.IsPopup(""));
        }

        [Test]
        public void AFullscreenSideVariantShowsItsLayout()
        {
            Assert.AreEqual("fs_single_cls_03", HDCAdLayouts.Fullscreen("fs_single_cls_03_left"));
            Assert.AreEqual("fs_single_nav_02", HDCAdLayouts.Fullscreen("fs_single_nav_02_right"));
            Assert.AreEqual("fs_single_universal_04", HDCAdLayouts.Fullscreen("fs_single_universal_04"));
            Assert.AreEqual("fs_single_prgso_03", HDCAdLayouts.Fullscreen("FS_SINGLE_PRGSO_03"));
            Assert.AreEqual(HDCAdLayouts.FullscreenDefault, HDCAdLayouts.Fullscreen("fs_single_universal_13"));
            Assert.IsFalse(HDCAdLayouts.IsFullscreen("fs_single_cls_04_left"));
        }

        [Test]
        public void ConfigCheckNamesLayoutsThatDoNotExist()
        {
            var ads = new HDCFakeAds();
            HDCAdsConfig adsConfig = HDCAdsConfig.Parse(PopupAds);
            HDCAdCoreConfig core = HDCAdCoreConfig.Parse(PopupCore);
            List<string> findings = ads.Channels.All
                .SelectMany(channel => channel.ConfigRule.Check(adsConfig, core))
                .Select(finding => finding.Text)
                .ToList();

            Assert.IsTrue(findings.Any(text => text.StartsWith("PU:") && text.Contains("'popup_big'")), string.Join("\n", findings));
            Assert.IsTrue(findings.Any(text => text.StartsWith("FA:") && text.Contains("'fs_single_fancy_01'")), string.Join("\n", findings));
            Assert.IsFalse(findings.Any(text => text.Contains("mrec_single_manual_13") || text.Contains("fs_single_cls_03_left")), string.Join("\n", findings));
        }

        [Test]
        public void ThePopupTabShowsTheLayoutInUse()
        {
            var ads = new HDCFakeAds();
            ads.Start(PopupAds, PopupCore);
            IAdChannel popup = ads.Channels.All.First(channel => channel.Key == "PU");
            List<HDCDebugGroup> groups = popup.Diagnostics.UnitGroups("old", "old", false);

            Assert.AreEqual("popup_single_manual_13 · from 'mrec_single_manual_13'", Layout(groups, "old").Value);
            HDCDebugInfo.Row typo = Layout(groups, "typo");
            StringAssert.StartsWith(HDCAdLayouts.PopupDefault + " · default: no layout named 'popup_big'", typo.Value);
            Assert.AreEqual(HDCDebugTone.Bad, typo.Tone);
        }

        [UnityTest]
        public IEnumerator APopupLoadsTheLayoutItsConfigNames()
        {
            yield return new EnterPlayMode();
            HDCAds.Testing.DebugLog = true;
            bool ready = false;
            HDCAds.Initialize(HDCTestConfigs.Ads, HDCTestConfigs.Core.Replace("popup_single_manual_01", "mrec_single_manual_13"), () => ready = true);
            yield return WaitFor(() => ready, 10f);

            LogAssert.Expect(LogType.Log, new Regex(@"call popup\.load .*""layout"":""popup_single_manual_13"""));
            HDCAds.Popup.Initialize("popup");
            yield return null;
            yield return new ExitPlayMode();
        }

        private static HDCDebugInfo.Row Layout(IEnumerable<HDCDebugGroup> groups, string name) =>
            groups.First(group => group.Name == name).Details.Parts.SelectMany(part => part.Rows).First(row => row.Label == "Layout");
    }
}
