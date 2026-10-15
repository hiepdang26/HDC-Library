using System.IO;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace HDC.Ads.DebugUI.Editor
{
    public static class HDCAdsDebugPanelBuilder
    {
        private const string PrefabName = "HDCAdsDebugPanel.prefab";
        private const int UiLayer = 5;
        private const float ButtonHeight = 84f;
        private const float ChipHeight = 72f;
        private const float LabelColumn = 360f;
        private const float AreaFrame = 44f;
        private const float AreaHandle = 96f;
        private const float FoldButtonWidth = 170f;
        private const int TabColumns = 8;
        private static readonly Vector2 TabSize = new Vector2(116f, 60f);
        private const float TabSpacing = 8f;

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
        private static readonly Color AreaColor = new Color(0.31f, 0.275f, 0.898f, 0.18f);
        private static readonly Color AreaFrameColor = new Color(0.647f, 0.706f, 0.988f, 0.55f);
        private static readonly Color ViewportColor = new Color(1f, 1f, 1f, 0.01f);

        private static readonly string[] Pages = { "Ads", "Remote Config", "Events", "Device" };

        private static Font font;
        private static Sprite rounded;
        private static Sprite circle;

        internal static string PrefabPath
        {
            get
            {
                string asmdef = CompilationPipeline.GetAssemblyDefinitionFilePathFromAssemblyName("HDC.Ads.Debug");
                return (Path.GetDirectoryName(asmdef) ?? "Assets/HDCLib/HDCAds/Debug").Replace('\\', '/') + "/" + PrefabName;
            }
        }

        [MenuItem("HDC/Debug panel/Add to open scene", false, 60)]
        private static void AddToOpenScene()
        {
#if UNITY_2023_1_OR_NEWER
            HDCAdsDebugPanel existing = UnityEngine.Object.FindAnyObjectByType<HDCAdsDebugPanel>();
#else
            HDCAdsDebugPanel existing = UnityEngine.Object.FindObjectOfType<HDCAdsDebugPanel>();
#endif
            if (existing != null)
            {
                Selection.activeGameObject = existing.gameObject;
                EditorGUIUtility.PingObject(existing.gameObject);
                Debug.Log("[HDCAds] The open scene already has the debug panel. HDCAdsSetup carries one.");
                return;
            }

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

            GameObject window = Box(safeArea.transform, "Window", WindowColor, true, true);
            Stretch(window.transform, new Vector2(16f, 16f), new Vector2(-16f, -16f));
            var windowCanvas = window.AddComponent<Canvas>();
            windowCanvas.enabled = false;
            var windowRaycaster = window.AddComponent<GraphicRaycaster>();
            windowRaycaster.enabled = false;

            GameObject header = Box(window.transform, "Header", HeaderColor, true, false);
            Top(header.transform, 0f, 150f);
            Horizontal(header, new RectOffset(28, 20, 14, 14), 14f, TextAnchor.MiddleLeft);
            GameObject titles = Element(header.transform, "Titles");
            Vertical(titles, new RectOffset(0, 0, 0, 0), 4f);
            Flexible(titles, 1f);
            Label(titles.transform, "Title", "HDC Ads Debug", 28, FontStyle.Bold, TextColor, TextAnchor.MiddleLeft);
            Text status = Label(titles.transform, "Status", "", 22, FontStyle.Normal, SoftTextColor, TextAnchor.UpperLeft);
            Button initSdk = Bar(header.transform, "Init SDK Button", "Init SDK", PrimaryColor, 150f, ButtonHeight, 22);
            Button refresh = Bar(header.transform, "Refresh Button", "Refresh", ButtonColor, 140f, ButtonHeight, 22);
            Button expandAll = Bar(header.transform, "Expand All Button", "Expand all", ButtonColor, 160f, ButtonHeight, 22);
            Button collapseAll = Bar(header.transform, "Collapse All Button", "Collapse all", ButtonColor, 175f, ButtonHeight, 22);
            Button close = Bar(header.transform, "Close Button", "Close", DangerColor, 110f, ButtonHeight, 22);

            GameObject pageBar = Element(window.transform, "Page Bar");
            Top(pageBar.transform, -162f, 92f);
            HorizontalStretch(pageBar, new RectOffset(24, 24, 0, 0), 12f);
            var pageButtons = new Button[Pages.Length];
            for (int i = 0; i < Pages.Length; i++)
            {
                pageButtons[i] = ButtonWithLabel(pageBar.transform, Pages[i] + " Tab", Pages[i], ButtonColor, 26);
                Flexible(pageButtons[i].gameObject, 1f);
            }

            GameObject pagesRoot = Element(window.transform, "Pages");
            Stretch(pagesRoot.transform, Vector2.zero, new Vector2(0f, -270f));

            HDCDebugAreaEditor area = AdArea(safeArea.transform, window.AddComponent<CanvasGroup>());
            HDCOptionPicker picker = Picker(safeArea.transform);
            HDCAdsDebugViewer viewer = Viewer(safeArea.transform);

            HDCAdsDebugWorkspace ads = AdsPage(pagesRoot.transform, picker, viewer, area);
            HDCRemoteConfigPage config = RemoteConfigPage(pagesRoot.transform, viewer);
            HDCEventsPage events = EventsPage(pagesRoot.transform, picker, viewer);
            HDCDevicePage device = DevicePage(pagesRoot.transform);
            config.GetComponent<Canvas>().enabled = false;
            events.GetComponent<Canvas>().enabled = false;
            device.GetComponent<Canvas>().enabled = false;

            Assign(panel, "window", window);
            Assign(panel, "windowCanvas", windowCanvas);
            Assign(panel, "windowRaycaster", windowRaycaster);
            Assign(panel, "safeArea", safeArea.GetComponent<RectTransform>());
            Assign(panel, "scaler", scaler);
            Assign(panel, "closeButton", close);
            Assign(panel, "picker", picker);
            Assign(panel, "viewer", viewer);
            Assign(panel, "statusText", status);
            Assign(panel, "initSdkButton", initSdk);
            Assign(panel, "refreshButton", refresh);
            Assign(panel, "expandAllButton", expandAll);
            Assign(panel, "collapseAllButton", collapseAll);
            AssignPages(panel, pageButtons, new HDCDebugPage[] { ads, config, events, device });

            string path = PrefabPath;
            CarryOverSettings(root, path);
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            Debug.Log($"[HDCAds] Built {path}.");
        }

        private static HDCAdsDebugWorkspace AdsPage(Transform parent, HDCOptionPicker picker, HDCAdsDebugViewer viewer, HDCDebugAreaEditor area)
        {
            GameObject page = Page(parent, "Ads Page");
            var workspace = page.AddComponent<HDCAdsDebugWorkspace>();

            GameObject tabsRoot = Element(page.transform, "Channel Tabs");
            Top(tabsRoot.transform, 0f, TabSize.y);
            var grid = tabsRoot.AddComponent<GridLayoutGroup>();
            grid.padding = new RectOffset(24, 24, 0, 0);
            grid.cellSize = TabSize;
            grid.spacing = new Vector2(TabSpacing, TabSpacing);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = TabColumns;
            grid.childAlignment = TextAnchor.UpperCenter;
            Button tabTemplate = Tab(tabsRoot.transform, "Tab Template");
            tabTemplate.gameObject.SetActive(false);

            RectTransform content = Body(page.transform, -(TabSize.y + TabSpacing), out RectTransform body);

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
            Button actionTemplate = ButtonWithLabel(actionGrid.transform, "Action Template", "Action", ButtonColor, 26);
            actionTemplate.gameObject.SetActive(false);
            Text lastCall = Label(actions.transform, "Last Call", "Last call: -", 22, FontStyle.Normal, MutedTextColor, TextAnchor.UpperLeft);

            GameObject detail = Card(content, "Detail Card");
            Button[] detailButtons = HeaderRow(detail.transform, "Detail Information Ad", out _, ("Copy Report", 230f));
            Text groupTitle = Label(detail.transform, "Group Title", "", 30, FontStyle.Bold, AccentColor, TextAnchor.MiddleLeft);
            HDCKeyValueList groupList = KeyValueList(detail.transform, "Group Info");
            Text unitsNotice = Label(detail.transform, "Notice", "", 22, FontStyle.Italic, MutedTextColor, TextAnchor.UpperLeft);
            GameObject unitsList = Element(detail.transform, "Units List");
            Vertical(unitsList, new RectOffset(0, 0, 0, 0), 14f);
            GameObject unitTemplate = UnitTemplate(unitsList.transform);

            GameObject eventsCard = Card(content, "Events Card");
            Button[] eventButtons = HeaderRow(eventsCard.transform, "Recent Events", out _, ("Full Screen", 230f), ("Expand", FoldButtonWidth));
            GameObject eventsBody = Element(eventsCard.transform, "Events Body");
            Vertical(eventsBody, new RectOffset(0, 0, 0, 0), 10f);
            Text eventsNotice = Label(eventsBody.transform, "Notice", "", 22, FontStyle.Italic, MutedTextColor, TextAnchor.UpperLeft);
            HDCEventRows eventRows = EventRows(eventsBody.transform, "Events List");
            Fold(eventsCard, eventButtons[1], eventsBody);

            GameObject systemCard = Card(content, "System Card");
            Button[] systemButtons = HeaderRow(systemCard.transform, "System", out Text systemTitle, ("Expand", FoldButtonWidth));
            GameObject systemBody = Element(systemCard.transform, "System Body");
            Vertical(systemBody, new RectOffset(0, 0, 0, 0), 10f);
            HDCKeyValueList systemList = KeyValueList(systemBody.transform, "System List");
            Fold(systemCard, systemButtons[0], systemBody);

            Assign(workspace, "tabTemplate", tabTemplate);
            Assign(workspace, "tabsArea", tabsRoot.GetComponent<RectTransform>());
            Assign(workspace, "body", body);
            Assign(workspace, "channelTitleText", channelTitle);
            Assign(workspace, "selectionHintText", hint);
            Assign(workspace, "groupButton", groupButton);
            Assign(workspace, "positionButton", positionButton);
            Assign(workspace, "actionTemplate", actionTemplate);
            Assign(workspace, "lastCallText", lastCall);
            Assign(workspace, "groupTitleText", groupTitle);
            Assign(workspace, "groupList", groupList);
            Assign(workspace, "unitsNoticeText", unitsNotice);
            Assign(workspace, "unitTemplate", unitTemplate);
            Assign(workspace, "copyReportButton", detailButtons[0]);
            Assign(workspace, "eventsFullScreenButton", eventButtons[0]);
            Assign(workspace, "eventsBody", eventsBody);
            Assign(workspace, "eventsNoticeText", eventsNotice);
            Assign(workspace, "eventRows", eventRows);
            Assign(workspace, "systemTitleText", systemTitle);
            Assign(workspace, "systemBody", systemBody);
            Assign(workspace, "systemList", systemList);
            Assign(workspace, "optionPicker", picker);
            Assign(workspace, "viewer", viewer);
            Assign(workspace, "area", area);
            return workspace;
        }

        private static HDCDebugAreaEditor AdArea(Transform parent, CanvasGroup window)
        {
            GameObject area = Box(parent, "Ad Area", AreaColor, true, true);
            Place(area.transform, Vector2.zero, new Vector2(90f, 60f), new Vector2(900f, 760f));
            var editor = area.AddComponent<HDCDebugAreaEditor>();

            Image[] frame =
            {
                FrameBar(area.transform, "Frame Top", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(-AreaFrame, 0f), new Vector2(AreaFrame, AreaFrame)),
                FrameBar(area.transform, "Frame Bottom", Vector2.zero, new Vector2(1f, 0f), new Vector2(-AreaFrame, -AreaFrame), new Vector2(AreaFrame, 0f)),
                FrameBar(area.transform, "Frame Left", Vector2.zero, new Vector2(0f, 1f), new Vector2(-AreaFrame, 0f), Vector2.zero),
                FrameBar(area.transform, "Frame Right", new Vector2(1f, 0f), Vector2.one, Vector2.zero, new Vector2(AreaFrame, 0f)),
            };

            Text label = Label(area.transform, "Label", "Popup area", 26, FontStyle.Italic, SoftTextColor, TextAnchor.MiddleCenter);
            Stretch(label.transform, new Vector2(20f, 20f), new Vector2(-20f, -20f));

            GameObject caption = Box(area.transform, "Caption", HeaderColor, true, true);
            Place(caption.transform, new Vector2(0f, 1f), new Vector2(-AreaFrame, AreaFrame + 8f), new Vector2(300f, 56f));
            ((RectTransform)caption.transform).pivot = Vector2.zero;
            Horizontal(caption, new RectOffset(18, 18, 8, 8), 0f, TextAnchor.MiddleLeft);
            var fitter = caption.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            Text captionText = Label(caption.transform, "Text", "Popup area", 24, FontStyle.Bold, TextColor, TextAnchor.MiddleLeft);
            captionText.horizontalOverflow = HorizontalWrapMode.Overflow;

            var corners = new[] { new Vector2(-1f, -1f), new Vector2(1f, -1f), new Vector2(-1f, 1f), new Vector2(1f, 1f) };
            var handles = new Object[corners.Length];
            for (int i = 0; i < corners.Length; i++)
                handles[i] = Handle(area.transform, editor, corners[i]);

            Assign(editor, "fill", area.GetComponent<Image>());
            AssignArray(editor, "frame", frame);
            Assign(editor, "label", label);
            Assign(editor, "caption", caption.GetComponent<RectTransform>());
            Assign(editor, "captionText", captionText);
            AssignArray(editor, "handles", handles);
            Assign(editor, "fadedWhileEditing", window);
            area.SetActive(false);
            return editor;
        }

        private static Image FrameBar(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            GameObject bar = Box(parent, name, AreaFrameColor, false, true);
            var rect = (RectTransform)bar.transform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            return bar.GetComponent<Image>();
        }

        private static GameObject Handle(Transform parent, HDCDebugAreaEditor editor, Vector2 corner)
        {
            string side = (corner.y > 0f ? "Top " : "Bottom ") + (corner.x > 0f ? "Right" : "Left");
            GameObject handle = Element(parent, "Handle " + side);
            var image = handle.AddComponent<Image>();
            image.sprite = circle;
            image.color = AccentColor;
            var anchor = new Vector2(corner.x > 0f ? 1f : 0f, corner.y > 0f ? 1f : 0f);
            Place(handle.transform, anchor, corner * (AreaFrame / 2f), new Vector2(AreaHandle, AreaHandle));
            ((RectTransform)handle.transform).pivot = new Vector2(0.5f, 0.5f);
            var component = handle.AddComponent<HDCDebugAreaHandle>();
            Assign(component, "editor", editor);
            var serialized = new SerializedObject(component);
            serialized.FindProperty("corner").vector2Value = corner;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return handle;
        }

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
            GameObject modes = Grid(viewerCard.transform, "Mode Buttons", 3, 306f, ChipHeight);
            Button remote = ButtonWithLabel(modes.transform, "Remote Button", "Remote", ButtonColor, 24);
            Button defaults = ButtonWithLabel(modes.transform, "Default Button", "Default", ButtonColor, 24);
            Button applied = ButtonWithLabel(modes.transform, "Applied Button", "Applied", ButtonColor, 24);
            GameObject parts = Grid(viewerCard.transform, "Part Buttons", 3, 306f, ChipHeight);
            Button adsButton = ButtonWithLabel(parts.transform, "Ads Config Button", "ads_config", ButtonColor, 24);
            Button coreButton = ButtonWithLabel(parts.transform, "Ad Core Button", "Ad Core", ButtonColor, 24);
            Button keysButton = ButtonWithLabel(parts.transform, "All Keys Button", "All Keys", ButtonColor, 24);
            GameObject appliedBox = Box(viewerCard.transform, "Applied Note", UnitColor, true, false);
            Vertical(appliedBox, new RectOffset(20, 20, 14, 14), 0f);
            Text appliedNote = Label(appliedBox.transform, "Text", "", 24, FontStyle.Normal, TextColor, TextAnchor.UpperLeft);
            GameObject optionGrid = Grid(viewerCard.transform, "Options", 2, 462f, 64f);
            Button optionTemplate = ButtonWithLabel(optionGrid.transform, "Option Template", "Option", ButtonColor, 22);
            Text info = Label(viewerCard.transform, "Info", "", 22, FontStyle.Italic, MutedTextColor, TextAnchor.UpperLeft);
            GameObject bodyBox = Box(viewerCard.transform, "Body Box", UnitColor, true, false);
            Vertical(bodyBox, new RectOffset(20, 20, 16, 16), 0f);
            Text body = Label(bodyBox.transform, "Body", "", 22, FontStyle.Normal, TextColor, TextAnchor.UpperLeft);

            GameObject mapCard = Card(content, "Map Card");
            Button[] mapButtons = HeaderRow(mapCard.transform, "Ad Units Map", out _, ("Expand", FoldButtonWidth));
            GameObject mapBody = Element(mapCard.transform, "Map Body");
            Vertical(mapBody, new RectOffset(0, 0, 0, 0), 10f);
            HDCKeyValueList mapList = KeyValueList(mapBody.transform, "Map List");
            Fold(mapCard, mapButtons[0], mapBody);

            Assign(config, "statusList", statusList);
            Assign(config, "checkList", checkList);
            Assign(config, "remoteButton", remote);
            Assign(config, "defaultButton", defaults);
            Assign(config, "appliedButton", applied);
            Assign(config, "adsButton", adsButton);
            Assign(config, "coreButton", coreButton);
            Assign(config, "keysButton", keysButton);
            Assign(config, "optionTemplate", optionTemplate);
            Assign(config, "appliedNoteText", appliedNote);
            Assign(config, "viewerInfoText", info);
            Assign(config, "bodyText", body);
            Assign(config, "copyButton", viewerButtons[0]);
            Assign(config, "fullScreenButton", viewerButtons[1]);
            Assign(config, "mapBody", mapBody);
            Assign(config, "mapList", mapList);
            Assign(config, "viewer", viewer);
            return config;
        }

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
            HDCEventRows list = EventRows(listCard.transform, "Event Rows");

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
            Assign(events, "eventRows", list);
            Assign(events, "optionPicker", picker);
            Assign(events, "viewer", viewer);
            return events;
        }

        private static HDCDevicePage DevicePage(Transform parent)
        {
            GameObject page = Page(parent, "Device Page");
            var device = page.AddComponent<HDCDevicePage>();
            RectTransform content = Body(page.transform, 0f);

            Transform build = FoldCard(content, "Build Card", "Build", out _);
            HDCKeyValueList buildList = KeyValueList(build, "Build List");

            Transform library = FoldCard(content, "Library Card", "HDC Ads", out _);
            HDCKeyValueList libraryList = KeyValueList(library, "Library List");
            GameObject libraryButtons = Grid(library, "Library Buttons", 2, 462f, ChipHeight);
            Button debugLog = ButtonWithLabel(libraryButtons.transform, "Debug Log Button", "Debug Log: Off", ButtonColor, 24);
            Button testDevice = ButtonWithLabel(libraryButtons.transform, "Test Device Button", "Make Test Device", ButtonColor, 24);
            Button testAdUnits = ButtonWithLabel(libraryButtons.transform, "Test Ad Units Button", "Test Ad Units: Off", ButtonColor, 24);
            Button restart = ButtonWithLabel(libraryButtons.transform, "Restart App Button", "Restart App", DangerColor, 24);
            restart.gameObject.SetActive(false);
            Button clearData = ButtonWithLabel(libraryButtons.transform, "Clear Data Button", "Clear Data & Restart", DangerColor, 24);

            Transform consent = FoldCard(content, "Consent Card", "Consent & ATT", out _);
            HDCKeyValueList consentList = KeyValueList(consent, "Consent List");
            GameObject consentButtons = Grid(consent, "Consent Buttons", 2, 462f, ChipHeight);
            Button privacyOptions = ButtonWithLabel(consentButtons.transform, "Privacy Options Button", "Privacy Options", PrimaryColor, 24);
            Button resetConsent = ButtonWithLabel(consentButtons.transform, "Reset Consent Button", "Reset Consent", DangerColor, 24);

            Transform mediation = FoldCard(content, "Mediation Card", "Mediation", out _);
            HDCKeyValueList mediationList = KeyValueList(mediation, "Mediation List");
            GameObject mediationButtons = Grid(mediation, "Mediation Buttons", 3, 306f, ChipHeight);
            Button metaOn = ButtonWithLabel(mediationButtons.transform, "Meta Test On Button", "Meta Test Mode On", ButtonColor, 24);
            Button metaOff = ButtonWithLabel(mediationButtons.transform, "Meta Test Off Button", "Meta Test Mode Off", ButtonColor, 24);
            Button adInspector = ButtonWithLabel(mediationButtons.transform, "Ad Inspector Button", "Open Ad Inspector", PrimaryColor, 24);

            Transform deviceBody = FoldCard(content, "Device Card", "Device", out _);
            HDCKeyValueList deviceList = KeyValueList(deviceBody, "Device List");

            Transform network = FoldCard(content, "Network Card", "Network", out Button[] networkButtons, ("Check Public IP", 290f));
            HDCKeyValueList networkList = KeyValueList(network, "Network List");

            Transform adjust = FoldCard(content, "Adjust Card", "Adjust", out Button[] adjustButtons, ("Refresh", 190f));
            HDCKeyValueList adjustList = KeyValueList(adjust, "Adjust List");

            Assign(device, "buildList", buildList);
            Assign(device, "libraryList", libraryList);
            Assign(device, "debugLogButton", debugLog);
            Assign(device, "testDeviceButton", testDevice);
            Assign(device, "testAdUnitsButton", testAdUnits);
            Assign(device, "restartButton", restart);
            Assign(device, "clearDataButton", clearData);
            Assign(device, "consentList", consentList);
            Assign(device, "privacyOptionsButton", privacyOptions);
            Assign(device, "resetConsentButton", resetConsent);
            Assign(device, "mediationList", mediationList);
            Assign(device, "metaOnButton", metaOn);
            Assign(device, "metaOffButton", metaOff);
            Assign(device, "adInspectorButton", adInspector);
            Assign(device, "deviceList", deviceList);
            Assign(device, "networkList", networkList);
            Assign(device, "probeButton", networkButtons[0]);
            Assign(device, "adjustList", adjustList);
            Assign(device, "adjustButton", adjustButtons[0]);
            return device;
        }

        private static Transform FoldCard(Transform parent, string name, string title, out Button[] buttons, params (string label, float width)[] extra)
        {
            GameObject card = Card(parent, name);
            var all = new (string label, float width)[extra.Length + 1];
            extra.CopyTo(all, 0);
            all[extra.Length] = ("Expand", FoldButtonWidth);
            buttons = HeaderRow(card.transform, title, out _, all);
            GameObject body = Element(card.transform, "Body");
            Vertical(body, new RectOffset(0, 0, 0, 0), 14f);
            Fold(card, buttons[buttons.Length - 1], body);
            return body.transform;
        }

        private static void Fold(GameObject card, Button toggle, GameObject body)
        {
            body.SetActive(false);
            var foldout = card.AddComponent<HDCDebugFoldout>();
            Assign(foldout, "toggleButton", toggle);
            Assign(foldout, "body", body);
        }

        private static HDCEventRows EventRows(Transform parent, string name)
        {
            GameObject list = Element(parent, name);
            var rows = list.AddComponent<HDCEventRows>();
            GameObject holder = Element(list.transform, "Rows");
            Stretch(holder.transform, Vector2.zero, Vector2.zero);

            GameObject row = Box(holder.transform, "Event Template", UnitColor, true, false);
            Top(row.transform, 0f, 0f);
            Horizontal(row, new RectOffset(18, 18, 14, 14), 16f, TextAnchor.UpperLeft);
            Text time = Label(row.transform, "Time", "00:00:00", 21, FontStyle.Normal, MutedTextColor, TextAnchor.UpperLeft);
            Width(time.gameObject, 100f);
            GameObject badge = Box(row.transform, "Badge", ButtonColor, true, false);
            Horizontal(badge, new RectOffset(8, 8, 5, 5), 0f, TextAnchor.MiddleCenter);
            Width(badge, 150f);
            Label(badge.transform, "Label", "Loaded", 19, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            GameObject main = Element(row.transform, "Main");
            Vertical(main, new RectOffset(0, 0, 0, 0), 4f);
            Flexible(main, 1f);
            Label(main.transform, "Title", "", 22, FontStyle.Bold, TextColor, TextAnchor.UpperLeft);
            Label(main.transform, "Detail", "", 20, FontStyle.Normal, SoftTextColor, TextAnchor.UpperLeft);
            Label(main.transform, "Explain", "", 20, FontStyle.Italic, MutedTextColor, TextAnchor.UpperLeft);
            row.SetActive(false);

            Assign(rows, "rowTemplate", row);
            Assign(rows, "rowsRoot", holder.GetComponent<RectTransform>());
            return rows;
        }

        private static Button Tab(Transform parent, string name)
        {
            GameObject tab = Box(parent, name, ButtonColor, true, true);
            var button = tab.AddComponent<Button>();
            button.targetGraphic = tab.GetComponent<Image>();
            Text keyText = Label(tab.transform, "Key", "KEY", 24, FontStyle.Bold, TextColor, TextAnchor.MiddleCenter);
            Stretch(keyText.transform, new Vector2(4f, 22f), new Vector2(-4f, -2f));
            Text nameText = Label(tab.transform, "Name", "Channel", 14, FontStyle.Normal, SoftTextColor, TextAnchor.MiddleCenter);
            Stretch(nameText.transform, new Vector2(4f, 3f), new Vector2(-4f, -38f));
            GameObject dotObject = Element(tab.transform, "Dot");
            var dot = dotObject.AddComponent<Image>();
            dot.sprite = circle;
            dot.color = MutedTextColor;
            dot.raycastTarget = false;
            Place(dotObject.transform, new Vector2(1f, 1f), new Vector2(-8f, -8f), new Vector2(12f, 12f));
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

        private static GameObject Page(Transform parent, string name)
        {
            GameObject page = Element(parent, name);
            Stretch(page.transform, Vector2.zero, Vector2.zero);
            page.AddComponent<Canvas>();
            page.AddComponent<GraphicRaycaster>().enabled = false;
            return page;
        }

        private static RectTransform Body(Transform page, float top) => Body(page, top, out _);

        private static RectTransform Body(Transform page, float top, out RectTransform scroll)
        {
            RectTransform content = ScrollView(page, "Body", out GameObject body);
            scroll = (RectTransform)body.transform;
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
            Text text = Element(parent, name).AddComponent<HDCDebugText>();
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
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return button;
        }

        private static Button Bar(Transform parent, string name, string label, Color color, float width, float height = ButtonHeight, int labelSize = 26)
        {
            Button button = ButtonWithLabel(parent, name, label, color, labelSize);
            Height(button.gameObject, height);
            if (width > 0f)
            {
                LayoutElement layout = button.GetComponent<LayoutElement>();
                layout.minWidth = width;
                layout.preferredWidth = width;
            }

            return button;
        }

        private static void Width(GameObject element, float width)
        {
            LayoutElement layout = element.GetComponent<LayoutElement>();
            if (layout == null)
                layout = element.AddComponent<LayoutElement>();
            layout.minWidth = width;
            layout.preferredWidth = width;
            layout.flexibleWidth = 0f;
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

        private static void CarryOverSettings(GameObject root, string path)
        {
            var previous = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (previous == null)
                return;
            Copy(previous.GetComponent<Canvas>(), root.GetComponent<Canvas>(), "m_SortingOrder");
            Copy(previous.GetComponent<GraphicRaycaster>(), root.GetComponent<GraphicRaycaster>(), "m_IgnoreReversedGraphics", "m_BlockingObjects", "m_BlockingMask");
            foreach (GraphicRaycaster nested in root.transform.Find("Safe Area/Window").GetComponentsInChildren<GraphicRaycaster>(true))
                Copy(root.GetComponent<GraphicRaycaster>(), nested, "m_IgnoreReversedGraphics", "m_BlockingObjects", "m_BlockingMask");
            Copy(previous.GetComponent<HDCAdsDebugPanel>(), root.GetComponent<HDCAdsDebugPanel>(),
                "startOpen", "activationCorner", "activationSize", "requiredTaps", "tapWindowSeconds", "cooldownSeconds", "f10Toggles", "keepAcrossScenes");
        }

        private static void Copy(Object from, Object to, params string[] fields)
        {
            if (from == null || to == null)
                return;
            var source = new SerializedObject(from);
            var target = new SerializedObject(to);
            foreach (string field in fields)
            {
                SerializedProperty property = source.FindProperty(field);
                if (property != null)
                    target.CopyFromSerializedProperty(property);
            }

            target.ApplyModifiedPropertiesWithoutUndo();
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

        private static void AssignArray(Object target, string field, Object[] values)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty list = serialized.FindProperty(field);
            if (list == null)
                throw new System.ArgumentException($"{target.GetType().Name} has no field {field}");
            list.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                list.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
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
