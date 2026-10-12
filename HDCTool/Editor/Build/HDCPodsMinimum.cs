using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace HDC.Tool.Editor
{
    internal static class HDCPodsMinimum
    {
        internal const string Variable = "hdc_minimum_ios";

        private static readonly Regex PostInstall = new Regex(@"^(?<indent>[ \t]*)post_install[ \t]+do[ \t]*\|[ \t]*(?<installer>\w+)[ \t]*\|[ \t]*(?=\r?$)", RegexOptions.Multiline);
        private static readonly Regex Assignment = new Regex(@"^(?<indent>[ \t]*)hdc_minimum_ios = '[^']*'", RegexOptions.Multiline);
        private static readonly Regex Platform = new Regex(@"^[ \t]*platform[ \t]+:ios[ \t]*,[ \t]*['""](?<version>[0-9.]+)['""]", RegexOptions.Multiline);

        [PostProcessBuild(46)]
        private static void AddHook(BuildTarget target, string buildPath)
        {
            if (target != BuildTarget.iOS)
                return;
            HDCBuildSettings settings = HDCBuildSettings.Find();
            string podfile = Path.Combine(buildPath, "Podfile");
            if (settings == null || !settings.PodsFollowMinimumIos || !File.Exists(podfile))
                return;
            string text = File.ReadAllText(podfile);
            string minimum = Minimum(PlayerSettings.iOS.targetOSVersionString, text);
            File.WriteAllText(podfile, WithHook(text, minimum));
            Debug.Log($"[HDCTool] The Podfile raises every pod below iOS {minimum} to {minimum} after pod install.");
        }

        internal static string Minimum(string playerMinimum, string podfile)
        {
            Match platform = Platform.Match(podfile ?? "");
            string declared = platform.Success ? platform.Groups["version"].Value : "";
            return HDCSdkVersions.Compare(declared, playerMinimum) > 0 ? declared : (playerMinimum ?? "").Trim();
        }

        internal static string WithHook(string podfile, string minimum)
        {
            string text = podfile ?? "";
            Match assignment = Assignment.Match(text);
            if (assignment.Success)
                return text.Substring(0, assignment.Index) + assignment.Groups["indent"].Value + $"{Variable} = '{minimum}'" + text.Substring(assignment.Index + assignment.Length);

            Match hook = PostInstall.Match(text);
            if (hook.Success)
            {
                int insert = hook.Index + hook.Length;
                return text.Substring(0, insert) + "\n" + Lines(hook.Groups["installer"].Value, minimum, hook.Groups["indent"].Value + "  ") + text.Substring(insert);
            }

            string separator = text.Length == 0 || text.EndsWith("\n", StringComparison.Ordinal) ? "" : "\n";
            return text + separator + "\npost_install do |installer|\n" + Lines("installer", minimum, "  ") + "\nend\n";
        }

        private static string Lines(string installer, string minimum, string indent) =>
            string.Join("\n", new[]
            {
                $"{Variable} = '{minimum}'",
                "hdc_raise_minimum = lambda do |config|",
                "  current = config.build_settings['IPHONEOS_DEPLOYMENT_TARGET'].to_s",
                $"  if current !~ /\\A\\d+(\\.\\d+)*\\z/ || Gem::Version.new(current) < Gem::Version.new({Variable})",
                $"    config.build_settings['IPHONEOS_DEPLOYMENT_TARGET'] = {Variable}",
                "  end",
                "end",
                $"{installer}.pods_project.build_configurations.each {{ |config| hdc_raise_minimum.call(config) }}",
                $"{installer}.pods_project.targets.each {{ |pod_target| pod_target.build_configurations.each {{ |config| hdc_raise_minimum.call(config) }} }}",
            }.Select(line => indent + line));
    }
}
