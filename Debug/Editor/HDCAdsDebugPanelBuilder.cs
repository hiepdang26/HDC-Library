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
        private static readonly Color DimColor = new Color(0f, 0f, 0f, 0.65f);
        private static readonly Color PopupAreaColor = new Color(0.31f, 0.275f, 0.898f, 0.18f);
        private static readonly Color ViewportColor = new Color(1f, 1f, 1f, 0.01f);

        private static readonly string[,] Channels =
        {
            { "AL", "AppLaunch" }, { "AR", "AppResume" }, { "RW", "Rewarded" }, { "FA", "ForceAd" },
            { "BN", "Banner" }, { "MREC", "Mrec" }, { "CL", "Collapsible" }, { "PU", "Popup" },
        };

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

            // Window: header, channel tabs, then the scrolling cards.
            GameObject window = Box(safeArea.transform, "Window", WindowColor, true, true);
            Stretch(window.transform, new Vector2(16f, 16f), new Vector2(-16f, -16f));
            var workspace = window.AddComponent<HDCAdsDebugWorkspace>();

            GameObject header = Box(window.transform, "Header", HeaderColor, true, false);
            Top(header.transform, 0f, 160f);
            Horizontal(header, new RectOffset(28, 20, 16, 16), 14f, TextAnchor.MiddleLeft);
            GameObject titles = Element(header.transform, "Titles");
            Vertical(titles, new RectOffset(0, 0, 0, 0), 4f);
            Flexible(titles, 1f);
            Label(titles.transform, "Title", "HDC Ads Debug", 40, FontStyle.Bold, TextColor, TextAnchor.MiddleLeft);
            Text status = Label(titles.transform, "Status", "", 22, FontStyle.Normal, MutedTextColor, TextAnchor.UpperLeft);
            Button initSdk = Bar(header.transform, "Init SDK Button", "Init SDK", PrimaryColor, 190f);
            Button refresh = Bar(header.transform, "Refresh Button", "Refresh", ButtonColor, 170f);
            Button close = Bar(header.transform, "Close Button", "Close", DangerColor, 150f);

            GameObject tabsRoot = Element(window.transform, "Channel Tabs");
            Top(tabsRoot.transform, -176f, 212f);
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

            RectTransform content = ScrollView(window.transform, "Body", out GameObject body);
            Stretch(body.transform, Vector2.zero, new Vector2(0f, -400f));
            Vertical(content.gameObject, new RectOffset(24, 24, 8, 32), 20f);
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Selection
            GameObject selectionCard = Card(content, "Selection Card");
            Text channelTitle = Label(selectionCard.transform, "Channel Title", "ForceAd · FA", 34, FontStyle.Bold, TextColor, TextAnchor.MiddleLeft);
            Text selection = Label(selectionCard.transform, "Selection", "", 24, FontStyle.Normal, SoftTextColor, TextAnchor.UpperLeft);
            GameObject selectionGrid = ButtonGrid(selectionCard.transform, "Selection Buttons");
            Button groupButton = ButtonWithLabel(selectionGrid.transform, "Group Button", "Group: -", ButtonColor, 26);
            Button positionButton = ButtonWithLabel(selectionGrid.transform, "Position Button", "Position: -", ButtonColor, 26);
            Button detailButton = ButtonWithLabel(selectionGrid.transform, "Detail Button", "BreakAd Debug", ButtonColor, 26);

            // Actions
            GameObject actionsCard = Card(content, "Actions Card");
            Label(actionsCard.transform, "Card Title", "Actions", 30, FontStyle.Bold, TextColor, TextAnchor.MiddleLeft);
            GameObject actionsGrid = ButtonGrid(actionsCard.transform, "Action Buttons");
            Button initButton = ButtonWithLabel(actionsGrid.transform, "Init Button", "Init", PrimaryColor, 26);
            Button showButton = ButtonWithLabel(actionsGrid.transform, "Show Button", "Show", PrimaryColor, 26);
            Button hideButton = ButtonWithLabel(actionsGrid.transform, "Hide Button", "Hide", ButtonColor, 26);
            Button utilityPrimary = ButtonWithLabel(actionsGrid.transform, "Utility Primary Button", "Start BreakAd", ButtonColor, 26);
            Button utilitySecondary = ButtonWithLabel(actionsGrid.transform, "Utility Secondary Button", "Stop BreakAd", ButtonColor, 26);
            Text lastCall = Label(actionsCard.transform, "Last Call", "Last call: -", 22, FontStyle.Normal, MutedTextColor, TextAnchor.UpperLeft);

            // Ad units
            GameObject unitsCard = Card(content, "Units Card");
            Button copyReport = CardHeader(unitsCard.transform, "Ad units", "Copy Report", 230f);
            Text unitsNotice = Label(unitsCard.transform, "Notice", "", 22, FontStyle.Italic, MutedTextColor, TextAnchor.UpperLeft);
            GameObject unitsList = Element(unitsCard.transform, "Units List");
            Vertical(unitsList, new RectOffset(0, 0, 0, 0), 12f);
            GameObject groupTemplate = GroupTemplate(unitsList.transform);
            GameObject unitTemplate = UnitTemplate(unitsList.transform);

            // Events
            GameObject eventsCard = Card(content, "Events Card");
            Label(eventsCard.transform, "Card Title", "Recent events", 30, FontStyle.Bold, TextColor, TextAnchor.MiddleLeft);
            Text eventsNotice = Label(eventsCard.transform, "Notice", "", 22, FontStyle.Italic, MutedTextColor, TextAnchor.UpperLeft);
            GameObject eventsList = Element(eventsCard.transform, "Events List");
            Vertical(eventsList, new RectOffset(0, 0, 0, 0), 8f);
            Text eventTemplate = Label(eventsList.transform, "Event Template", "", 22, FontStyle.Normal, SoftTextColor, TextAnchor.UpperLeft);
            eventTemplate.gameObject.SetActive(false);

            // System
            GameObject systemCard = Card(content, "System Card");
            Button systemToggle = CardHeader(systemCard.transform, "System", "Hide", 160f);
            GameObject systemBody = Element(systemCard.transform, "System Body");
            Vertical(systemBody, new RectOffset(0, 0, 0, 0), 14f);
            Text summary = Label(systemBody.transform, "Summary", "", 21, FontStyle.Normal, SoftTextColor, TextAnchor.UpperLeft);
            Text system = Label(systemBody.transform, "System", "", 22, FontStyle.Normal, TextColor, TextAnchor.UpperLeft);

            // Popup area, picker and viewer sit above the window.
            GameObject popupArea = Box(safeArea.transform, "Popup Area", PopupAreaColor, true, false);
            Place(popupArea.transform, new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(900f, 560f));
            Text popupLabel = Label(popupArea.transform, "Label", "Popup area", 26, FontStyle.Italic, SoftTextColor, TextAnchor.MiddleCenter);
            Stretch(popupLabel.transform, Vector2.zero, Vector2.zero);
            popupArea.SetActive(false);

            HDCOptionPicker picker = Picker(safeArea.transform);
            HDCAdsDebugViewer viewer = Viewer(safeArea.transform);
            window.SetActive(false);

            Assign(panel, "window", window);
            Assign(panel, "safeArea", safeArea.GetComponent<RectTransform>());
            Assign(panel, "scaler", scaler);
            Assign(panel, "closeButton", close);
            Assign(panel, "workspace", workspace);
            Assign(panel, "picker", picker);
            Assign(panel, "viewer", viewer);

            Assign(workspace, "statusText", status);
            Assign(workspace, "initSdkButton", initSdk);
            Assign(workspace, "refreshButton", refresh);
            AssignTabs(workspace, tabButtons, tabDots);
            Assign(workspace, "channelTitleText", channelTitle);
            Assign(workspace, "selectionText", selection);
            Assign(workspace, "groupButton", groupButton);
            Assign(workspace, "positionButton", positionButton);
            Assign(workspace, "detailButton", detailButton);
            Assign(workspace, "initButton", initButton);
            Assign(workspace, "showButton", showButton);
            Assign(workspace, "hideButton", hideButton);
            Assign(workspace, "utilityPrimaryButton", utilityPrimary);
            Assign(workspace, "utilitySecondaryButton", utilitySecondary);
            Assign(workspace, "lastCallText", lastCall);
            Assign(workspace, "groupTemplate", groupTemplate);
            Assign(workspace, "unitTemplate", unitTemplate);
            Assign(workspace, "unitsNoticeText", unitsNotice);
            Assign(workspace, "copyReportButton", copyReport);
            Assign(workspace, "eventTemplate", eventTemplate.gameObject);
            Assign(workspace, "eventsNoticeText", eventsNotice);
            Assign(workspace, "systemToggleButton", systemToggle);
            Assign(workspace, "systemBody", systemBody);
            Assign(workspace, "summaryText", summary);
            Assign(workspace, "systemText", system);
            Assign(workspace, "optionPicker", picker);
            Assign(workspace, "viewer", viewer);
            Assign(workspace, "popupArea", popupArea.GetComponent<RectTransform>());

            string path = PrefabPath;
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            Debug.Log($"[HDCAds] Built {path}.");
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

        private static GameObject GroupTemplate(Transform parent)
        {
            GameObject template = Box(parent, "Group Template", CardColor, true, false);
            Vertical(template, new RectOffset(20, 20, 14, 14), 6f);
            Label(template.transform, "Title", "group", 28, FontStyle.Bold, TextColor, TextAnchor.MiddleLeft);
            Label(template.transform, "Info", "", 21, FontStyle.Normal, SoftTextColor, TextAnchor.UpperLeft);
            template.SetActive(false);
            return template;
        }

        private static GameObject UnitTemplate(Transform parent)
        {
            GameObject template = Box(parent, "Unit Template", UnitColor, true, false);
            Vertical(template, new RectOffset(20, 20, 16, 16), 8f);

            GameObject top = Element(template.transform, "Top");
            Horizontal(top, new RectOffset(0, 0, 0, 0), 14f, TextAnchor.MiddleLeft);
            GameObject badge = Box(top.transform, "Badge", ButtonColor, true, false);
            Horizontal(badge, new RectOffset(14, 14, 8, 8), 0f, TextAnchor.MiddleCenter);
            Text badgeLabel = Label(badge.transform, "Label", "IDLE", 20, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            badgeLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
            Text title = Label(top.transform, "Title", "", 24, FontStyle.Bold, TextColor, TextAnchor.MiddleLeft);
            Flexible(title.gameObject, 1f);

            Label(template.transform, "Unit", "", 22, FontStyle.Normal, SoftTextColor, TextAnchor.UpperLeft);
            Text errors = Label(template.transform, "Errors", "", 22, FontStyle.Normal, TextColor, TextAnchor.UpperLeft);
            errors.gameObject.SetActive(false);
            Label(template.transform, "Stats", "", 21, FontStyle.Normal, MutedTextColor, TextAnchor.UpperLeft);
            template.SetActive(false);
            return template;
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
            Stretch(card.transform, new Vector2(24f, 120f), new Vector2(-24f, -120f));
            Vertical(card, new RectOffset(28, 28, 24, 28), 16f);

            GameObject header = Element(card.transform, "Header");
            Horizontal(header, new RectOffset(0, 0, 0, 0), 14f, TextAnchor.MiddleLeft);
            Text title = Label(header.transform, "Title", "Viewer", 32, FontStyle.Bold, TextColor, TextAnchor.MiddleLeft);
            Flexible(title.gameObject, 1f);
            Button copy = Bar(header.transform, "Copy Button", "Copy", ButtonColor, 170f);
            Button close = Bar(header.transform, "Close Button", "Close", DangerColor, 150f);

            RectTransform content = ScrollView(card.transform, "Body Scroll", out GameObject scroll);
            Flexible(scroll, 1f, true);
            Vertical(content.gameObject, new RectOffset(8, 8, 8, 8), 0f);
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            Text bodyText = Label(content, "Body", "", 24, FontStyle.Normal, TextColor, TextAnchor.UpperLeft);

            modal.SetActive(false);
            Assign(viewer, "modalRoot", modal);
            Assign(viewer, "titleText", title);
            Assign(viewer, "bodyText", bodyText);
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

        private static GameObject Card(Transform parent, string name)
        {
            GameObject card = Box(parent, name, CardColor, true, false);
            Vertical(card, new RectOffset(26, 26, 22, 24), 14f);
            return card;
        }

        private static Button CardHeader(Transform parent, string title, string buttonLabel, float buttonWidth)
        {
            GameObject row = Element(parent, "Header Row");
            Horizontal(row, new RectOffset(0, 0, 0, 0), 14f, TextAnchor.MiddleLeft);
            Text titleText = Label(row.transform, "Card Title", title, 30, FontStyle.Bold, TextColor, TextAnchor.MiddleLeft);
            Flexible(titleText.gameObject, 1f);
            return Bar(row.transform, buttonLabel + " Button", buttonLabel, ButtonColor, buttonWidth, 64f);
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
            var layout = button.gameObject.AddComponent<LayoutElement>();
            layout.minHeight = height;
            layout.preferredHeight = height;
            if (width > 0f)
            {
                layout.minWidth = width;
                layout.preferredWidth = width;
            }

            return button;
        }

        private static GameObject ButtonGrid(Transform parent, string name)
        {
            GameObject element = Element(parent, name);
            var grid = element.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(306f, ButtonHeight);
            grid.spacing = new Vector2(12f, 12f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
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
    }
}
