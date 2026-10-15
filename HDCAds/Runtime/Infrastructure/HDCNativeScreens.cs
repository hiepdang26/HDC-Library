using System;
using System.Globalization;
using HDC.Ads.Domain;
using UnityEngine;
#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace HDC.Ads.Infrastructure
{
    internal static class HDCNativeScreens
    {
        internal const float GmaEditorCanvasHeight = 1080f;
        private const int AndroidContentViewId = 0x01020002;

        private static int measuredWidth = -1;
        private static int measuredHeight = -1;
        private static Rect measuredSafeArea;
        private static HDCNativeScreen measured;

        internal static HDCNativeScreen Current
        {
            get
            {
                Rect safeArea = Screen.safeArea;
                if (Screen.width != measuredWidth || Screen.height != measuredHeight || safeArea != measuredSafeArea)
                {
                    measuredWidth = Screen.width;
                    measuredHeight = Screen.height;
                    measuredSafeArea = safeArea;
                    measured = Measure(new Vector2(measuredWidth, measuredHeight));
                }

                return measured;
            }
        }

        internal static void ResetStatics() => measuredWidth = -1;

        internal static HDCNativeScreen EditorPlaceholder(Vector2 unityPixels) =>
            HDCNativeScreen.Scaled(unityPixels, unityPixels.y / GmaEditorCanvasHeight, Vector2.zero);

        internal static HDCNativeScreen? FromIos(Vector2 unityPixels, string viewMetrics)
        {
            string[] parts = (viewMetrics ?? string.Empty).Split('|');
            if (parts.Length < 4)
                return null;

            var numbers = new float[4];
            for (int i = 0; i < numbers.Length; i++)
            {
                if (!float.TryParse(parts[i], NumberStyles.Float, CultureInfo.InvariantCulture, out numbers[i]))
                    return null;
            }

            var screen = new HDCNativeScreen(unityPixels, new Vector2(numbers[0], numbers[1]), new Vector2(numbers[2], numbers[3]));
            return screen.IsValid ? screen : (HDCNativeScreen?)null;
        }

        internal static HDCNativeScreen? FromAndroid(Vector2 unityPixels, Vector2 contentPixels, float density)
        {
            if (density <= 0f)
                return null;
            var screen = new HDCNativeScreen(unityPixels, contentPixels / density, Vector2.zero);
            return screen.IsValid ? screen : (HDCNativeScreen?)null;
        }

        private static HDCNativeScreen Measure(Vector2 unityPixels)
        {
            HDCNativeScreen? native = null;
            try
            {
                native = MeasureNative(unityPixels);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[HDCAds] could not measure the screen of native ad views, using the device scale: " + exception.Message);
            }

            return native ?? Fallback(unityPixels);
        }

        private static HDCNativeScreen Fallback(Vector2 unityPixels)
        {
#if UNITY_IOS && !UNITY_EDITOR
            Rect safeArea = Screen.safeArea;
            var origin = new Vector2(safeArea.xMin, unityPixels.y - safeArea.yMax);
#else
            Vector2 origin = Vector2.zero;
#endif
            return HDCNativeScreen.Scaled(unityPixels, HDCAdsSdk.PixelsPerPoint, origin);
        }

#if UNITY_EDITOR
        private static HDCNativeScreen? MeasureNative(Vector2 unityPixels) =>
            unityPixels.x > 0f && unityPixels.y > 0f ? EditorPlaceholder(unityPixels) : (HDCNativeScreen?)null;
#elif UNITY_IOS
        [DllImport("__Internal")]
        private static extern string HDCAds_ViewMetrics();

        private static HDCNativeScreen? MeasureNative(Vector2 unityPixels) => FromIos(unityPixels, HDCAds_ViewMetrics());
#elif UNITY_ANDROID
        private static HDCNativeScreen? MeasureNative(Vector2 unityPixels)
        {
            using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (AndroidJavaObject activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
            using (AndroidJavaObject window = activity?.Call<AndroidJavaObject>("getWindow"))
            using (AndroidJavaObject decor = window?.Call<AndroidJavaObject>("getDecorView"))
            using (AndroidJavaObject content = decor?.Call<AndroidJavaObject>("findViewById", AndroidContentViewId))
            using (var resources = new AndroidJavaClass("android.content.res.Resources"))
            using (AndroidJavaObject system = resources.CallStatic<AndroidJavaObject>("getSystem"))
            using (AndroidJavaObject metrics = system?.Call<AndroidJavaObject>("getDisplayMetrics"))
            {
                if (content == null || metrics == null)
                    return null;
                var contentPixels = new Vector2(content.Call<int>("getWidth"), content.Call<int>("getHeight"));
                return FromAndroid(unityPixels, contentPixels, metrics.Get<float>("density"));
            }
        }
#else
        private static HDCNativeScreen? MeasureNative(Vector2 unityPixels) => null;
#endif
    }
}
