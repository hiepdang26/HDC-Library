using System;
using UnityEditor;
using UnityEngine;

namespace HDC.Tool.Editor
{
    internal static class HDCBuildState
    {
        private const string Prefix = "HDC.Build.";

        internal static HDCBuildPlatform Tab
        {
            get => Enum.TryParse(Read("Tab"), out HDCBuildPlatform tab) && Enum.IsDefined(typeof(HDCBuildPlatform), tab)
                ? tab
                : EditorUserBuildSettings.activeBuildTarget == BuildTarget.iOS ? HDCBuildPlatform.Ios : HDCBuildPlatform.Android;
            set => Write("Tab", value.ToString());
        }

        internal static string AndroidDevice
        {
            get => Read("AndroidDevice");
            set => Write("AndroidDevice", value);
        }

        internal static string BuildFolder
        {
            get => Read("Folder");
            set => Write("Folder", value);
        }

        internal static HDCAppleTarget IosTarget
        {
            get => Enum.TryParse(Read("IosTarget"), out HDCAppleTarget target) && Enum.IsDefined(typeof(HDCAppleTarget), target) ? target : HDCAppleTarget.Phone;
            set => Write("IosTarget", value.ToString());
        }

        internal static string IosSimulator
        {
            get => Read("IosSimulator");
            set => Write("IosSimulator", value);
        }

        internal static string IosDevice
        {
            get => Read("IosDevice");
            set => Write("IosDevice", value);
        }

        internal static HDCBuildPreset Preset(HDCBuildPlatform platform) =>
            Enum.TryParse(Read("Preset." + platform), out HDCBuildPreset preset) && Enum.IsDefined(typeof(HDCBuildPreset), preset)
                ? preset
                : HDCBuildPreset.TestDebug;

        internal static void SetPreset(HDCBuildPlatform platform, HDCBuildPreset preset) => Write("Preset." + platform, preset.ToString());

        internal static HDCBuildResult LastResult(HDCBuildPlatform platform)
        {
            string json = SessionState.GetString(Prefix + "Last." + platform, "");
            if (json.Length == 0)
                return null;
            try
            {
                return JsonUtility.FromJson<HDCBuildResult>(json);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        internal static void SetLastResult(HDCBuildResult result)
        {
            if (result != null)
                SessionState.SetString(Prefix + "Last." + result.platform, JsonUtility.ToJson(result));
        }

        private static string Read(string key) => (EditorUserSettings.GetConfigValue(Prefix + key) ?? "").Trim();

        private static void Write(string key, string value)
        {
            if ((value ?? "") != Read(key))
                EditorUserSettings.SetConfigValue(Prefix + key, value ?? "");
        }
    }
}
