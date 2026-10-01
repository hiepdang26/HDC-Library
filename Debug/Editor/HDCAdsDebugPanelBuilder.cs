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

        private static readonly Color PanelColor = new Color(0.113f, 0.113f, 0.113f, 1f);
        private static readonly Color SectionColor = new Color(1f, 1f, 1f, 0.031f);
        private static readonly Color CardColor = new Color(1f, 1f, 1f, 0.07f);
        private static readonly Color ScrollColor = new Color(0.08f, 0.08f, 0.08f, 0.85f);
        private static readonly Color ViewportColor = new Color(1f, 1f, 1f, 0.02f);
        private static readonly Color TitleBarColor = new Color(0.017f, 0f, 0.425f, 1f);
        private static readonly Color ButtonColor = new Color(0.31f, 0.31f, 0.31f, 1f);
        private static readonly Color AccentColor = new Color(0.86f, 0.53f, 0.08f, 1f);
        private static readonly Color CloseColor = new Color(0.717f, 0f, 0f, 1f);
        private static readonly Color NoticeColor = new Color(1f, 1f, 1f, 0.333f);
        private static readonly Color DimColor = new Color(0f, 0f, 0f, 0.6f);
        private static readonly Color PickerColor = new Color(0.16f, 0.16f, 0.16f, 1f);
        private static readonly Color PopupAreaColor = new Color(0.86f, 0.53f, 0.08f, 0.18f);

        private static Font font;

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

            var root = new GameObject("HDCAdsDebugPanel", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster))
            {
                layer = UiLayer,
            };
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0f;
            var panelComponent = root.AddComponent<HDCAdsDebugPanel>();

            Button open = CreateButton(root.transform, "Open Button", "ADS", TitleBarColor);
            Place(open.transform, new Vector2(0f, 1f), new Vector2(20f, -20f), new Vector2(160f, 70f));

            // Panel with its header and scroll view.
            GameObject panel = CreateImage(root.transform, "Panel", PanelColor, true);
            Stretch(panel.transform, new Vector2(30f, 200f), new Vector2(-30f, -80f));

            GameObject header = CreateElement(panel.transform, "Header");
            var headerRect = (RectTransform)header.transform;
            headerRect.anchorMin = new Vector2(0f, 1f);
            headerRect.anchorMax = Vector2.one;
            headerRect.pivot = new Vector2(0.5f, 1f);
            headerRect.sizeDelta = new Vector2(0f, 90f);
            Text headerTitle = CreateText(header.transform, "Title", "HDC Ads Debug", 28, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);
            Stretch(headerTitle.transform, new Vector2(20f, 0f), new Vector2(-330f, 0f));
            Button close = CreateButton(header.transform, "Close Button", "CLOSE", CloseColor, 28);
            Place(close.transform, new Vector2(1f, 0.5f), new Vector2(-15f, 0f), new Vector2(300f, 60f));

            RectTransform content = ScrollView(panel.transform, "Scroll Root", ScrollColor, 10, 10f);
            Stretch(content.parent.parent, new Vector2(10f, 10f), new Vector2(-10f, -100f));

            // Ad systems section.
            GameObject section = CreateImage(content, "Ad Systems Section", SectionColor, false);
            Column(section, 10, 10f);
            var systems = section.AddComponent<HDCAdSystemsSection>();

            TitleBar(section.transform, "Ad Systems");
            Notice(section.transform, "Pick a channel, then a group and a position from Remote Config, and call the HDCAds API.");
            Text summary = Information(section.transform, "Summary");
            Title(section.transform, "Public API Methods");
            Button[] channels1 = Row(section.transform, "Channels Row 1", "AL", "AR", "RW", "FA");
            Button[] channels2 = Row(section.transform, "Channels Row 2", "BN", "MREC", "CL", "PU");

            GameObject card = CreateImage(section.transform, "Detail Card", CardColor, false);
            Column(card, 12, 8f);
            Text channelTitle = Title(card.transform, "Selected channel: FA");
            Text selection = Information(card.transform, "Channel: FA | ForceAd");
            Button[] selectionRow = Row(card.transform, "Selection Row", "Refresh", "Group: -", "Position: -", "BreakAd Debug");
            selectionRow[0].image.color = AccentColor;
            Button[] actions = Row(card.transform, "Actions Row", "Init", "Show", "Hide", "Start BreakAd", "Stop BreakAd");

            RectTransform detailContent = ScrollView(card.transform, "Detail Scroll", ScrollColor, 12, 0f);
            Layout(detailContent.parent.parent.gameObject, 760f);
            Text detail = Information(detailContent, "Detail");

            // Where popups show while the popup channel is selected.
            GameObject popupArea = CreateImage(root.transform, "Popup Area", PopupAreaColor, false);
            Place(popupArea.transform, new Vector2(0.5f, 0f), new Vector2(0f, 230f), new Vector2(900f, 560f), new Vector2(0.5f, 0f));
            Text popupLabel = CreateText(popupArea.transform, "Label", "Popup area", 25, FontStyle.Italic, TextAnchor.MiddleCenter, Color.white);
            Stretch(popupLabel.transform, Vector2.zero, Vector2.zero);
            popupArea.SetActive(false);

            // Picker for groups and positions.
            GameObject pickerRoot = CreateElement(root.transform, "Option Picker");
            Stretch(pickerRoot.transform, Vector2.zero, Vector2.zero);
            var picker = pickerRoot.AddComponent<HDCOptionPicker>();
            GameObject modal = CreateImage(pickerRoot.transform, "Modal", DimColor, true);
            Stretch(modal.transform, Vector2.zero, Vector2.zero);
            GameObject pickerCard = CreateImage(modal.transform, "Card", PickerColor, true);
            Place(pickerCard.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 1300f), new Vector2(0.5f, 0.5f));
            Column(pickerCard, 20, 12f);
            Text pickerTitle = Title(pickerCard.transform, "Select");
            Text pickerSubtitle = Notice(pickerCard.transform, "Choose an option.");
            RectTransform options = ScrollView(pickerCard.transform, "Options Scroll", ScrollColor, 8, 8f);
            LayoutElement optionsLayout = options.parent.parent.gameObject.AddComponent<LayoutElement>();
            optionsLayout.flexibleHeight = 1f;
            optionsLayout.minHeight = 300f;
            Button optionTemplate = CreateButton(options, "Option Template", "Option", ButtonColor);
            Layout(optionTemplate.gameObject, 70f);
            Button pickerClose = CreateButton(pickerCard.transform, "Close Button", "CLOSE", CloseColor, 28);
            Layout(pickerClose.gameObject, 60f);
            modal.SetActive(false);

            Assign(panelComponent, "panelRoot", panel);
            Assign(panelComponent, "closeButton", close);
            Assign(panelComponent, "openButton", open);

            Assign(systems, "appLaunchButton", channels1[0]);
            Assign(systems, "appResumeButton", channels1[1]);
            Assign(systems, "rewardedButton", channels1[2]);
            Assign(systems, "forceAdButton", channels1[3]);
            Assign(systems, "bannerButton", channels2[0]);
            Assign(systems, "mrecButton", channels2[1]);
            Assign(systems, "collapsibleButton", channels2[2]);
            Assign(systems, "popupButton", channels2[3]);
            Assign(systems, "summaryText", summary);
            Assign(systems, "channelTitleText", channelTitle);
            Assign(systems, "selectionText", selection);
            Assign(systems, "detailText", detail);
            Assign(systems, "refreshButton", selectionRow[0]);
            Assign(systems, "groupButton", selectionRow[1]);
            Assign(systems, "positionButton", selectionRow[2]);
            Assign(systems, "detailButton", selectionRow[3]);
            Assign(systems, "initButton", actions[0]);
            Assign(systems, "showButton", actions[1]);
            Assign(systems, "hideButton", actions[2]);
            Assign(systems, "utilityPrimaryButton", actions[3]);
            Assign(systems, "utilitySecondaryButton", actions[4]);
            Assign(systems, "optionPicker", picker);
            Assign(systems, "popupArea", popupArea.transform);

            Assign(picker, "modalRoot", modal);
            Assign(picker, "titleText", pickerTitle);
            Assign(picker, "subtitleText", pickerSubtitle);
            Assign(picker, "optionsRoot", options);
            Assign(picker, "optionTemplate", optionTemplate);
            Assign(picker, "closeButton", pickerClose);

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            Debug.Log($"[HDCAds] Built {PrefabPath}.");
        }

        private static GameObject CreateElement(Transform parent, string name)
        {
            var element = new GameObject(name, typeof(RectTransform)) { layer = UiLayer };
            element.transform.SetParent(parent, false);
            return element;
        }

        private static GameObject CreateImage(Transform parent, string name, Color color, bool blocksClicks)
        {
            GameObject element = CreateElement(parent, name);
            var image = element.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = blocksClicks;
            return element;
        }

        private static Text CreateText(Transform parent, string name, string value, int size, FontStyle style, TextAnchor alignment, Color color)
        {
            var text = CreateElement(parent, name).AddComponent<Text>();
            text.font = font;
            text.text = value;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static Button CreateButton(Transform parent, string name, string label, Color color, int labelSize = 25)
        {
            GameObject element = CreateImage(parent, name, color, true);
            var button = element.AddComponent<Button>();
            button.targetGraphic = element.GetComponent<Image>();
            Text text = CreateText(element.transform, "Label", label, labelSize, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            Stretch(text.transform, new Vector2(6f, 0f), new Vector2(-6f, 0f));
            // Long group and position names shrink instead of spilling out of the button.
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = MinLabelSize;
            text.resizeTextMaxSize = labelSize;
            return button;
        }

        private static Button[] Row(Transform parent, string name, params string[] labels)
        {
            GameObject row = CreateElement(parent, name);
            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 5f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            Layout(row, 50f);

            var buttons = new Button[labels.Length];
            for (int i = 0; i < labels.Length; i++)
            {
                buttons[i] = CreateButton(row.transform, labels[i], labels[i], ButtonColor);
                buttons[i].gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            }

            return buttons;
        }

        private static void TitleBar(Transform parent, string value)
        {
            GameObject bar = CreateImage(parent, "Section Title", TitleBarColor, false);
            Layout(bar, 50f);
            Text label = CreateText(bar.transform, "Label", value, 25, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            Stretch(label.transform, Vector2.zero, Vector2.zero);
        }

        private static Text Title(Transform parent, string value)
        {
            Text text = CreateText(parent, "Title text", value, 28, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            Layout(text.gameObject, 50f);
            return text;
        }

        private static Text Information(Transform parent, string value)
        {
            Text text = CreateText(parent, "Information text", value, 25, FontStyle.Normal, TextAnchor.UpperLeft, Color.white);
            text.lineSpacing = 1.1f;
            return text;
        }

        private static Text Notice(Transform parent, string value)
        {
            Text text = CreateText(parent, "Notice text", value, 25, FontStyle.Italic, TextAnchor.MiddleLeft, NoticeColor);
            text.lineSpacing = 1.1f;
            return text;
        }

        // A vertical scroll view; returns its content, whose children stack top to bottom.
        private static RectTransform ScrollView(Transform parent, string name, Color color, int padding, float spacing)
        {
            GameObject scroll = CreateImage(parent, name, color, true);
            var scrollRect = scroll.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 30f;

            GameObject viewport = CreateImage(scroll.transform, "Viewport", ViewportColor, true);
            viewport.AddComponent<RectMask2D>();
            Stretch(viewport.transform, Vector2.zero, Vector2.zero);

            GameObject content = CreateElement(viewport.transform, "Content");
            var contentRect = (RectTransform)content.transform;
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = Vector2.one;
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.sizeDelta = Vector2.zero;
            Column(content, padding, spacing);
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = (RectTransform)viewport.transform;
            scrollRect.content = contentRect;
            return contentRect;
        }

        private static void Column(GameObject element, int padding, float spacing)
        {
            var layout = element.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(padding, padding, padding, padding);
            layout.spacing = spacing;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
        }

        private static void Layout(GameObject element, float height)
        {
            var layout = element.GetComponent<LayoutElement>();
            if (layout == null)
                layout = element.AddComponent<LayoutElement>();
            layout.preferredHeight = height;
            layout.minHeight = height;
        }

        private static void Stretch(Transform element, Vector2 offsetMin, Vector2 offsetMax)
        {
            var rect = (RectTransform)element;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static void Place(Transform element, Vector2 anchor, Vector2 position, Vector2 size, Vector2? pivot = null)
        {
            var rect = (RectTransform)element;
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot ?? anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Assign(Object target, string field, Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
