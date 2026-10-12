using System;
using System.IO;
using System.Linq;
using HDC.Ads;
using UnityEditor;
using UnityEngine;

namespace HDC.Onboarding.Editor
{
    internal static class HDCOnboardingConfigKeys
    {
        private const string AndroidInterstitial = "ca-app-pub-3940256099942544/1033173712";
        private const string AndroidNative = "ca-app-pub-3940256099942544/2247696110";
        private const string IosInterstitial = "ca-app-pub-3940256099942544/4411468910";
        private const string IosNative = "ca-app-pub-3940256099942544/3986624511";
        private const string LegacySplashKey = "splash_config";
        private const string LegacyIntroKey = "intro_config";

        internal static string SplashConfig(string interstitialId, string nativeId, string gameScene) =>
            "{\n" +
            "  \"loadingTime\": 25,\n" +
            "  \"nativeSplashGroup\": \"native_splash\",\n" +
            "  \"nativeSplashShowTime\": 5,\n" +
            "  \"nativeSplashLoadTimeOut\": 20,\n" +
            "  \"enableAdmobLayout\": false,\n" +
            "  \"launchChannel\": {\n" +
            "    \"launchAdType\": 0,\n" +
            "    \"forceAdUnit\": {\n" +
            "      \"admobUnit\": { \"id\": \"" + interstitialId + "\" },\n" +
            "      \"nativeAdUnit\": { \"id\": \"\", \"layoutGroupName\": \"\" }\n" +
            "    },\n" +
            "    \"appOpenUnit\": { \"admobUnit\": { \"id\": \"\" } }\n" +
            "  },\n" +
            "  \"waitNativeSplashLoad\": false,\n" +
            "  \"interLoadAdTimeout\": 25,\n" +
            "  \"delayShowInterstitials\": 0.1,\n" +
            "  \"nativeAfterInterId\": \"" + nativeId + "\",\n" +
            "  \"nativeAfterLayoutGroup\": \"cls\",\n" +
            "  \"firstOpenNextScene\": \"" + HDCOnboardingPaths.IntroSceneName + "\",\n" +
            "  \"nextScene\": \"" + gameScene + "\"\n" +
            "}";

        internal static string IntroConfig(string gameScene) =>
            "{\n" +
            "  \"nextSceneName\": \"" + gameScene + "\",\n" +
            "  \"Configs\": [\n" +
            "    { \"IntroType\": 1, \"AdConfig\": { \"PreloadType\": 0, \"GroupName\": \"native_intro1\" }, \"TimeDelayPreload\": 0, " +
            "\"IntroInterval\": 5, \"IntroDescription\": \"Welcome! Every choice you make matters here.\" },\n" +
            "    { \"IntroType\": 0, \"AdConfig\": { \"PreloadType\": 0, \"GroupName\": \"native_intro2\" }, \"TimeDelayPreload\": 0, " +
            "\"IntroInterval\": 5, \"IntroDescription\": \"Complete missions to unlock new places.\" },\n" +
            "    { \"IntroType\": 5, \"AdConfig\": { \"PreloadType\": 1, \"GroupName\": \"native_intro_full\" }, \"TimeDelayPreload\": 0 }\n" +
            "  ]\n" +
            "}";

        internal static string NativeBannerConfig(string gameScene) =>
            "{\n" +
            "  \"activeSceneName\": \"" + gameScene + "\",\n" +
            "  \"bannerLoops\": [\n" +
            BannerLoop("native_banner", 2, 30, 60) + ",\n" +
            BannerLoop("native_banner_1", -1, 45, 45) + ",\n" +
            BannerLoop("native_banner_2", -1, 45, 45) + ",\n" +
            BannerLoop("native_banner_3", -1, 45, 45) + ",\n" +
            BannerLoop("native_banner_4", -1, 45, 45) + "\n" +
            "  ]\n" +
            "}";

        internal static string OnboardingConfig(string splashConfig, string introConfig) =>
            "{\n  \"splash_config\": " + Nested(splashConfig) + ",\n  \"intro_config\": " + Nested(introConfig) + "\n}";

