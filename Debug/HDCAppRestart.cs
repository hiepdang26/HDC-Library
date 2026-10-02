using UnityEngine;

namespace HDC.Ads.DebugUI
{
    internal static class HDCAppRestart
    {
        internal static bool Relaunches => Application.isEditor || Application.platform == RuntimePlatform.Android;

        internal static void Restart()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            UnityEditor.EditorApplication.delayCall += EnterPlayMode;
#elif UNITY_ANDROID
            RelaunchAndroid();
#else
            Application.Quit();
#endif
        }

#if UNITY_EDITOR
        private static void EnterPlayMode()
        {
            if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
            {
                UnityEditor.EditorApplication.delayCall += EnterPlayMode;
                return;
            }

            UnityEditor.EditorApplication.isPlaying = true;
        }
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
        private static void RelaunchAndroid()
        {
            using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (AndroidJavaObject activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
            using (AndroidJavaObject packages = activity.Call<AndroidJavaObject>("getPackageManager"))
            using (AndroidJavaObject launch = packages.Call<AndroidJavaObject>("getLaunchIntentForPackage", activity.Call<string>("getPackageName")))
            using (AndroidJavaObject component = launch.Call<AndroidJavaObject>("getComponent"))
            using (var intents = new AndroidJavaClass("android.content.Intent"))
            using (AndroidJavaObject restart = intents.CallStatic<AndroidJavaObject>("makeRestartActivityTask", component))
            {
                activity.Call("startActivity", restart);
            }

            using (var runtime = new AndroidJavaClass("java.lang.Runtime"))
            using (AndroidJavaObject current = runtime.CallStatic<AndroidJavaObject>("getRuntime"))
            {
                current.Call("exit", 0);
            }
        }
#endif
    }
}
