using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HDC.Ads;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace HDC.Onboarding.Editor
{
    internal sealed class HDCOnboardingBuildCheck : IPreprocessBuildWithReport
    {
        internal const string NotDeclared =
            "A build scene uses HDCSplash or HDCIntro, but HDCAdsSettings has no onboarding_config custom key, so the splash reads an empty " +
            "config, shows no ads and never leaves its scene. Add the key with HDC > Onboarding > Add config keys.";

        internal const string BannerNotDeclared =
            "A build scene has HDCNativeBannerLoader, but HDCAdsSettings has no first_native_banner_config custom key, so no native banner " +
            "shows. Add the key with HDC > Onboarding > Add config keys.";

        private static readonly Dictionary<Type, string> ScriptPaths = new Dictionary<Type, string>();
        private static HashSet<string> dependencies = new HashSet<string>(StringComparer.Ordinal);
        private static string dependenciesKey;
        private static bool listening;

        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            string[] scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
            Check(HDCOnboardingConfigKeys.FindSettings(), report.summary.platform, scenes);
        }

        internal static void Check(HDCAdsSettings settings, BuildTarget platform, IReadOnlyCollection<string> scenes)
        {
            List<(bool Error, string Message)> findings = Findings(settings, platform, scenes);
            foreach ((bool error, string message) in findings)
            {
                if (error)
                    throw new BuildFailedException("[Splash] " + message);
            }

            foreach ((bool Error, string Message) finding in findings)
                Debug.LogWarning("[Splash] " + finding.Message);
        }

        internal static List<(bool Error, string Message)> Findings(HDCAdsSettings settings, BuildTarget platform, IReadOnlyCollection<string> scenes,
            bool cached = false)
        {
            var findings = new List<(bool Error, string Message)>();
            HashSet<string> used = Dependencies(scenes, cached);
            bool splash = UsesScript(used, typeof(HDCSplash), cached);
            bool intro = UsesScript(used, typeof(HDCIntro), cached);
            if (splash || intro)
            {
                string value = HDCOnboardingConfigKeys.DeclaredValue(settings, platform);
                if (value == null)
                    findings.Add((true, NotDeclared));
                else
                    findings.AddRange(Problems(value, splash, intro, scenes).Select(problem => (false, problem)));
            }

            if (UsesScript(used, typeof(HDCNativeBannerLoader), cached))
            {
                string banner = HDCOnboardingConfigKeys.DeclaredValue(settings, platform, HDCOnboardingKeys.FirstNativeBannerConfig);
                findings.AddRange(BannerProblems(banner, scenes).Select(problem => (false, problem)));
            }

            return findings;
        }

        internal static IEnumerable<string> BannerProblems(string value, IEnumerable<string> scenes)
        {
            if (value == null)
            {
                yield return BannerNotDeclared;
                yield break;
            }

            if (string.IsNullOrWhiteSpace(value))
            {
                yield return "first_native_banner_config has no default for this platform, so until Remote Config sends it no native banner shows.";
                yield break;
            }

            HDCNativeBannerConfig config = HDCNativeBannerConfig.Parse(value);
            if (config?.bannerLoops == null || config.bannerLoops.Length == 0)
            {
                yield return "first_native_banner_config has no bannerLoops, so no native banner shows.";
                yield break;
            }

            if (string.IsNullOrEmpty(config.activeSceneName))
                yield return "first_native_banner_config.activeSceneName is empty, so the native banners never start.";
            else if (!SceneNames(scenes).Contains(config.activeSceneName))
                yield return $"first_native_banner_config.activeSceneName is '{config.activeSceneName}', which is not an enabled scene in Build Settings, " +
                             "so the native banners never start.";
        }

        internal static IEnumerable<string> Problems(string value, bool splash, bool intro, IEnumerable<string> scenes)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                yield return "onboarding_config has no default for this platform, so until Remote Config sends it the splash shows no ads and stays open.";
                yield break;
            }

            HDCOnboardingConfig config = HDCOnboardingConfig.Parse(value);
            HashSet<string> names = SceneNames(scenes);

            var targets = new List<(string Field, string Scene)>();
            if (splash)
            {
                targets.Add(("splash_config.firstOpenNextScene", config.splash_config.firstOpenNextScene));
                targets.Add(("splash_config.nextScene", config.splash_config.nextScene));
            }

            if (intro)
                targets.Add(("intro_config.nextSceneName", config.intro_config.nextSceneName));

            foreach ((string field, string scene) in targets)
            {
                if (string.IsNullOrEmpty(scene))
                    yield return $"onboarding_config.{field} is empty, so that scene stays open.";
                else if (!names.Contains(scene))
                    yield return $"onboarding_config.{field} is '{scene}', which is not an enabled scene in Build Settings.";
            }
        }

        private static HashSet<string> SceneNames(IEnumerable<string> scenes)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (string path in scenes)
            {
                names.Add(path);
                names.Add(Path.GetFileNameWithoutExtension(path));
            }

            return names;
        }

        internal static HashSet<string> Dependencies(IReadOnlyCollection<string> scenes, bool cached)
        {
            Listen();
            string key = string.Join("|", scenes.Select(scene => scene + "@" + (File.Exists(scene) ? File.GetLastWriteTimeUtc(scene).Ticks : 0L)));
            if (cached && key == dependenciesKey)
                return dependencies;
            string[] existing = scenes.Where(File.Exists).ToArray();
            dependencies = new HashSet<string>(existing.Length == 0 ? new string[0] : AssetDatabase.GetDependencies(existing, true), StringComparer.Ordinal);
            dependenciesKey = key;
            return dependencies;
        }

        private static void Listen()
        {
            if (listening)
                return;
            listening = true;
            EditorApplication.projectChanged += () =>
            {
                dependenciesKey = null;
                ScriptPaths.Clear();
            };
        }

        private static bool UsesScript(HashSet<string> used, Type type, bool cached)
        {
            string script = ScriptPath(type, cached);
            return !string.IsNullOrEmpty(script) && used.Contains(script);
        }

        private static string ScriptPath(Type type, bool cached)
        {
            Listen();
            if (cached && ScriptPaths.TryGetValue(type, out string known))
                return known;
            string found = AssetDatabase.FindAssets("t:MonoScript " + type.Name)
                .Select(AssetDatabase.GUIDToAssetPath)
                .FirstOrDefault(path => AssetDatabase.LoadAssetAtPath<MonoScript>(path)?.GetClass() == type);
            if (!string.IsNullOrEmpty(found))
                ScriptPaths[type] = found;
            return found;
        }
    }
}
