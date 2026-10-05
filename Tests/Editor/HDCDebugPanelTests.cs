using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HDC.Ads.Composition;
using HDC.Ads.DebugUI;
using HDC.Ads.Diagnostics;
using HDC.Ads.Domain;
using HDC.Ads.Logic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static HDC.Ads.Tests.HDCTestUi;
using Object = UnityEngine.Object;

namespace HDC.Ads.Tests
{
    public class HDCDebugPanelTests : HDCPlayModeTest
    {
        [Test]
        public void PrefabStartsClosedWithEveryPage()
        {
            GameObject prefab = HDCTestPaths.DebugPanelPrefab();
            Assert.IsNotNull(prefab.GetComponent<HDCAdsDebugPanel>());
            Transform window = prefab.transform.Find("Safe Area/Window");
            Assert.IsFalse(window.gameObject.activeSelf, "hidden until opened");
            foreach (string page in new[] { "Ads Page", "Remote Config Page", "Events Page", "Device Page" })
                Assert.IsNotNull(window.Find("Pages/" + page), page);
            Assert.IsNotNull(window.Find("Pages/Ads Page/Body/Viewport/Content/Detail Card"));
        }

        [UnityTest]
        public IEnumerator AdsPageShowsOnlyTheSelectedGroup()
        {
            yield return new EnterPlayMode();
            bool ready = false;
            HDCAds.Initialize(HDCTestConfigs.Ads, HDCTestConfigs.Core, () => ready = true);
            yield return WaitFor(() => ready, 10f);
            HDCAds.ForceAd.Initialize("native_gameplay");
            HDCAds.ForceAd.Initialize("native_ui");
            HDCAds.Popup.Initialize("popup");
            HDCAds.Banner.Initialize(HDCBannerSlot.FullBottom);

            GameObject panelObject = Object.Instantiate(HDCTestPaths.DebugPanelPrefab());
            var panel = panelObject.GetComponent<HDCAdsDebugPanel>();
            yield return null;
            Assert.IsFalse(panel.IsOpen, "hidden until opened");
            panel.Open();
            yield return new WaitForSecondsRealtime(3f);

            Transform window = panelObject.transform.Find("Safe Area/Window");
            Transform page = window.Find("Pages/Ads Page");
            Transform content = page.Find("Body/Viewport/Content");

            CollectionAssert.AreEqual(new[] { "AL", "AR", "RW", "FA", "BN", "MREC", "PU" }, Rows(page.Find("Channel Tabs"), "").Select(tab => tab.name).ToArray());
            Transform actions = content.Find("Actions Card");
            Assert.IsTrue(actions.Find("Selection Row/Group Button").gameObject.activeSelf);
            Assert.IsTrue(actions.Find("Selection Row/Position Button").gameObject.activeSelf);
            Assert.IsNull(content.Find("Selection Card"));
            Assert.IsFalse(AllText(actions).Contains("BreakAd"), "no break ad in FA");
            CollectionAssert.AreEqual(new[] { "Init", "Show" }, Rows(actions.Find("Action Buttons"), "").Select(ButtonText).ToArray());

            Transform detail = content.Find("Detail Card");
            StringAssert.Contains("Detail Information Ad", AllText(detail.Find("Header Row")));
            Assert.AreEqual("Group: native_gameplay", TextOf(detail.Find("Group Title")));
            List<Transform> units = Rows(detail.Find("Units List"), "Unit ");
            Assert.AreEqual(2, units.Count, "only native_gameplay's units");
            StringAssert.StartsWith("LOAD FAILED", Badge(units[0]));
            string firstUnit = AllText(units[0]);
            StringAssert.Contains("LOAD ERROR · 3 · NO_FILL", firstUnit);
            StringAssert.Contains("không có quảng cáo", firstUnit);
            StringAssert.Contains("Gợi ý", firstUnit);
            StringAssert.Contains("Load Failed", firstUnit);
            StringAssert.Contains("AdMob Interstitial", TextOf(units[1].Find("Top/Title")));
            Assert.AreNotEqual("BACKUP · NOT STARTED", Badge(units[1]), "the backup unit starts after the first one failed");

            Transform events = content.Find("Events Card");
            Transform system = content.Find("System Card");
            Assert.IsFalse(events.Find("Events Body").gameObject.activeSelf);
            Assert.IsFalse(system.Find("System Body").gameObject.activeSelf);
            Click(events.Find("Header Row/Expand Button"));
            Click(system.Find("Header Row/Expand Button"));
            yield return null;
            List<Transform> eventRows = Rows(events.Find("Events Body/Events List"), "Event ");
            Assert.IsTrue(eventRows.Exists(row => TextOf(row).Contains("LoadFailed")));
            string systemText = AllText(system.Find("System Body"));
            StringAssert.Contains("CONFIGS", systemText);
            StringAssert.Contains("Enabled", systemText);
            StringAssert.Contains("POSITION GAMEPLAY", systemText);
            StringAssert.Contains("GROUP NATIVE_GAMEPLAY", systemText);
            Assert.IsFalse(systemText.Contains("Banner"), "only the selected channel");
            yield return null;
            Capture(panelObject, "hdc-debug-ads-fa.png");

            Click(events.Find("Header Row/Full Screen Button"));
            yield return null;
            Transform viewer = panelObject.transform.Find("Safe Area/Detail Viewer/Modal");
            Assert.IsTrue(viewer.gameObject.activeSelf);
            var viewerBody = viewer.Find("Card/Body Scroll/Viewport/Content/Body").GetComponent<UnityEngine.UI.Text>();
            StringAssert.Contains("LoadFailed", viewerBody.text);
            Assert.AreEqual(32, viewerBody.fontSize, "large text full screen");
            Capture(panelObject, "hdc-debug-events-full.png");
            Click(viewer.Find("Card/Header/Close Button"));

            Click(actions.Find("Selection Row/Group Button"));
            yield return null;
            Click(Option(panelObject.transform.Find("Safe Area/Option Picker/Modal/Sheet/Options Scroll/Viewport/Content"), "native_ui"));
            yield return null;
            Assert.AreEqual("Group: native_ui", TextOf(detail.Find("Group Title")));
            units = Rows(detail.Find("Units List"), "Unit ");
            Assert.AreEqual(1, units.Count);
            Assert.AreEqual("READY", Badge(units[0]));

            Click(detail.Find("Header Row/Copy Report Button"));
            StringAssert.Contains("native_ui", GUIUtility.systemCopyBuffer);

            Click(window.Find("Pages/Ads Page/Channel Tabs/PU"));
            yield return null;
            units = Rows(detail.Find("Units List"), "Unit ");
            Assert.AreEqual(1, units.Count);
            StringAssert.StartsWith("LOAD FAILED", Badge(units[0]));
            Assert.AreEqual("Native Popup", TextOf(units[0].Find("Top/Title")).Substring(4));
            Assert.IsTrue(panelObject.transform.Find("Safe Area/Popup Area").gameObject.activeSelf);
            CollectionAssert.AreEqual(new[] { "Init", "Show", "Hide", "UpdatePos" }, Rows(actions.Find("Action Buttons"), "").Select(ButtonText).ToArray());
            Click(actions.Find("Action Buttons/Update Position Button"));
            StringAssert.Contains("Popup.Move(\"popup\", popup area)", TextOf(actions.Find("Last Call")));

            Click(window.Find("Pages/Ads Page/Channel Tabs/BN"));
            yield return null;
            Assert.AreEqual("Placement: FullBottom", TextOf(detail.Find("Group Title")));
            Assert.AreEqual("Activate", ButtonText(actions.Find("Action Buttons/Show Button")));
            Capture(panelObject, "hdc-debug-ads-bn.png");

            Click(window.Find("Page Bar/Remote Config Tab"));
            yield return null;
            Assert.IsFalse(page.gameObject.activeSelf);
            Assert.IsFalse(panelObject.transform.Find("Safe Area/Popup Area").gameObject.activeSelf, "the popup area belongs to the ads page");
            Transform configContent = window.Find("Pages/Remote Config Page/Body/Viewport/Content");
            StringAssert.Contains("Given to HDCAds.Initialize directly", AllText(configContent.Find("Status Card")));
            StringAssert.Contains("position 'orphan'", AllText(configContent.Find("Check Card")));
            Click(configContent.Find("Viewer Card/Mode Buttons/Applied Button"));
            yield return null;
            StringAssert.Contains("APPLIED = DIRECT", TextOf(configContent.Find("Viewer Card/Applied Note/Text")));
            Assert.AreEqual("Applied: Direct", ButtonText(configContent.Find("Viewer Card/Mode Buttons/Applied Button")));
            StringAssert.Contains("Applied: ads_config", AllText(configContent.Find("Status Card")));
            string body = TextOf(configContent.Find("Viewer Card/Body Box/Body"));
            StringAssert.Contains("selectedAdCoreName", body);
            StringAssert.Contains("{...}", body, "top-level keys start folded");
            Click(configContent.Find("Viewer Card/Part Buttons/Ad Core Button"));
            yield return null;
            Click(Option(configContent.Find("Viewer Card/Options"), "Expand All"));
            yield return null;
            StringAssert.Contains("native_gameplay", TextOf(configContent.Find("Viewer Card/Body Box/Body")));
            Click(configContent.Find("Map Card/Header Row/Expand Button"));
            yield return null;
            StringAssert.Contains("FORCE AD · NATIVE_UI", AllText(configContent.Find("Map Card")));
            Capture(panelObject, "hdc-debug-config.png");

            Click(window.Find("Page Bar/Events Tab"));
            yield return null;
            Transform eventsContent = window.Find("Pages/Events Page/Body/Viewport/Content");
            StringAssert.Contains("LoadFailed", TextOf(eventsContent.Find("List Card/List")));
            Click(eventsContent.Find("Filter Card/Tool Buttons/Explain Button"));
            yield return null;
            StringAssert.Contains("Load lỗi", TextOf(eventsContent.Find("List Card/List")));
            Capture(panelObject, "hdc-debug-events.png");

            Click(window.Find("Page Bar/Device Tab"));
            yield return null;
            Transform deviceContent = window.Find("Pages/Device Page/Body/Viewport/Content");
            StringAssert.Contains("Unity Version", AllText(deviceContent.Find("Build Card")));
            StringAssert.Contains("Editor simulation", AllText(deviceContent.Find("Library Card")));
            string mediation = AllText(deviceContent.Find("Mediation Card"));
            StringAssert.Contains("META AUDIENCE NETWORK", mediation);
            StringAssert.Contains("Meta Test Mode On", mediation);
            StringAssert.Contains("Adjust", AllText(deviceContent.Find("Adjust Card")));
            Transform testAdUnits = deviceContent.Find("Library Card/Library Buttons/Test Ad Units Button");
            Assert.AreEqual("Test Ad Units: Off", ButtonText(testAdUnits));
            Transform restart = deviceContent.Find("Library Card/Library Buttons/Restart App Button");
            Assert.IsFalse(restart.gameObject.activeSelf, "only once the choice changed");
            Click(testAdUnits);
            yield return null;
            Assert.IsTrue(HDCAds.Testing.UseTestAdUnits);
            Assert.IsTrue(HDCTestAdUnitsSwitch.IsSaved, "kept for the next launches");
            Assert.AreEqual("Test Ad Units: On", ButtonText(testAdUnits));
            StringAssert.Contains("Bấm Restart App", AllText(deviceContent.Find("Library Card")));
            Assert.IsTrue(restart.gameObject.activeSelf);
            Assert.AreEqual("Restart App", ButtonText(restart));
            Capture(panelObject, "hdc-debug-device.png");
            Click(testAdUnits);
            Assert.IsFalse(HDCAds.Testing.UseTestAdUnits);
            Assert.IsFalse(HDCTestAdUnitsSwitch.IsSaved);

            Transform clearData = deviceContent.Find("Library Card/Library Buttons/Clear Data Button");
            Assert.IsTrue(clearData.gameObject.activeSelf, "always offered");
            Assert.AreEqual("Clear Data & Restart", ButtonText(clearData));
            Click(clearData);
            yield return null;
            Assert.IsTrue(Application.isPlaying, "the first tap only asks for a second one");
            Assert.AreEqual("Tap Again To Clear", ButtonText(clearData));
            StringAssert.Contains("Bấm lần nữa", AllText(deviceContent.Find("Library Card")));
            yield return new WaitForSecondsRealtime(HDCDevicePage.ClearDataConfirmSeconds + 0.3f);
            Assert.AreEqual("Clear Data & Restart", ButtonText(clearData), "the request lapses");

            Click(window.Find("Header/Close Button"));
            Assert.IsFalse(panel.IsOpen);
            yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator AChannelFromATestShowsOnThePanel()
        {
            yield return new EnterPlayMode();
            var ads = new HDCFakeAds(extraChannels: new Func<HDCAdsContext, IAdChannel>[] { context => new HDCFakeChannel(context) });
            HDCAdsRuntime.Use(ads.Runtime);
            try
            {
                ads.Start(HDCTestConfigs.Ads, HDCTestConfigs.Core);
                var fake = (HDCFakeChannel)HDCAds.Channels.All.Last();
                Assert.AreEqual(1, fake.Starts, "it starts with the others once the SDK is ready");

                GameObject panelObject = Object.Instantiate(HDCTestPaths.DebugPanelPrefab());
                panelObject.GetComponent<HDCAdsDebugPanel>().Open();
                yield return null;
                Transform window = panelObject.transform.Find("Safe Area/Window");
                Transform page = window.Find("Pages/Ads Page");
                Transform tab = page.Find("Channel Tabs/FAKE");
                Assert.AreEqual("Fake", TextOf(tab.Find("Name")));
                Click(tab);
                yield return null;

                Transform content = page.Find("Body/Viewport/Content");
                Transform actions = content.Find("Actions Card");
                Assert.AreEqual("Fake · FAKE", TextOf(actions.Find("Channel Title")));
                Assert.AreEqual("Position: fake_spot", ButtonText(actions.Find("Selection Row/Position Button")));
                CollectionAssert.AreEqual(new[] { "Ping" }, Rows(actions.Find("Action Buttons"), "").Select(ButtonText).ToArray());
                Click(actions.Find("Action Buttons/Ping Button"));
                Assert.AreEqual(1, fake.Pings);
                StringAssert.Contains("Fake.Ping(\"fake_spot\")", TextOf(actions.Find("Last Call")));

                Transform detail = content.Find("Detail Card");
                Assert.AreEqual("Group: fake_group", TextOf(detail.Find("Group Title")));
                List<Transform> units = Rows(detail.Find("Units List"), "Unit ");
                Assert.AreEqual(1, units.Count);
                Assert.AreEqual("#1  Fake Banner", TextOf(units[0].Find("Top/Title")));
                Assert.AreEqual("READY", Badge(units[0]));

                Transform system = content.Find("System Card");
                Click(system.Find("Header Row/Expand Button"));
                yield return null;
                string systemText = AllText(system.Find("System Body"));
                StringAssert.Contains("FAKE STATE", systemText);
                StringAssert.Contains("Fake.Ping(position)", systemText);
                Capture(panelObject, "hdc-debug-ads-fake.png");

                Assert.AreEqual("FAKE", HDCEventText.Channel(new HDCAdEvent { id = HDCFakeChannel.InstanceId, format = HDCAdFormat.Banner }));
                CollectionAssert.AreEqual(new[] { "AL", "AR", "RW", "FA", "BN", "MREC", "PU", "FAKE", "SDK" }, HDCEventText.Channels().ToArray());

                Click(window.Find("Page Bar/Remote Config Tab"));
                yield return null;
                Transform configContent = window.Find("Pages/Remote Config Page/Body/Viewport/Content");
                string check = AllText(configContent.Find("Check Card"));
                StringAssert.Contains("FAKE: kênh giả luôn có một cảnh báo.", check);
                StringAssert.Contains($"Position '{HDCFakeChannel.SharedPosition}' có ở cả FA và FAKE", check);
                Click(configContent.Find("Map Card/Header Row/Expand Button"));
                yield return null;
                StringAssert.Contains("FAKE CHANNEL", AllText(configContent.Find("Map Card")));

                ads.Context.SetAdsRemoved(true);
                Assert.AreEqual(1, fake.Removals);
                Object.Destroy(panelObject);
            }
            finally
            {
                HDCAdsRuntime.Use(HDCAdsRuntime.CreateDefault());
            }

            yield return new ExitPlayMode();
        }

#if HDC_FIREBASE
        [UnityTest]
        public IEnumerator RemoteConfigPageReportsTheEditorDefaults()
        {
            yield return new EnterPlayMode();
            bool ready = false;
            HDCRemoteConfig.FetchAndInitialize(HDCTestConfigs.Ads, new Dictionary<string, string> { { "core", "{}" } }, () => ready = true);
            yield return WaitFor(() => ready, 10f);
            Assert.IsTrue(HDCConfigReport.DefaultsOnly);
            Assert.AreEqual(HDCConfigSource.Default, HDCConfigReport.Find(HDCRemoteConfig.AdsConfigKey).Source);
            Assert.AreEqual("core", HDCConfigReport.CoreKey);

            GameObject panelObject = Object.Instantiate(HDCTestPaths.DebugPanelPrefab());
            var panel = panelObject.GetComponent<HDCAdsDebugPanel>();
            panel.Open();
            panel.ShowPage(1);
            yield return null;
            Transform content = panelObject.transform.Find("Safe Area/Window/Pages/Remote Config Page/Body/Viewport/Content");
            StringAssert.Contains("Project defaults (Editor)", AllText(content.Find("Status Card")));
            string note = TextOf(content.Find("Viewer Card/Applied Note/Text"));
            StringAssert.Contains("APPLIED = DEFAULT", note);
            StringAssert.Contains("Trong Editor HDC dùng config mặc định cho ads_config", note);
            Assert.AreEqual("Default · used", ButtonText(content.Find("Viewer Card/Mode Buttons/Default Button")));
            Assert.AreEqual("Applied: Default", ButtonText(content.Find("Viewer Card/Mode Buttons/Applied Button")));
            Click(content.Find("Viewer Card/Part Buttons/Ad Core Button"));
            yield return null;
            StringAssert.Contains("APPLIED = DEFAULT", TextOf(content.Find("Viewer Card/Applied Note/Text")));
            Click(content.Find("Viewer Card/Mode Buttons/Applied Button"));
            yield return null;
            StringAssert.Contains("(= Default)", TextOf(content.Find("Viewer Card/Info")));
            StringAssert.Contains("Ad core config 'core' rỗng", AllText(content.Find("Check Card")));
            yield return new ExitPlayMode();
        }

        [UnityTest, Explicit("Needs the network and the project's Firebase files"), Category("Network")]
        public IEnumerator RemoteConfigPageShowsTheFetchedValues()
        {
            yield return new EnterPlayMode();
            HDCRemoteConfig.FetchInEditor = true;
            try
            {
                HDCAdsSettings defaults = HDCAdsSettings.Load();
                HDCRemoteConfig.FetchAndInitialize(defaults.AdsConfig, defaults.CoreConfigsByKey(), null, 20f);
                float end = Time.realtimeSinceStartup + 25f;
                while (!HDCConfigReport.Finished && Time.realtimeSinceStartup < end)
                    yield return null;
                Debug.Log($"[HDCTest] fetch: outcome={HDCConfigReport.Outcome} firebase={HDCConfigReport.Firebase} fetch={HDCConfigReport.FetchStatus} keys={HDCConfigReport.RemoteValueCount} core={HDCConfigReport.CoreKey}");
                foreach (HDCConfigEntry entry in HDCConfigReport.Entries)
                    Debug.Log($"[HDCTest] entry {entry.Key}: source={entry.Source} origin={entry.RemoteOrigin} remote={entry.Remote.Length} saved={entry.Saved.Length} default={entry.Default.Length}");
                Assert.IsTrue(HDCConfigReport.Finished);

                GameObject panelObject = Object.Instantiate(HDCTestPaths.DebugPanelPrefab());
                var panel = panelObject.GetComponent<HDCAdsDebugPanel>();
                panel.Open();
                panel.ShowPage(1);
                yield return new WaitForSecondsRealtime(1.5f);
                Transform content = panelObject.transform.Find("Safe Area/Window/Pages/Remote Config Page/Body/Viewport/Content");
                Debug.Log("[HDCTest] status: " + AllText(content.Find("Status Card")).Replace("\n", " | "));
                Click(content.Find("Viewer Card/Mode Buttons/Applied Button"));
                yield return null;
                Debug.Log("[HDCTest] applied: " + TextOf(content.Find("Viewer Card/Applied Note/Text"))
                    + " | buttons: " + AllText(content.Find("Viewer Card/Mode Buttons")).Replace("\n", ", "));
                Debug.Log("[HDCTest] check: " + AllText(content.Find("Check Card")).Replace("\n", " | "));
                Capture(panelObject, "hdc-debug-config-fetched.png");
                Click(content.Find("Viewer Card/Part Buttons/All Keys Button"));
                yield return null;
                Debug.Log("[HDCTest] keys: " + AllText(content.Find("Viewer Card/Options")).Replace("\n", " | "));
                Capture(panelObject, "hdc-debug-config-keys.png");
            }
            finally
            {
                HDCRemoteConfig.FetchInEditor = false;
            }

            yield return new ExitPlayMode();
        }
#endif
    }
}
