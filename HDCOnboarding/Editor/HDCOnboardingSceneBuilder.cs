using System;
using HDC.Ads;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace HDC.Onboarding.Editor
{
    internal static class HDCOnboardingSceneBuilder
    {
        private const string SetupPrefabGuid = "10e1a444d69fc4f72a856e51d2def004";
        private const int UiLayer = 5;

        private static readonly Vector2 Reference = new Vector2(1920, 1080);
        private static readonly Color Accent = new Color(0.16f, 0.6f, 1f);
        private static readonly Color Card = new Color(0.09f, 0.13f, 0.2f, 0.96f);
        private static readonly Color Backdrop = new Color(0.06f, 0.09f, 0.14f);
        private static readonly Color Muted = new Color(0.36f, 0.4f, 0.47f);

        internal static void BuildFromCommandLine()
        {
            try
            {
                Build();
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        internal static void BuildIntroFromCommandLine()
        {
            try
            {
                EnsureFolder(HDCOnboardingPaths.ScenesFolder);
                BuildIntro();
                AssetDatabase.SaveAssets();
                Debug.Log("[Splash] Built " + HDCOnboardingPaths.IntroScene);
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        internal static void BuildSplashFromCommandLine()
        {
            try
            {
                var noInternet = AssetDatabase.LoadAssetAtPath<GameObject>(HDCOnboardingPaths.NoInternetPrefab);
                if (noInternet == null)
                    throw new InvalidOperationException(HDCOnboardingPaths.NoInternetPrefab + " is missing: run BuildFromCommandLine");
                GameObject nativeBanner = BuildNativeBannerPrefab();
                BuildSplashAdAreaPrefabs();
                BuildSplash(noInternet, nativeBanner);
                AssetDatabase.SaveAssets();
                Debug.Log("[Splash] Built " + HDCOnboardingPaths.NativeBannerPrefab + " and " + HDCOnboardingPaths.SplashScene);
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        internal static void UpdateSplashAdAreasFromCommandLine()
        {
            try
            {
                BuildSplashAdAreaPrefabs();
                Scene scene = EditorSceneManager.OpenScene(HDCOnboardingPaths.SplashScene, OpenSceneMode.Single);
                Debug.Log("[Splash] " + HDCSplashAdAreas.Move(scene));
                EditorSceneManager.SaveScene(scene);
                AssetDatabase.SaveAssets();
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        internal static void Build()
        {
            EnsureFolder(HDCOnboardingPaths.ScenesFolder);
            EnsureFolder(HDCOnboardingPaths.PrefabsFolder);
            GameObject noInternet = BuildNoInternetPrefab();
            GameObject nativeBanner = BuildNativeBannerPrefab();
            BuildSplashAdAreaPrefabs();
            BuildSplash(noInternet, nativeBanner);
            BuildIntro();
            AssetDatabase.SaveAssets();
            Debug.Log("[Splash] Built " + HDCOnboardingPaths.SplashScene + ", " + HDCOnboardingPaths.IntroScene + ", " + HDCOnboardingPaths.NoInternetPrefab +
                      " and " + HDCOnboardingPaths.NativeBannerPrefab);
        }

        private static GameObject BuildNoInternetPrefab()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Canvas canvas = AddCanvas("HDCNoInternet", null, 1000);
            var noInternet = canvas.gameObject.AddComponent<HDCNoInternet>();
            RectTransform popup = Stretch("Popup", canvas.transform);
            AddImage(Stretch("Dim", popup), new Color(0, 0, 0, 0.6f), null, true);
            RectTransform window = Node("Window", popup, Center, Center, new Vector2(0, -35), new Vector2(1100, 675));
            AddImage(window, Card, BuiltinSprite("UI/Skin/Background.psd"), true);
            AddText(Node("Title", window, Center, Center, new Vector2(0, 264), new Vector2(900, 85)), "No Internet Connection", 54,
                TextAlignmentOptions.Center);
            AddImage(Node("Line", window, Center, Center, new Vector2(0, 209), new Vector2(900, 4)), new Color(0, 0.89f, 1f, 0.62f));

            RectTransform tryContent = Stretch("TryReconnectContent", window);
            AddText(Node("Message", tryContent, Center, Center, new Vector2(0, 27), new Vector2(920, 155)),
                "Please check your connection to continue and enjoy the best experience.", 44, TextAlignmentOptions.Center,
                new Color(0.87f, 0.98f, 1f));
            Button tryButton = AddButton(Node("TryReconnectButton", tryContent, Center, Center, new Vector2(0, -208), new Vector2(342, 124)),
                "Try Reconnect", 44, Accent);

            RectTransform reconnectContent = Stretch("ReconnectContent", window);
            AddText(Node("Message", reconnectContent, Center, Center, new Vector2(0, 27), new Vector2(920, 155)), "Reconnecting to the internet…",
                44, TextAlignmentOptions.Center, new Color(0.87f, 0.98f, 1f));
            RectTransform waiting = Node("Waiting", reconnectContent, Center, Center, new Vector2(0, -208), new Vector2(342, 124));
            AddImage(waiting, Muted, BuiltinSprite("UI/Skin/UISprite.psd"));
            AddText(Stretch("Label", waiting), "Reconnect...", 44, TextAlignmentOptions.Center);
            reconnectContent.gameObject.SetActive(false);
            popup.gameObject.SetActive(false);

            Set(noInternet, "popup", popup.gameObject);
            Set(noInternet, "tryReconnectContent", tryContent.gameObject);
            Set(noInternet, "reconnectContent", reconnectContent.gameObject);
            UnityEventTools.AddVoidPersistentListener(tryButton.onClick, noInternet.ToggleReconnect);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(canvas.gameObject, HDCOnboardingPaths.NoInternetPrefab);
            Object.DestroyImmediate(canvas.gameObject);
            return prefab;
        }

        private static GameObject BuildNativeBannerPrefab()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("HDCNativeBanner");
            var loader = root.AddComponent<HDCNativeBannerLoader>();
            Canvas canvas = AddCanvas("Banner Canvas", root.transform, 0);
            RectTransform area = Node("Banner Area", canvas.transform, Vector2.zero, new Vector2(1f, 0.15f), Vector2.zero, Vector2.zero);
            Set(loader, "bannerArea", area);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, HDCOnboardingPaths.NativeBannerPrefab);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static void BuildSplashAdAreaPrefabs()
        {
            BuildAreaPrefab(HDCOnboardingPaths.NativeSplashAreaPrefab, "HDCNativeSplashArea", HDCSplashAdAreas.NativeAreaName, HDCSplashAdAreaKind.NativeSplash,
                true, Vector2.zero, new Vector2(1, 0.175f), new Color(0, 0, 0, 0.25f));
            BuildAreaPrefab(HDCOnboardingPaths.NativeSplashAdMobAreaPrefab, "HDCNativeSplashAdMobArea", HDCSplashAdAreas.AdMobAreaName,
                HDCSplashAdAreaKind.NativeSplashAdMob, false, new Vector2(0.5f, 0), new Vector2(1, 0.9167f), null);
        }

        private static void BuildAreaPrefab(string path, string rootName, string areaName, HDCSplashAdAreaKind kind, bool followOrientation,
            Vector2 anchorMin, Vector2 anchorMax, Color? color)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Canvas canvas = AddCanvas(rootName, null, 1);
            Object.DestroyImmediate(canvas.GetComponent<GraphicRaycaster>());
            RectTransform area = Node(areaName, canvas.transform, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
            if (color.HasValue)
                AddImage(area, color.Value);
            var component = area.gameObject.AddComponent<HDCSplashAdArea>();
            Set(component, "kind", kind);
            Set(component, "followOrientation", followOrientation);
            area.anchorMin = anchorMin;
            area.anchorMax = anchorMax;
            area.offsetMin = Vector2.zero;
            area.offsetMax = Vector2.zero;
            PrefabUtility.SaveAsPrefabAsset(canvas.gameObject, path);
            Object.DestroyImmediate(canvas.gameObject);
        }

        private static void BuildSplash(GameObject noInternetPrefab, GameObject nativeBannerPrefab)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            AddCamera();

            var setupPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(SetupPrefabGuid));
            if (setupPrefab == null)
                throw new InvalidOperationException("HDCAdsSetup.prefab is missing (guid " + SetupPrefabGuid + ")");
            var setup = (GameObject)PrefabUtility.InstantiatePrefab(setupPrefab, scene);
            var setupComponent = setup.GetComponent<HDCAdsSetup>();
            Set(setupComponent, "nextScene", "");
            Set(setupComponent, "startAllChannels", false);
            Set(setupComponent, "runAppLaunch", false);

            Canvas canvas = AddCanvas("Canvas", null, 0);
            RectTransform backdrop = Stretch("Backdrop", canvas.transform);
            AddImage(backdrop, Backdrop);

            RectTransform header = Node("Header", canvas.transform, new Vector2(0, 1), Vector2.one, new Vector2(0, -45), new Vector2(0, 90));
            AddImage(header, new Color(0, 0, 0, 0.6f));
            var loadingView = header.gameObject.AddComponent<HDCSplashLoadingView>();
            TextMeshProUGUI loadingText = AddText(Node("LoadingText", header, new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(50, 0),
                new Vector2(-100, 50)), "Starting up...", 34, TextAlignmentOptions.Left);
            TextMeshProUGUI progressText = AddText(Node("Progress", header, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-30, 0),
                new Vector2(200, 50), new Vector2(1, 0.5f)), "0%", 34, TextAlignmentOptions.Right);
            RectTransform bar = Node("LoadingBar", header, new Vector2(0, 1), Vector2.one, new Vector2(0, -6), new Vector2(0, 12));
            AddImage(bar, new Color(1, 1, 1, 0.15f));
            Image fill = AddImage(Stretch("Fill", bar), Accent, BuiltinSprite("UI/Skin/UISprite.psd"));
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillAmount = 0f;
            Set(loadingView, "loadingText", loadingText);
            Set(loadingView, "progressText", progressText);
            Set(loadingView, "fill", fill);

            var noInternet = (GameObject)PrefabUtility.InstantiatePrefab(noInternetPrefab, scene);

            var appId = new GameObject("HDCAppIdLabel").AddComponent<HDCAppIdLabel>();
            Set(appId, "target", loadingText.rectTransform);

            var splash = new GameObject("HDCSplash").AddComponent<HDCSplash>();
            Set(splash, "noInternet", noInternet.GetComponent<HDCNoInternet>());
            UnityEventTools.AddPersistentListener(splash.ProgressChanged, loadingView.UpdateProgress);
            EditorUtility.SetDirty(splash);

            HDCSplashAdAreas.Move(scene);

            new GameObject("HDCAdTracking").AddComponent<HDCAdTracking>();
            PrefabUtility.InstantiatePrefab(nativeBannerPrefab, scene);

            EditorSceneManager.SaveScene(scene, HDCOnboardingPaths.SplashScene);
        }

        private static void BuildIntro()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            AddCamera();
            Canvas canvas = AddCanvas("Canvas", null, 0);
            AddImage(Stretch("Background", canvas.transform), Backdrop);

            RectTransform areas = Stretch("Ad Areas", canvas.transform);
            RectTransform adRight = Node("Ad Right", areas, new Vector2(0.5f, 0), Vector2.one, new Vector2(0, 87.5f), new Vector2(-100, -175));
            RectTransform adLeft = Node("Ad Left", areas, Vector2.zero, new Vector2(0.5f, 1), new Vector2(0, 87.5f), new Vector2(-100, -175));
            RectTransform adWide = Node("Ad Wide", areas, Vector2.zero, new Vector2(1, 0.872f), Vector2.zero, Vector2.zero);

            HDCIntroCoverView left = BuildCoverView(canvas.transform, "AL View", HDCIntroType.AL, true, adRight);
            HDCIntroCoverView right = BuildCoverView(canvas.transform, "AR View", HDCIntroType.AR, false, adLeft);
            HDCIntroBackgroundView wide = BuildBackgroundView(canvas.transform, adWide);

            var intro = new GameObject("HDCIntro").AddComponent<HDCIntro>();
            Set(intro, "views", new Object[] { left, right, wide });
            EditorSceneManager.SaveScene(scene, HDCOnboardingPaths.IntroScene);
        }

        private static HDCIntroCoverView BuildCoverView(Transform parent, string name, HDCIntroType type, bool panelOnLeft, RectTransform adArea)
        {
            RectTransform root = Stretch(name, parent);
            var view = root.gameObject.AddComponent<HDCIntroCoverView>();
            Image background = AddImage(Stretch("Background", root), Backdrop);
            AddImage(Stretch("Dim", root), new Color(0, 0, 0, 0.35f));
            RectTransform panel = Node("Panel", root, panelOnLeft ? Vector2.zero : new Vector2(0.5f, 0), panelOnLeft ? new Vector2(0.5f, 1) : Vector2.one,
                new Vector2(0, 37.5f), new Vector2(0, -75));
            Image cover = AddImage(Node("Cover", panel, Center, Center, new Vector2(0, 131), new Vector2(792, 501)), Muted);
            cover.preserveAspect = true;
            TextMeshProUGUI description = AddText(Node("Description", panel, Center, Center, new Vector2(0, -305), new Vector2(800, 110)),
                "Description", 34, TextAlignmentOptions.Center);
            description.enableAutoSizing = true;
            description.fontSizeMin = 22;
            description.fontSizeMax = 36;
            Button continueButton = AddButton(Node("ContinueButton", panel, Center, Center, new Vector2(0, -440), new Vector2(500, 130)),
                "Tap to continue", 44, Accent);
            continueButton.gameObject.SetActive(false);
            TextMeshProUGUI countdown = AddText(Node("Countdown", panel, Center, Center, new Vector2(0, -440), new Vector2(500, 130)),
                "Next in 5s.", 44, TextAlignmentOptions.Center);

            Set(view, "introType", type);
            Set(view, "adArea", adArea);
            Set(view, "background", background);
            Set(view, "cover", cover);
            Set(view, "description", description);
            Set(view, "countdown", countdown);
            Set(view, "continueButton", continueButton);
            root.gameObject.SetActive(false);
            return view;
        }

        private static HDCIntroBackgroundView BuildBackgroundView(Transform parent, RectTransform adArea)
        {
            RectTransform root = Stretch("PT View", parent);
            var view = root.gameObject.AddComponent<HDCIntroBackgroundView>();
            Image background = AddImage(Stretch("Background", root), Backdrop);
            RectTransform corner = Node("Close", root, new Vector2(0, 1), new Vector2(0, 1), new Vector2(75, -75), new Vector2(70, 70));

            RectTransform countdownRoot = Stretch("Countdown", corner);
            AddImage(countdownRoot, new Color(0, 0, 0, 0.6f), BuiltinSprite("UI/Skin/Knob.psd"));
            TextMeshProUGUI seconds = AddText(Stretch("Seconds", countdownRoot), "5", 34, TextAlignmentOptions.Center);
            AddText(Node("Hint", countdownRoot, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(12, 0), new Vector2(260, 45),
                new Vector2(0, 0.5f)), "before close", 26, TextAlignmentOptions.Left);

            RectTransform close = Stretch("ContinueButton", corner);
            AddImage(close, new Color(0, 0, 0, 0.6f), BuiltinSprite("UI/Skin/Knob.psd"), true);
            Button continueButton = close.gameObject.AddComponent<Button>();
            AddText(Stretch("Icon", close), "X", 34, TextAlignmentOptions.Center);
            AddText(Node("Hint", close, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(12, 0), new Vector2(260, 45), new Vector2(0, 0.5f)),
                "tap to close", 26, TextAlignmentOptions.Left);
            close.gameObject.SetActive(false);

            Set(view, "adArea", adArea);
            Set(view, "background", background);
            Set(view, "countdownRoot", countdownRoot.gameObject);
            Set(view, "countdown", seconds);
            Set(view, "continueButton", continueButton);
            root.gameObject.SetActive(false);
            return view;
        }

        private static Vector2 Center => new Vector2(0.5f, 0.5f);

        private static void AddCamera()
        {
            var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
            camera.tag = "MainCamera";
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.orthographic = true;
            camera.transform.position = new Vector3(0, 0, -10);
        }

        private static Canvas AddCanvas(string name, Transform parent, int order)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)) { layer = UiLayer };
            if (parent != null)
                go.transform.SetParent(parent, false);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = order;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = Reference;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        private static RectTransform Node(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size,
            Vector2? pivot = null)
        {
            var go = new GameObject(name, typeof(RectTransform)) { layer = UiLayer };
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot ?? new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private static RectTransform Stretch(string name, Transform parent) => Node(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        private static Image AddImage(RectTransform rect, Color color, Sprite sprite = null, bool raycast = false)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.sprite = sprite;
            image.raycastTarget = raycast;
            if (sprite != null && sprite.border != Vector4.zero)
                image.type = Image.Type.Sliced;
            return image;
        }

        private static TextMeshProUGUI AddText(RectTransform rect, string text, float size, TextAlignmentOptions alignment, Color? color = null)
        {
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = size;
            label.alignment = alignment;
            label.color = color ?? Color.white;
            label.raycastTarget = false;
            return label;
        }

        private static Button AddButton(RectTransform rect, string label, float size, Color color)
        {
            AddImage(rect, color, BuiltinSprite("UI/Skin/UISprite.psd"), true);
            Button button = rect.gameObject.AddComponent<Button>();
            AddText(Stretch("Label", rect), label, size, TextAlignmentOptions.Center);
            return button;
        }

        private static Sprite BuiltinSprite(string path) => AssetDatabase.GetBuiltinExtraResource<Sprite>(path);

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
                return;
            string parent = System.IO.Path.GetDirectoryName(folder)?.Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(folder));
        }

        private static void Set(Object target, string field, object value)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(field);
            if (property == null)
                throw new ArgumentException(target.GetType().Name + " has no serialized field " + field);
            switch (value)
            {
                case null:
                    property.objectReferenceValue = null;
                    break;
                case Object reference:
                    property.objectReferenceValue = reference;
                    break;
                case Object[] references:
                    property.arraySize = references.Length;
                    for (int i = 0; i < references.Length; i++)
                        property.GetArrayElementAtIndex(i).objectReferenceValue = references[i];
                    break;
                case string[] texts:
                    property.arraySize = texts.Length;
                    for (int i = 0; i < texts.Length; i++)
                        property.GetArrayElementAtIndex(i).stringValue = texts[i];
                    break;
                case string text:
                    property.stringValue = text;
                    break;
                case bool flag:
                    property.boolValue = flag;
                    break;
                case float number:
                    property.floatValue = number;
                    break;
                case Color color:
                    property.colorValue = color;
                    break;
                case Enum choice:
                    property.intValue = Convert.ToInt32(choice);
                    break;
                default:
                    throw new ArgumentException("Cannot set " + field + " to a " + value.GetType().Name);
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
