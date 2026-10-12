using System.IO;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace HDC.Ads.Editor
{
    public static class HDCBreakAdBuilder
    {
        private const string PrefabName = "HDCBreakAd.prefab";
        private const string RingName = "HDCBreakAdRing.png";
        private const string SpinnerName = "HDCBreakAdSpinner.png";
        private const int UiLayer = 5;
        private const int SpriteSize = 128;
        private const float RingWidth = 16f;

        private static readonly Color NoticeColor = new Color(0.047f, 0.117f, 0.179f, 0.88f);
        private static readonly Color AccentColor = new Color(1f, 0.804f, 0.098f, 1f);
        private static readonly Color TextColor = new Color(0.765f, 0.765f, 0.765f, 1f);

        internal static string Folder
        {
            get
            {
                string asmdef = CompilationPipeline.GetAssemblyDefinitionFilePathFromAssemblyName("HDC.Ads.BreakAd");
                return (string.IsNullOrEmpty(asmdef) ? "Assets/HDCLib/HDCAds/BreakAd" : Path.GetDirectoryName(asmdef)).Replace('\\', '/');
            }
        }

        internal static string PrefabPath => Folder + "/" + PrefabName;

        [MenuItem("HDC/Break Ad/Add to open scene", false, 65)]
        private static void AddToOpenScene()
        {
#if UNITY_2023_1_OR_NEWER
            HDCBreakAdView existing = Object.FindAnyObjectByType<HDCBreakAdView>();
#else
            HDCBreakAdView existing = Object.FindObjectOfType<HDCBreakAdView>();
#endif
            if (existing != null)
            {
                Selection.activeGameObject = existing.gameObject;
                EditorGUIUtility.PingObject(existing.gameObject);
                Debug.Log("[HDCAds] The open scene already has the break ad.");
                return;
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                Debug.LogError($"[HDCAds] {PrefabPath} is missing.");
                return;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Undo.RegisterCreatedObjectUndo(instance, "Add HDC break ad");
            Selection.activeGameObject = instance;
            EditorSceneManager.MarkSceneDirty(instance.scene);
        }

        [MenuItem("HDC/Break Ad/Add break_ads_config to the default ad cores", false, 66)]
        private static void AddConfig() => HDCBreakAdConfigTemplate.AddToDefaults();

        public static void Build()
        {
            Sprite ring = SaveSprite(RingName, RingPixels(false));
            Sprite spinner = SaveSprite(SpinnerName, RingPixels(true));
            Font font = Resources.GetBuiltinResource<Font>(
#if UNITY_2022_2_OR_NEWER
                "LegacyRuntime.ttf"
#else
                "Arial.ttf"
#endif
            );

            var root = new GameObject("HDCBreakAd", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler)) { layer = UiLayer };
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 15;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0f;
            var view = root.AddComponent<HDCBreakAdView>();

            RectTransform notice = Node("Notice", root.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -243f), new Vector2(542f, 112f));
            Image background = AddImage(Stretch("Background", notice), NoticeColor, AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"));
            background.type = Image.Type.Sliced;
            RectTransform messageRect = Stretch("Message", notice);
            messageRect.offsetMin = new Vector2(30f, 0f);
            messageRect.offsetMax = new Vector2(-135f, 0f);
            Text message = AddText(messageRect, "BREAK AD...", 52, TextAnchor.MiddleLeft, FontStyle.Italic, font);
            message.resizeTextForBestFit = true;
            message.resizeTextMinSize = 24;
            message.resizeTextMaxSize = 52;

            RectTransform countdown = Node("Countdown", notice, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-75f, 0f), new Vector2(90f, 90f));
            Image ringImage = AddImage(countdown, AccentColor, ring);
            ringImage.type = Image.Type.Filled;
            ringImage.fillMethod = Image.FillMethod.Radial360;
            ringImage.fillOrigin = (int)Image.Origin360.Top;
            ringImage.fillClockwise = false;
            Text seconds = AddText(Stretch("Seconds", countdown), "5", 46, TextAnchor.MiddleCenter, FontStyle.Bold, font);

            RectTransform loading = Node("Loading", notice, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-75f, 0f), new Vector2(90f, 90f));
            RectTransform spin = Stretch("Spinner", loading);
            AddImage(spin, AccentColor, spinner);
            AddText(Stretch("Label", loading), "AD", 30, TextAnchor.MiddleCenter, FontStyle.Bold, font);
            loading.gameObject.SetActive(false);
            notice.gameObject.SetActive(false);

            var serialized = new SerializedObject(view);
            serialized.FindProperty("notice").objectReferenceValue = notice.gameObject;
            serialized.FindProperty("message").objectReferenceValue = message;
            serialized.FindProperty("countdown").objectReferenceValue = countdown.gameObject;
            serialized.FindProperty("ring").objectReferenceValue = ringImage;
            serialized.FindProperty("seconds").objectReferenceValue = seconds;
            serialized.FindProperty("loading").objectReferenceValue = loading.gameObject;
            serialized.FindProperty("spinner").objectReferenceValue = spin;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            Debug.Log("[HDCAds] Built " + PrefabPath);
        }

        private static RectTransform Node(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
        {
            var node = new GameObject(name, typeof(RectTransform)) { layer = UiLayer };
            var rect = (RectTransform)node.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private static RectTransform Stretch(string name, Transform parent) => Node(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        private static Image AddImage(RectTransform rect, Color color, Sprite sprite)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static Text AddText(RectTransform rect, string value, int size, TextAnchor alignment, FontStyle style, Font font)
        {
            var text = rect.gameObject.AddComponent<Text>();
            text.font = font;
            text.text = value;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = TextColor;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static Color32[] RingPixels(bool fadingTail)
        {
            var pixels = new Color32[SpriteSize * SpriteSize];
            float center = (SpriteSize - 1) / 2f;
            float outer = SpriteSize / 2f - 1f;
            float inner = outer - RingWidth;
            for (int y = 0; y < SpriteSize; y++)
            {
                for (int x = 0; x < SpriteSize; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01(outer - distance + 0.5f) * Mathf.Clamp01(distance - inner + 0.5f);
                    if (fadingTail)
                    {
                        float angle = Mathf.Atan2(dx, dy) * Mathf.Rad2Deg;
                        if (angle < 0f)
                            angle += 360f;
                        alpha *= Mathf.Lerp(0.15f, 1f, Mathf.InverseLerp(0f, 140f, angle));
                    }

                    pixels[y * SpriteSize + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
                }
            }

            return pixels;
        }

        private static Sprite SaveSprite(string fileName, Color32[] pixels)
        {
            string folder = Folder + "/Sprites";
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder(Folder, "Sprites");
            string path = folder + "/" + fileName;
            var texture = new Texture2D(SpriteSize, SpriteSize, TextureFormat.RGBA32, false);
            texture.SetPixels32(pixels);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