        internal static void AddMissing()
        {
            HDCAdsSettings settings = FindSettings();
            if (settings == null)
            {
                Debug.LogWarning("[Splash] No HDCAdsSettings asset yet: open HDC > Settings asset once, then add the config keys again.");
                return;
            }

            var serialized = new SerializedObject(settings);
            SerializedProperty keys = serialized.FindProperty("customKeys");
            Selection.activeObject = settings;
            string gameScene = GameScene();
            bool addOnboarding = Find(keys, HDCOnboardingKeys.OnboardingConfig) == null;
            bool addBanner = Find(keys, HDCOnboardingKeys.FirstNativeBannerConfig) == null;
            if (!addOnboarding)
                Debug.Log("[Splash] onboarding_config is already a custom key: edit it in HDC > Edit configs > Custom keys.");
            if (!addBanner)
                Debug.Log("[Splash] first_native_banner_config is already a custom key: edit it in HDC > Edit configs > Custom keys.");
            if (addBanner)
            {
                string banner = NativeBannerConfig(gameScene);
                Add(keys, HDCOnboardingKeys.FirstNativeBannerConfig, banner, banner, "");
            }

            if (!addOnboarding)
            {
                Save(serialized, addBanner, gameScene);
                return;
            }

            SerializedProperty oldSplash = Find(keys, LegacySplashKey);
            SerializedProperty oldIntro = Find(keys, LegacyIntroKey);
            string splashAndroid = Value(oldSplash, "android") ?? SplashConfig(AndroidInterstitial, AndroidNative, gameScene);
            string splashIos = Value(oldSplash, "ios") ?? Value(oldSplash, "android") ?? SplashConfig(IosInterstitial, IosNative, gameScene);
            string introAndroid = Value(oldIntro, "android") ?? IntroConfig(gameScene);
            string introIos = Value(oldIntro, "ios") ?? introAndroid;
            string splashCountry = Value(oldSplash, "country");
            string introCountry = Value(oldIntro, "country");

            Add(keys, HDCOnboardingKeys.OnboardingConfig, OnboardingConfig(splashAndroid, introAndroid), OnboardingConfig(splashIos, introIos),
                splashCountry == null && introCountry == null ? "" : OnboardingConfig(splashCountry ?? splashAndroid, introCountry ?? introAndroid));
            Save(serialized, addBanner, gameScene);
            Debug.Log(oldSplash != null || oldIntro != null
                ? "[Splash] Added onboarding_config from the splash_config and intro_config custom keys. HDCLib reads only onboarding_config: " +
                  "remove the two old keys once Remote Config has onboarding_config."
                : "[Splash] Added onboarding_config with the old system's defaults and Google's test ad units. " +
                  "Set the game's scene and real ad units in HDC > Edit configs > Custom keys and in Remote Config.");
        }

        internal static string DeclaredValue(HDCAdsSettings settings, BuildTarget platform) =>
            DeclaredValue(settings, platform, HDCOnboardingKeys.OnboardingConfig);

        internal static string DeclaredValue(HDCAdsSettings settings, BuildTarget platform, string name)
        {
            if (settings == null)
                return null;
            SerializedProperty key = Find(new SerializedObject(settings).FindProperty("customKeys"), name);
            if (key == null)
                return null;
            string android = key.FindPropertyRelative("android").stringValue ?? "";
            string ios = key.FindPropertyRelative("ios").stringValue ?? "";
            return platform == BuildTarget.iOS && !string.IsNullOrWhiteSpace(ios) ? ios : android;
        }

        private static string BannerLoop(string group, int showCount, int seconds, int metaSeconds) =>
            "    { \"isEnabled\": true, \"groupName\": \"" + group + "\", \"timeOut\": 30, \"adShowCount\": " + showCount +
            ", \"defaultTimeConfig\": " + seconds + ", \"metaTimeConfig\": " + metaSeconds + " }";

        private static void Add(SerializedProperty keys, string name, string android, string ios, string country)
        {
            keys.arraySize++;
            SerializedProperty key = keys.GetArrayElementAtIndex(keys.arraySize - 1);
            key.FindPropertyRelative("key").stringValue = name;
            key.FindPropertyRelative("android").stringValue = android;
            key.FindPropertyRelative("ios").stringValue = ios;
            key.FindPropertyRelative("country").stringValue = country;
        }

        private static void Save(SerializedObject serialized, bool addedBanner, string gameScene)
        {
            serialized.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
            if (addedBanner)
                Debug.Log("[Splash] Added first_native_banner_config with the old system's loops (native_banner, then native_banner_1 to " +
                          "native_banner_4), starting in scene '" + gameScene + "'. The ads config needs these popup groups and positions.");
        }

        private static string Nested(string json) => (string.IsNullOrWhiteSpace(json) ? "{}" : json.Trim()).Replace("\n", "\n  ");

        private static SerializedProperty Find(SerializedProperty keys, string name)
        {
            for (int i = 0; i < keys.arraySize; i++)
            {
                SerializedProperty key = keys.GetArrayElementAtIndex(i);
                if (key.FindPropertyRelative("key").stringValue == name)
                    return key;
            }

            return null;
        }

        private static string Value(SerializedProperty key, string field)
        {
            string value = key?.FindPropertyRelative(field).stringValue;
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        internal static HDCAdsSettings FindSettings()
        {
            HDCAdsSettings found = null;
            foreach (string guid in AssetDatabase.FindAssets("t:" + nameof(HDCAdsSettings)))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var settings = AssetDatabase.LoadAssetAtPath<HDCAdsSettings>(path);
                if (settings == null)
                    continue;
                if (path.EndsWith("/Resources/" + HDCAdsSettings.ResourceName + ".asset", StringComparison.Ordinal))
                    return settings;
                if (found == null)
                    found = settings;
            }

            return found;
        }

        private static string GameScene()
        {
            string root = HDCOnboardingPaths.Root + "/";
            return EditorBuildSettings.scenes
                .Where(scene => scene.enabled && !scene.path.Replace('\\', '/').StartsWith(root, StringComparison.Ordinal))
                .Select(scene => Path.GetFileNameWithoutExtension(scene.path))
                .FirstOrDefault(name => name != HDCOnboardingPaths.SplashSceneName && name != HDCOnboardingPaths.IntroSceneName
                                        && !name.StartsWith("HDCAdsTest", StringComparison.Ordinal)) ?? "";
        }
    }
}
