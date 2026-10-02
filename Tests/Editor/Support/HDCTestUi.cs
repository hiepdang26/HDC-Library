using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HDC.Ads.DebugUI;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace HDC.Ads.Tests
{
    /// <summary>Reads and drives the debug panel's uGUI.</summary>
    internal static class HDCTestUi
    {
        /// <summary>Names a folder to render the panel into while the tests run, to look at a layout change.</summary>
        private const string CaptureFolderVariable = "HDC_TEST_CAPTURE";

        internal static void Click(Transform target) => target.GetComponent<Button>().onClick.Invoke();

        internal static string TextOf(Transform target) => target.GetComponent<Text>().text;

        internal static string ButtonText(Transform button) => button.GetComponentInChildren<Text>().text;

        /// <summary>Every visible text under <paramref name="root"/>, one per line.</summary>
        internal static string AllText(Transform root) =>
            string.Join("\n", root.GetComponentsInChildren<Text>(false).Select(text => text.text));

        /// <summary>The visible children of a list whose names start with <paramref name="prefix"/>.</summary>
        internal static List<Transform> Rows(Transform list, string prefix) =>
            list.Cast<Transform>().Where(row => row.gameObject.activeSelf && row.name.StartsWith(prefix)).ToList();

        /// <summary>The first visible child of <paramref name="list"/> whose text is <paramref name="text"/>.</summary>
        internal static Transform Option(Transform list, string text) =>
            list.Cast<Transform>().First(child => child.gameObject.activeSelf && ButtonText(child) == text);

        /// <summary>The state badge of a unit in the Detail card.</summary>
        internal static string Badge(Transform unit) => TextOf(unit.Find("Top/Badge/Label"));

        /// <summary>
        /// Renders the panel in portrait 1080 x 1920 to a PNG when HDC_TEST_CAPTURE names a folder; does nothing
        /// otherwise. The panel stays in that portrait layout afterwards.
        /// </summary>
        internal static void Capture(GameObject panel, string file)
        {
            string folder = Environment.GetEnvironmentVariable(CaptureFolderVariable);
            if (string.IsNullOrEmpty(folder))
                return;

            panel.GetComponent<HDCAdsDebugPanel>().enabled = false;
            var scaler = panel.GetComponent<CanvasScaler>();
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0f;
            var safeArea = (RectTransform)panel.transform.Find("Safe Area");
            safeArea.anchorMin = Vector2.zero;
            safeArea.anchorMax = Vector2.one;
            var canvas = panel.GetComponent<Canvas>();
            var camera = new GameObject("Capture Camera").AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.25f, 0.4f, 0.3f);
            var target = new RenderTexture(1080, 1920, 24);
            camera.targetTexture = target;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)panel.transform);
            Canvas.ForceUpdateCanvases();
            camera.Render();
            RenderTexture.active = target;
            var image = new Texture2D(1080, 1920, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 1080, 1920), 0, 0);
            image.Apply();
            RenderTexture.active = null;
            Directory.CreateDirectory(folder);
            string path = Path.Combine(folder, file);
            File.WriteAllBytes(path, image.EncodeToPNG());
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            camera.targetTexture = null;
            Object.Destroy(camera.gameObject);
            Object.Destroy(target);
            Object.Destroy(image);
            panel.GetComponent<HDCAdsDebugPanel>().enabled = true;
            Debug.Log("[HDCTest] captured " + path);
        }
    }
}
