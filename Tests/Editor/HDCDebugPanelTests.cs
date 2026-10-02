using System.Collections;
using System.Collections.Generic;
using HDC.Ads.DebugUI;
using HDC.Ads.Diagnostics;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static HDC.Ads.Tests.HDCTestUi;

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

            // Selection lives in the Actions card; break ad buttons are gone.
            Transform actions = content.Find("Actions Card");
            Assert.IsTrue(actions.Find("Selection Row/Group Button").gameObject.activeSelf);
            Assert.IsTrue(actions.Find("Selection Row/Position Button").gameObject.activeSelf);
            Assert.IsNull(content.Find("Selection Card"));
            Assert.IsFalse(AllText(actions).Contains("BreakAd"), "no break ad in FA");
            Assert.IsFalse(actions.Find("Action Buttons/Update Position Button").gameObject.activeSelf);

            // Only the selected group: the two units of native_gameplay.
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

            // Recent events and system start folded.
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

            // Picking another group shows only that group.
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

            // Popup channel: one popup group with its error.
            Click(window.Find("Pages/Ads Page/Channel Tabs/PU"));
            yield return null;
            units = Rows(detail.Find("Units List"), "Unit ");
            Assert.AreEqual(1, units.Count);
            StringAssert.StartsWith("LOAD FAILED", Badge(units[0]));
            Assert.IsTrue(panelObject.transform.Find("Safe Area/Popup Area").gameObject.activeSelf);

            // Banner channel: only the selected placement.
            Click(window.Find("Pages/Ads Page/Channel Tabs/BN"));
            yield return null;
            Assert.AreEqual("Placement: FullBottom", TextOf(detail.Find("Group Title")));
            Capture(panelObject, "hdc-debug-ads-bn.png");

            // Remote Config page, with configs given to HDCAds.Initialize directly.
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

            // Events page
            Click(window.Find("Page Bar/Events Tab"));
            yield return null;
            Transform eventsContent = window.Find("Pages/Events Page/Body/Viewport/Content");
            StringAssert.Contains("LoadFailed", TextOf(eventsContent.Find("List Card/List")));
            Click(eventsContent.Find("Filter Card/Tool Buttons/Explain Button"));
            yield return null;
            StringAssert.Contains("Load lỗi", TextOf(eventsContent.Find("List Card/List")));
            Capture(panelObject, "hdc-debug-events.png");

            // Device page
            Click(window.Find("Page Bar/Device Tab"));
            yield return null;
            Transform deviceContent = window.Find("Pages/Device Page/Body/Viewport/Content");
            StringAssert.Contains("Unity Version", AllText(deviceContent.Find("Build Card")));
            StringAssert.Contains("Editor simulation", AllText(deviceContent.Find("Library Card")));
            string mediation = AllText(deviceContent.Find("Mediation Card"));
            StringAssert.Contains("META AUDIENCE NETWORK", mediation);
            StringAssert.Contains("Meta Test Mode On", mediation);
            StringAssert.Contains("Adjust", AllText(deviceContent.Find("Adjust Card")));
            Capture(panelObject, "hdc-debug-device.png");

            Click(window.Find("Header/Close Button"));
            Assert.IsFalse(panel.IsOpen);
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

        /// <summary>
        /// Fetches the project's real Remote Config in the Editor and logs what the page shows, to check a
        /// project's setup by eye. Needs the network and the project's Firebase files, so it only runs when picked.
        /// </summary>
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
