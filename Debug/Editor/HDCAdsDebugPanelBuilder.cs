using System.IO;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace HDC.Ads.DebugUI.Editor
{
    /// <summary>
    /// Builds the HDCAdsDebugPanel prefab that ships with HDCLib, and adds it to scenes (HDC > Debug panel).
    /// After changing the layout here, rebuild the prefab with
    /// Unity -batchmode -executeMethod HDC.Ads.DebugUI.Editor.HDCAdsDebugPanelBuilder.Build
    /// </summary>
    public static class HDCAdsDebugPanelBuilder
    {
        private const string PrefabName = "HDCAdsDebugPanel.prefab";
        private const int UiLayer = 5;
        private const int MinLabelSize = 14;
        private const float ButtonHeight = 84f;
        private const float ChipHeight = 72f;
        private const float LabelColumn = 360f;

        private static readonly Color WindowColor = new Color(0.043f, 0.067f, 0.125f, 0.98f);
        private static readonly Color HeaderColor = new Color(0.067f, 0.102f, 0.18f, 1f);
        private static readonly Color CardColor = new Color(0.118f, 0.161f, 0.231f, 1f);
        private static readonly Color UnitColor = new Color(0.059f, 0.09f, 0.165f, 1f);
        private static readonly Color ButtonColor = new Color(0.2f, 0.255f, 0.333f, 1f);
        private static readonly Color PrimaryColor = new Color(0.31f, 0.275f, 0.898f, 1f);
        private static readonly Color DangerColor = new Color(0.725f, 0.11f, 0.11f, 1f);
        private static readonly Color TextColor = new Color(0.945f, 0.961f, 0.976f, 1f);
        private static readonly Color SoftTextColor = new Color(0.796f, 0.835f, 0.882f, 1f);
        private static readonly Color MutedTextColor = new Color(0.58f, 0.639f, 0.722f, 1f);
        private static readonly Color AccentColor = new Color(0.647f, 0.706f, 0.988f, 1f);
        private static readonly Color DimColor = new Color(0f, 0f, 0f, 0.65f);
        private static readonly Color PopupAreaColor = new Color(0.31f, 0.275f, 0.898f, 0.18f);
        private static readonly Color ViewportColor = new Color(1f, 1f, 1f, 0.01f);

        private static readonly string[,] Channels =
        {
            { "AL", "AppLaunch" }, { "AR", "AppResume" }, { "RW", "Rewarded" }, { "FA", "ForceAd" },
            { "BN", "Banner" }, { "MREC", "Mrec" }, { "CL", "Collapsible" }, { "PU", "Popup" },
        };

        private static readonly string[] Pages = { "Ads", "Remote Config", "Events", "Device" };

        private static Font font;
        private static Sprite rounded;
        private static Sprite circle;

        internal static string PrefabPath
        {
            get
            {
                string asmdef = CompilationPipeline.GetAssemblyDefinitionFilePathFromAssemblyName("HDC.Ads.Debug");
                return (Path.GetDirectoryName(asmdef) ?? "Assets/HDCLib/Debug").Replace('\\', '/') + "/" + PrefabName;
            }
        }

        [MenuItem("HDC/Debug panel/Add to open scene", false, 60)]
        private static void AddToOpenScene()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                Debug.LogError($"[HDCAds] {PrefabPath} is missing.");
                return;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Undo.RegisterCreatedObjectUndo(instance, "Add HDC ads debug panel");
            Selection.activeGameObject = instance;
            EditorSceneManager.MarkSceneDirty(instance.scene);
        }

        public static void Build()
        {
            font = Resources.GetBuiltinResource<Font>(
#if UNITY_2022_2_OR_NEWER
                "LegacyRuntime.ttf"
#else
                "Arial.ttf"
#endif
            );
            rounded = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            circle = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

            var root = new GameObject("HDCAdsDebugPanel", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster))
            {
                layer = UiLayer,
            };
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32000;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0f;
            var panel = root.AddComponent<HDCAdsDebugPanel>();

            GameObject safeArea = Element(root.transform, "Safe Area");
            Stretch(safeArea.transform, Vector2.zero, Vector2.zero);

            // Window: header, page bar, then the pages.
            GameObject window = Box(safeArea.transform, "Window", WindowColor, true, true);
            Stretch(window.transform, new Vector2(16f, 16f), new Vector2(-16f, -16f));

            GameObject header = Box(window.transform, "Header", HeaderColor, true, false);
            Top(header.transform, 0f, 150f);
            Horizontal(header, new RectOffset(28, 20, 14, 14), 14f, TextAnchor.MiddleLeft);
            GameObject titles = Element(header.transform, "Titles");
            Vertical(titles, new RectOffset(0, 0, 0, 0), 4f);
            Flexible(titles, 1f);
            Label(titles.transform, "Title", "HDC Ads Debug", 40, FontStyle.Bold, TextColor, TextAnchor.MiddleLeft);
            Text status = Label(titles.transform, "Status", "", 22, FontStyle.Normal, SoftTextColor, TextAnchor.UpperLeft);
            Button initSdk = Bar(header.transform, "Init SDK Button", "Init SDK", PrimaryColor, 190f);
            Button refresh = Bar(header.transform, "Refresh Button", "Refresh", ButtonColor, 170f);
            Button close = Bar(header.transform, "Close Button", "Close", DangerColor, 150f);

            GameObject pageBar = Element(window.transform, "Page Bar");
            Top(pageBar.transform, -162f, 92f);
            HorizontalStretch(pageBar, new RectOffset(24, 24, 0, 0), 12f);
            var pageButtons = new Button[Pages.Length];
            for (int i = 0; i < Pages.Length; i++)
            {
                pageButtons[i] = ButtonWithLabel(pageBar.transform, Pages[i] + " Tab", Pages[i], ButtonColor, 28);
                Flexible(pageButtons[i].gameObject, 1f);
            }

            GameObject pagesRoot = Element(window.transform, "Pages");
            Stretch(pagesRoot.transform, Vector2.zero, new Vector2(0f, -270f));

            // Popup area, picker and viewer sit above the window.
            GameObject popupArea = Box(safeArea.transform, "Popup Area", PopupAreaColor, true, false);
            Place(popupArea.transform, new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(900f, 560f));
            Text popupLabel = Label(popupArea.transform, "Label", "Popup area", 26, FontStyle.Italic, SoftTextColor, TextAnchor.MiddleCenter);
            Stretch(popupLabel.transform, Vector2.zero, Vector2.zero);
            popupArea.SetActive(false);
            HDCOptionPicker picker = Picker(safeArea.transform);
            HDCAdsDebugViewer viewer = Viewer(safeArea.transform);

            HDCAdsDebugWorkspace ads = AdsPage(pagesRoot.transform, picker, viewer, popupArea);
            HDCRemoteConfigPage config = RemoteConfigPage(pagesRoot.transform, viewer);
            HDCEventsPage events = EventsPage(pagesRoot.transform, picker, viewer);
            HDCDevicePage device = DevicePage(pagesRoot.transform);
            config.gameObject.SetActive(false);
            events.gameObject.SetActive(false);
            device.gameObject.SetActive(false);
            window.SetActive(false);

            Assign(panel, "window", window);
            Assign(panel, "safeArea", safeArea.GetComponent<RectTransform>());
            Assign(panel, "scaler", scaler);
            Assign(panel, "closeButton", close);
            Assign(panel, "picker", picker);
            Assign(panel, "viewer", viewer);
            Assign(panel, "statusText", status);
            Assign(panel, "initSdkButton", initSdk);
            Assign(panel, "refreshButton", refresh);
            AssignPages(panel, pageButtons, new HDCDebugPage[] { ads, config, events, device });

            string path = PrefabPath;
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            Debug.Log($"[HDCAds] Built {path}.");
        }

        // Ads page: channel tabs, then Actions, Detail Information, Recent Events and System.
        private static HDCAdsDebugWorkspace AdsPage(Transform parent, HDCOptionPicker picker, HDCAdsDebugViewer viewer, GameObject popupArea)
        {
            GameObject page = Page(parent, "Ads Page");
            var workspace = page.AddComponent<HDCAdsDebugWorkspace>();

            GameObject tabsRoot = Element(page.transform, "Channel Tabs");
            Top(tabsRoot.transform, 0f, 212f);
            var grid = tabsRoot.AddComponent<GridLayoutGroup>();
            grid.padding = new RectOffset(24, 24, 0, 0);
            grid.cellSize = new Vector2(243f, 100f);
            grid.spacing = new Vector2(12f, 12f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;
            grid.childAlignment = TextAnchor.UpperCenter;
            var tabButtons = new Button[Channels.GetLength(0)];
            var tabDots = new Image[Channels.GetLength(0)];
            for (int i = 0; i < Channels.GetLength(0); i++)
                tabButtons[i] = Tab(tabsRoot.transform, Channels[i, 0], Channels[i, 1], out tabDots[i]);

            RectTransform content = Body(page.transform, -224f);

            // Actions: the channel, the group and position to act on, and the API calls.
            GameObject actions = Card(content, "Actions Card");
            Text channelTitle = Label(actions.transform, "Channel Title", "ForceAd · FA", 34, FontStyle.Bold, TextColor, TextAnchor.MiddleLeft);
            Text hint = Label(actions.transform, "Hint", "", 22, FontStyle.Italic, MutedTextColor, TextAnchor.UpperLeft);
            GameObject selection = Element(actions.transform, "Selection Row");
            HorizontalStretch(selection, new RectOffset(0, 0, 0, 0), 12f);
            Button groupButton = ButtonWithLabel(selection.transform, "Group Button", "Group: -", ButtonColor, 26);
            Flexible(groupButton.gameObject, 1f);
            Height(groupButton.gameObject, ButtonHeight);
            Button positionButton = ButtonWithLabel(selection.transform, "Position Button", "Position: -", ButtonColor, 26);
            Flexible(positionButton.gameObject, 1f);
            Height(positionButton.gameObject, ButtonHeight);
            GameObject actionGrid = Grid(actions.transform, "Action Buttons", 3, 306f, ButtonHeight);
            Button initButton = ButtonWithLabel(actionGrid.transform, "Init Button", "Init", PrimaryColor, 26);
            Button showButton = ButtonWithLabel(actionGrid.transform, "Show Button", "Show", PrimaryColor, 26);
            Button hideButton = ButtonWithLabel(actionGrid.transform, "Hide Button", "Hide", ButtonColor, 26);
            Button utilityPrimary = ButtonWithLabel(actionGrid.transform, "Update Position Button", "UpdatePos", ButtonColor, 26);
            Button utilitySecondary = ButtonWithLabel(actionGrid.transform, "Get Size Button", "GetSize", ButtonColor, 26);
            Text lastCall = Label(actions.transform, "Last Call", "Last call: -", 22, FontStyle.Normal, MutedTextColor, TextAnchor.UpperLeft);

            // Detail information: the selected group and its ad units.
            GameObject detail = Card(content, "Detail Card");
            Button[] detailButtons = HeaderRow(detail.transform, "Detail Information Ad", out _, ("Copy Report", 230f));
            Text groupTitle = Label(detail.transform, "Group Title", "", 30, FontStyle.Bold, AccentColor, TextAnchor.MiddleLeft);
            HDCKeyValueList groupList = KeyValueList(detail.transform, "Group Info");
            Text unitsNotice = Label(detail.transform, "Notice", "", 22, FontStyle.Italic, MutedTextColor, TextAnchor.UpperLeft);
            GameObject unitsList = Element(detail.transform, "Units List");
            Vertical(unitsList, new RectOffset(0, 0, 0, 0), 14f);
            GameObject unitTemplate = UnitTemplate(unitsList.transform);

            // Recent events, folded until opened.
            GameObject eventsCard = Card(content, "Events Card");
            Button[] eventButtons = HeaderRow(eventsCard.transform, "Recent Events", out _, ("Expand", 190f), ("Full Screen", 230f));
            GameObject eventsBody = Element(eventsCard.transform, "Events Body");
            Vertical(eventsBody, new RectOffset(0, 0, 0, 0), 10f);
            Text eventsNotice = Label(eventsBody.transform, "Notice", "", 22, FontStyle.Italic, MutedTextColor, TextAnchor.UpperLeft);
            GameObject eventsList = Element(eventsBody.transform, "Events List");
            Vertical(eventsList, new RectOffset(0, 0, 0, 0), 12f);
            Text eventTemplate = Label(eventsList.transform, "Event Template", "", 23, FontStyle.Normal, SoftTextColor, TextAnchor.UpperLeft);
            eventTemplate.gameObject.SetActive(false);
            eventsBody.SetActive(false);

            // System: the selected channel only, folded until opened.
            GameObject systemCard = Card(content, "System Card");
            Button[] systemButtons = HeaderRow(systemCard.transform, "System", out Text systemTitle, ("Expand", 190f));
            GameObject systemBody = Element(systemCard.transform, "System Body");
            Vertical(systemBody, new RectOffset(0, 0, 0, 0), 10f);
            HDCKeyValueList systemList = KeyValueList(systemBody.transform, "System List");
            systemBody.SetActive(false);

            AssignTabs(workspace, tabButtons, tabDots);
            Assign(workspace, "channelTitleText", channelTitle);
            Assign(workspace, "selectionHintText", hint);
            Assign(workspace, "groupButton", groupButton);
            Assign(workspace, "positionButton", positionButton);
            Assign(workspace, "initButton", initButton);
            Assign(workspace, "showButton", showButton);
            Assign(workspace, "hideButton", hideButton);
            Assign(workspace, "utilityPrimaryButton", utilityPrimary);
            Assign(workspace, "utilitySecondaryButton", utilitySecondary);
            Assign(workspace, "lastCallText", lastCall);
            Assign(workspace, "groupTitleText", groupTitle);
            Assign(workspace, "groupList", groupList);
            Assign(workspace, "unitsNoticeText", unitsNotice);
            Assign(workspace, "unitTemplate", unitTemplate);
            Assign(workspace, "copyReportButton", detailButtons[0]);
            Assign(workspace, "eventsToggleButton", eventButtons[0]);
            Assign(workspace, "eventsFullScreenButton", eventButtons[1]);
            Assign(workspace, "eventsBody", eventsBody);
            Assign(workspace, "eventsNoticeText", eventsNotice);
            Assign(workspace, "eventTemplate", eventTemplate.gameObject);
            Assign(workspace, "systemTitleText", systemTitle);
            Assign(workspace, "systemToggleButton", systemButtons[0]);
            Assign(workspace, "systemBody", systemBody);
            Assign(workspace, "systemList", systemList);
            Assign(workspace, "optionPicker", picker);
            Assign(workspace, "viewer", viewer);
            Assign(workspace, "popupArea", popupArea.GetComponent<RectTransform>());
            return workspace;
        }

        // Remote Config page: load status, config check, the config viewer and the ad units map.
        private static HDCRemoteConfigPage RemoteConfigPage(Transform parent, HDCAdsDebugViewer viewer)
        {
            GameObject page = Page(parent, "Remote Config Page");
            var config = page.AddComponent<HDCRemoteConfigPage>();
            RectTransform content = Body(page.transform, 0f);

            GameObject statusCard = Card(content, "Status Card");
            HeaderRow(statusCard.transform, "Remote Config", out _);
            HDCKeyValueList statusList = KeyValueList(statusCard.transform, "Status List");

            GameObject checkCard = Card(content, "Check Card");
            HeaderRow(checkCard.transform, "Config Check", out _);
            HDCKeyValueList checkList = KeyValueList(checkCard.transform, "Check List");

            GameObject viewerCard = Card(content, "Viewer Card");
            Button[] viewerButtons = HeaderRow(viewerCard.transform, "Config Viewer", out _, ("Copy", 160f), ("Full Screen", 230f));
            GameObject modes = Grid(viewerCard.transform, "Mode Buttons", 4, 228f, ChipHeight);
            Button remote = ButtonWithLabel(modes.transform, "Remote Button", "Remote", ButtonColor, 24);
            Button saved = ButtonWithLabel(modes.transform, "Saved Button", "Saved", ButtonColor, 24);
            Button defaults = ButtonWithLabel(modes.transform, "Default Button", "Default", ButtonColor, 24);
            Button applied = ButtonWithLabel(modes.transform, "Applied Button", "Applied", ButtonColor, 24);
            GameObject parts = Grid(viewerCard.transform, "Part Buttons", 3, 306f, ChipHeight);
            Button adsButton = ButtonWithLabel(parts.transform, "Ads Config Button", "ads_config", ButtonColor, 24);
            Button coreButton = ButtonWithLabel(parts.transform, "Ad Core Button", "Ad Core", ButtonColor, 24);
            Button keysButton = ButtonWithLabel(parts.transform, "All Keys Button", "All Keys", ButtonColor, 24);
            GameObject optionGrid = Grid(viewerCard.transform, "Options", 2, 462f, 64f);
            Button optionTemplate = ButtonWithLabel(optionGrid.transform, "Option Template", "Option", ButtonColor, 22);
            Text info = Label(viewerCard.transform, "Info", "", 22, FontStyle.Italic, MutedTextColor, TextAnchor.UpperLeft);
            GameObject bodyBox = Box(viewerCard.transform, "Body Box", UnitColor, true, false);
            Vertical(bodyBox, new RectOffset(20, 20, 16, 16), 0f);
            Text body = Label(bodyBox.transform, "Body", "", 22, FontStyle.Normal, TextColor, TextAnchor.UpperLeft);

            GameObject mapCard = Card(content, "Map Card");
            Button[] mapButtons = HeaderRow(mapCard.transform, "Ad Units Map", out _, ("Expand", 190f));
            GameObject mapBody = Element(mapCard.transform, "Map Body");
            Vertical(mapBody, new RectOffset(0, 0, 0, 0), 10f);
            HDCKeyValueList mapList = KeyValueList(mapBody.transform, "Map List");
            mapBody.SetActive(false);

            Assign(config, "statusList", statusList);
            Assign(config, "checkList", checkList);
            Assign(config, "remoteButton", remote);
            Assign(config, "savedButton", saved);
            Assign(config, "defaultButton", defaults);
            Assign(config, "appliedButton", applied);
            Assign(config, "adsButton", adsButton);
            Assign(config, "coreButton", coreButton);
            Assign(config, "keysButton", keysButton);
            Assign(config, "optionTemplate", optionTemplate);
            Assign(config, "viewerInfoText", info);
            Assign(config, "bodyText", body);
            Assign(config, "copyButton", viewerButtons[0]);
            Assign(config, "fullScreenButton", viewerButtons[1]);
            Assign(config, "mapToggleButton", mapButtons[0]);
            Assign(config, "mapBody", mapBody);
            Assign(config, "mapList", mapList);
            Assign(config, "viewer", viewer);
            return config;
        }

        // Events page: counts, filters and the list of every channel's ad events.
        private static HDCEventsPage EventsPage(Transform parent, HDCOptionPicker picker, HDCAdsDebugViewer viewer)
        {
            GameObject page = Page(parent, "Events Page");
            var events = page.AddComponent<HDCEventsPage>();
            RectTransform content = Body(page.transform, 0f);

            GameObject summaryCard = Card(content, "Summary Card");
            HeaderRow(summaryCard.transform, "Ad Events", out _);
            HDCKeyValueList summaryList = KeyValueList(summaryCard.transform, "Summary List");

            GameObject filterCard = Card(content, "Filter Card");
            HeaderRow(filterCard.transform, "Filters", out _);
            GameObject modes = Grid(filterCard.transform, "Mode Buttons", 2, 462f, ChipHeight);
            Button sequential = ButtonWithLabel(modes.transform, "Sequential Button", "Sequential", ButtonColor, 24);
            Button count = ButtonWithLabel(modes.transform, "Count Button", "Count", ButtonColor, 24);
            GameObject filters = Grid(filterCard.transform, "Filter Buttons", 2, 462f, ChipHeight);
            Button channel = ButtonWithLabel(filters.transform, "Channel Button", "Channel: All", ButtonColor, 24);
            Button type = ButtonWithLabel(filters.transform, "Event Button", "Event: All", ButtonColor, 24);
            GameObject tools = Grid(filterCard.transform, "Tool Buttons", 4, 228f, ChipHeight);
            Button explain = ButtonWithLabel(tools.transform, "Explain Button", "Explain: Off", ButtonColor, 22);
            Button copy = ButtonWithLabel(tools.transform, "Copy Button", "Copy", ButtonColor, 22);
            Button clear = ButtonWithLabel(tools.transform, "Clear Button", "Clear", DangerColor, 22);
            Button fullScreen = ButtonWithLabel(tools.transform, "Full Screen Button", "Full Screen", PrimaryColor, 22);

            GameObject listCard = Card(content, "List Card");
            HeaderRow(listCard.transform, "Event List", out _);
            Text notice = Label(listCard.transform, "Notice", "", 22, FontStyle.Italic, MutedTextColor, TextAnchor.UpperLeft);
            Text list = Label(listCard.transform, "List", "", 23, FontStyle.Normal, SoftTextColor, TextAnchor.UpperLeft);

            Assign(events, "summaryList", summaryList);
            Assign(events, "sequentialButton", sequential);
            Assign(events, "countButton", count);
            Assign(events, "channelButton", channel);
            Assign(events, "typeButton", type);
            Assign(events, "explainButton", explain);
            Assign(events, "copyButton", copy);
            Assign(events, "clearButton", clear);
            Assign(events, "fullScreenButton", fullScreen);
            Assign(events, "noticeText", notice);
            Assign(events, "listText", list);
            Assign(events, "optionPicker", picker);
            Assign(events, "viewer", viewer);
            return events;
        }

        // Device page: build, the ads library's switches, device, network and Adjust.
        private static HDCDevicePage DevicePage(Transform parent)
        {
            GameObject page = Page(parent, "Device Page");
            var device = page.AddComponent<HDCDevicePage>();
            RectTransform content = Body(page.transform, 0f);

            GameObject buildCard = Card(content, "Build Card");
            HeaderRow(buildCard.transform, "Build", out _);
            HDCKeyValueList buildList = KeyValueList(buildCard.transform, "Build List");

            GameObject libraryCard = Card(content, "Library Card");
            HeaderRow(libraryCard.transform, "HDC Ads", out _);
            HDCKeyValueList libraryList = KeyValueList(libraryCard.transform, "Library List");
            GameObject libraryButtons = Grid(libraryCard.transform, "Library Buttons", 2, 462f, ChipHeight);
            Button debugLog = ButtonWithLabel(libraryButtons.transform, "Debug Log Button", "Debug Log: Off", ButtonColor, 24);
            Button testDevice = ButtonWithLabel(libraryButtons.transform, "Test Device Button", "Use Google Test Ads", ButtonColor, 24);
            Button metaOn = ButtonWithLabel(libraryButtons.transform, "Meta Test On Button", "Meta Test Mode On", ButtonColor, 24);
            Button metaOff = ButtonWithLabel(libraryButtons.transform, "Meta Test Off Button", "Meta Test Mode Off", ButtonColor, 24);

            GameObject deviceCard = Card(content, "Device Card");
            HeaderRow(deviceCard.transform, "Device", out _);
            HDCKeyValueList deviceList = KeyValueList(deviceCard.transform, "Device List");

            GameObject networkCard = Card(content, "Network Card");
            Button[] networkButtons = HeaderRow(networkCard.transform, "Network", out _, ("Check Public IP", 290f));
            HDCKeyValueList networkList = KeyValueList(networkCard.transform, "Network List");

            GameObject adjustCard = Card(content, "Adjust Card");
            Button[] adjustButtons = HeaderRow(adjustCard.transform, "Adjust", out _, ("Refresh", 190f));
            HDCKeyValueList adjustList = KeyValueList(adjustCard.transform, "Adjust List");

            Assign(device, "buildList", buildList);
            Assign(device, "libraryList", libraryList);
            Assign(device, "debugLogButton", debugLog);
            Assign(device, "testDeviceButton", testDevice);
            Assign(device, "metaOnButton", metaOn);
            Assign(device, "metaOffButton", metaOff);
            Assign(device, "deviceList", deviceList);
            Assign(device, "networkList", networkList);
            Assign(device, "probeButton", networkButtons[0]);
            Assign(device, "adjustList", adjustList);
            Assign(device, "adjustButton", adjustButtons[0]);
            return device;
        }

        private static Button Tab(Transform parent, string key, string name, out Image dot)
        {
            GameObject tab = Box(parent, key, ButtonColor, true, true);
            var button = tab.AddComponent<Button>();
            button.targetGraphic = tab.GetComponent<Image>();
            Text keyText = Label(tab.transform, "Key", key, 32, FontStyle.Bold, TextColor, TextAnchor.MiddleCenter);
            Stretch(keyText.transform, new Vector2(8f, 34f), new Vector2(-8f, -6f));
            Text nameText = Label(tab.transform, "Name", name, 20, FontStyle.Normal, SoftTextColor, TextAnchor.MiddleCenter);
            Stretch(nameText.transform, new Vector2(8f, 6f), new Vector2(-8f, -62f));
            GameObject dotObject = Element(tab.transform, "Dot");
            dot = dotObject.AddComponent<Image>();
            dot.sprite = circle;
            dot.color = MutedTextColor;
            dot.raycastTarget = false;
            Place(dotObject.transform, new Vector2(1f, 1f), new Vector2(-14f, -14f), new Vector2(20f, 20f));
            return button;
        }

        private static GameObject UnitTemplate(Transform parent)
        {
            GameObject template = Box(parent, "Unit Template", UnitColor, true, false);
            Vertical(template, new RectOffset(22, 22, 18, 20), 12f);

            GameObject top = Element(template.transform, "Top");
            Horizontal(top, new RectOffset(0, 0, 0, 0), 16f, TextAnchor.MiddleLeft);
            GameObject badge = Box(top.transform, "Badge", ButtonColor, true, false);
            Horizontal(badge, new RectOffset(14, 14, 8, 8), 0f, TextAnchor.MiddleCenter);
            Text badgeLabel = Label(badge.transform, "Label", "IDLE", 21, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            badgeLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
            Text title = Label(top.transform, "Title", "", 26, FontStyle.Bold, TextColor, TextAnchor.MiddleLeft);
            Flexible(title.gameObject, 1f);

            KeyValueList(template.transform, "Info");
            template.SetActive(false);
            return template;
        }

        // Two columns of labeled values, with section headers and notes; see HDCKeyValueList.
        private static HDCKeyValueList KeyValueList(Transform parent, string name)
        {
            GameObject list = Element(parent, name);
            Vertical(list, new RectOffset(0, 0, 0, 0), 8f);
            var component = list.AddComponent<HDCKeyValueList>();

            GameObject row = Element(list.transform, "Row Template");
            Horizontal(row, new RectOffset(0, 0, 0, 0), 18f, TextAnchor.UpperLeft);
            Text label = Label(row.transform, "Label", "Label", 23, FontStyle.Normal, MutedTextColor, TextAnchor.UpperLeft);
            var labelLayout = label.gameObject.AddComponent<LayoutElement>();
            labelLayout.minWidth = LabelColumn;
            labelLayout.preferredWidth = LabelColumn;
            labelLayout.flexibleWidth = 0f;
            Text value = Label(row.transform, "Value", "Value", 24, FontStyle.Bold, TextColor, TextAnchor.UpperLeft);
            Flexible(value.gameObject, 1f);

            Text header = Label(list.transform, "Header Template", "SECTION", 22, FontStyle.Bold, AccentColor, TextAnchor.LowerLeft);
            var headerLayout = header.gameObject.AddComponent<LayoutElement>();
            headerLayout.minHeight = 50f;

            Text note = Label(list.transform, "Note Template", "", 23, FontStyle.Normal, TextColor, TextAnchor.UpperLeft);

            row.SetActive(false);
            header.gameObject.SetActive(false);
            note.gameObject.SetActive(false);
            Assign(component, "rowTemplate", row);
            Assign(component, "headerTemplate", header.gameObject);
            Assign(component, "noteTemplate", note.gameObject);
            return component;
        }

        private static HDCOptionPicker Picker(Transform parent)
        {
            GameObject root = Element(parent, "Option Picker");
            Stretch(root.transform, Vector2.zero, Vector2.zero);
            var picker = root.AddComponent<HDCOptionPicker>();

            GameObject modal = Box(root.transform, "Modal", DimColor, false, true);
            Stretch(modal.transform, Vector2.zero, Vector2.zero);
            GameObject sheet = Box(modal.transform, "Sheet", HeaderColor, true, true);
            Stretch(sheet.transform, new Vector2(16f, 16f), new Vector2(-16f, -560f));
            Vertical(sheet, new RectOffset(28, 28, 28, 28), 16f);
            Text title = Label(sheet.transform, "Title", "Select", 34, FontStyle.Bold, TextColor, TextAnchor.MiddleLeft);
            Text subtitle = Label(sheet.transform, "Subtitle", "Choose an option.", 24, FontStyle.Italic, MutedTextColor, TextAnchor.UpperLeft);
            RectTransform options = ScrollView(sheet.transform, "Options Scroll", out GameObject scroll);
            Flexible(scroll, 1f, true);
            Vertical(options.gameObject, new RectOffset(0, 0, 0, 0), 12f);
            options.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            Button optionTemplate = Bar(options, "Option Template", "Option", ButtonColor, 0f);
            Button closeButton = Bar(sheet.transform, "Close Button", "Cancel", DangerColor, 0f);

            modal.SetActive(false);
            Assign(picker, "modalRoot", modal);
            Assign(picker, "titleText", title);
            Assign(picker, "subtitleText", subtitle);
            Assign(picker, "optionsRoot", options);
            Assign(picker, "optionTemplate", optionTemplate);
            Assign(picker, "closeButton", closeButton);
            return picker;
        }

        // A full-screen reader for long texts, such as the events or a config.
        private static HDCAdsDebugViewer Viewer(Transform parent)
        {
            GameObject root = Element(parent, "Detail Viewer");
            Stretch(root.transform, Vector2.zero, Vector2.zero);
            var viewer = root.AddComponent<HDCAdsDebugViewer>();

            GameObject modal = Box(root.transform, "Modal", DimColor, false, true);
            Stretch(modal.transform, Vector2.zero, Vector2.zero);
            GameObject card = Box(modal.transform, "Card", HeaderColor, true, true);
            Stretch(card.transform, new Vector2(16f, 16f), new Vector2(-16f, -16f));
            Vertical(card, new RectOffset(28, 28, 24, 28), 16f);

            GameObject header = Element(card.transform, "Header");
            Horizontal(header, new RectOffset(0, 0, 0, 0), 14f, TextAnchor.MiddleLeft);
            Text title = Label(header.transform, "Title", "Viewer", 30, FontStyle.Bold, TextColor, TextAnchor.MiddleLeft);
            Flexible(title.gameObject, 1f);
            Button copy = Bar(header.transform, "Copy Button", "Copy", ButtonColor, 170f);
            Button close = Bar(header.transform, "Close Button", "Close", DangerColor, 150f);

            RectTransform content = ScrollView(card.transform, "Body Scroll", out GameObject scroll);
            Flexible(scroll, 1f, true);
            Vertical(content.gameObject, new RectOffset(8, 8, 8, 24), 0f);
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            Text bodyText = Label(content, "Body", "", 24, FontStyle.Normal, TextColor, TextAnchor.UpperLeft);

            modal.SetActive(false);
            Assign(viewer, "modalRoot", modal);
            Assign(viewer, "titleText", title);
            Assign(viewer, "bodyText", bodyText);
            Assign(viewer, "bodyScroll", scroll.GetComponent<ScrollRect>());
            Assign(viewer, "copyButton", copy);
            Assign(viewer, "closeButton", close);
            return viewer;
        }

        // Building blocks

        private static GameObject Element(Transform parent, string name)
        {
            var element = new GameObject(name, typeof(RectTransform)) { layer = UiLayer };
            element.transform.SetParent(parent, false);
            return element;
        }

        private static GameObject Box(Transform parent, string name, Color color, bool roundCorners, bool blocksClicks)
        {
            GameObject element = Element(parent, name);
            var image = element.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = blocksClicks;
            if (roundCorners && rounded != null)
            {
                image.sprite = rounded;
                image.type = Image.Type.Sliced;
                image.pixelsPerUnitMultiplier = 0.5f;
            }

            return element;
        }

        // A page fills the area under the page bar; the panel shows one at a time.
        private static GameObject Page(Transform parent, string name)
        {
            GameObject page = Element(parent, name);
            Stretch(page.transform, Vector2.zero, Vector2.zero);
            return page;
        }

        // The page's scrolling column of cards, from <paramref name="top"/> below the page's top edge.
        private static RectTransform Body(Transform page, float top)
        {
            RectTransform content = ScrollView(page, "Body", out GameObject body);
            Stretch(body.transform, Vector2.zero, new Vector2(0f, top));
            Vertical(content.gameObject, new RectOffset(24, 24, 8, 40), 20f);
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return content;
        }

        private static GameObject Card(Transform parent, string name)
        {
            GameObject card = Box(parent, name, CardColor, true, false);
            Vertical(card, new RectOffset(26, 26, 22, 26), 14f);
            return card;
        }

        // A card's title row, with buttons on the right.
        private static Button[] HeaderRow(Transform parent, string title, out Text titleText, params (string label, float width)[] buttons)
        {
            GameObject row = Element(parent, "Header Row");
            Horizontal(row, new RectOffset(0, 0, 0, 0), 14f, TextAnchor.MiddleLeft);
            titleText = Label(row.transform, "Card Title", title, 30, FontStyle.Bold, TextColor, TextAnchor.MiddleLeft);
            Flexible(titleText.gameObject, 1f);
            var made = new Button[buttons.Length];
            for (int i = 0; i < buttons.Length; i++)
                made[i] = Bar(row.transform, buttons[i].label + " Button", buttons[i].label, ButtonColor, buttons[i].width, 64f);
            return made;
        }

        private static Text Label(Transform parent, string name, string value, int size, FontStyle style, Color color, TextAnchor alignment)
        {
            var text = Element(parent, name).AddComponent<Text>();
            text.font = font;
            text.text = value;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            text.supportRichText = true;
            text.lineSpacing = 1.1f;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static Button ButtonWithLabel(Transform parent, string name, string label, Color color, int labelSize)
        {
            GameObject element = Box(parent, name, color, true, true);
            var button = element.AddComponent<Button>();
            button.targetGraphic = element.GetComponent<Image>();
            Text text = Label(element.transform, "Label", label, labelSize, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            Stretch(text.transform, new Vector2(10f, 4f), new Vector2(-10f, -4f));
            // Long group and position names shrink instead of spilling out of the button.
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = MinLabelSize;
            text.resizeTextMaxSize = labelSize;
            return button;
        }

        // A button in a layout row; a width of 0 lets the row stretch it.
        private static Button Bar(Transform parent, string name, string label, Color color, float width, float height = ButtonHeight)
        {
            Button button = ButtonWithLabel(parent, name, label, color, 26);
            Height(button.gameObject, height);
            if (width > 0f)
            {
                LayoutElement layout = button.GetComponent<LayoutElement>();
                layout.minWidth = width;
                layout.preferredWidth = width;
            }

            return button;
        }

        private static void Height(GameObject element, float height)
        {
            LayoutElement layout = element.GetComponent<LayoutElement>();
            if (layout == null)
                layout = element.AddComponent<LayoutElement>();
            layout.minHeight = height;
            layout.preferredHeight = height;
        }

        private static GameObject Grid(Transform parent, string name, int columns, float cellWidth, float cellHeight)
        {
            GameObject element = Element(parent, name);
            var grid = element.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(cellWidth, cellHeight);
            grid.spacing = new Vector2(12f, 12f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = columns;
            grid.childAlignment = TextAnchor.UpperLeft;
            return element;
        }

        private static RectTransform ScrollView(Transform parent, string name, out GameObject scroll)
        {
            scroll = Element(parent, name);
            var scrollRect = scroll.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 40f;

            GameObject viewport = Box(scroll.transform, "Viewport", ViewportColor, false, true);
            Stretch(viewport.transform, Vector2.zero, Vector2.zero);
            viewport.AddComponent<RectMask2D>();

            GameObject content = Element(viewport.transform, "Content");
            var rect = (RectTransform)content.transform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;

            scrollRect.viewport = (RectTransform)viewport.transform;
            scrollRect.content = rect;
            return rect;
        }

        private static void Vertical(GameObject element, RectOffset padding, float spacing)
        {
            var layout = element.AddComponent<VerticalLayoutGroup>();
            layout.padding = padding;
            layout.spacing = spacing;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
        }

        private static void Horizontal(GameObject element, RectOffset padding, float spacing, TextAnchor alignment)
        {
            var layout = element.AddComponent<HorizontalLayoutGroup>();
            layout.padding = padding;
            layout.spacing = spacing;
            layout.childAlignment = alignment;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
        }

        // A row whose children share its height and split its width by their flexible widths.
        private static void HorizontalStretch(GameObject element, RectOffset padding, float spacing)
        {
            Horizontal(element, padding, spacing, TextAnchor.MiddleCenter);
            element.GetComponent<HorizontalLayoutGroup>().childForceExpandHeight = true;
        }

        private static void Flexible(GameObject element, float width, bool height = false)
        {
            LayoutElement layout = element.GetComponent<LayoutElement>();
            if (layout == null)
                layout = element.AddComponent<LayoutElement>();
            layout.flexibleWidth = width;
            if (height)
                layout.flexibleHeight = 1f;
        }

        private static void Stretch(Transform element, Vector2 offsetMin, Vector2 offsetMax)
        {
            var rect = (RectTransform)element;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        // Spans the width at <paramref name="top"/> below the top edge, with a fixed height.
        private static void Top(Transform element, float top, float height)
        {
            var rect = (RectTransform)element;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, top);
            rect.sizeDelta = new Vector2(0f, height);
        }

        private static void Place(Transform element, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var rect = (RectTransform)element;
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Assign(Object target, string field, Object value)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(field);
            if (property == null)
                throw new System.ArgumentException($"{target.GetType().Name} has no field {field}");
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignTabs(HDCAdsDebugWorkspace workspace, Button[] buttons, Image[] dots)
        {
            var serialized = new SerializedObject(workspace);
            SerializedProperty tabs = serialized.FindProperty("tabs");
            tabs.arraySize = buttons.Length;
            for (int i = 0; i < buttons.Length; i++)
            {
                SerializedProperty tab = tabs.GetArrayElementAtIndex(i);
                tab.FindPropertyRelative("key").stringValue = Channels[i, 0];
                tab.FindPropertyRelative("button").objectReferenceValue = buttons[i];
                tab.FindPropertyRelative("dot").objectReferenceValue = dots[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignPages(HDCAdsDebugPanel panel, Button[] buttons, HDCDebugPage[] pages)
        {
            var serialized = new SerializedObject(panel);
            SerializedProperty list = serialized.FindProperty("pages");
            list.arraySize = buttons.Length;
            for (int i = 0; i < buttons.Length; i++)
            {
                SerializedProperty page = list.GetArrayElementAtIndex(i);
                page.FindPropertyRelative("button").objectReferenceValue = buttons[i];
                page.FindPropertyRelative("page").objectReferenceValue = pages[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
