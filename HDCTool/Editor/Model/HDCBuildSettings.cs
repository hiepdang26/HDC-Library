using System.IO;
using HDC.Ads.Editor;
using UnityEditor;
using UnityEngine;

namespace HDC.Tool.Editor
{
    internal sealed class HDCBuildSettings : ScriptableObject
    {
        internal const string AssetFolder = "Assets/HDCTool";
        internal const string AssetPath = AssetFolder + "/HDCBuildSettings.asset";
        internal const string NotReady = AssetPath + " cannot be loaded right now, for example while Unity compiles or imports. Nothing was changed; the Build Tool works again once Unity can load it.";

        [SerializeField] private HDCBuildRules testDebug = HDCBuildRules.Defaults(HDCBuildPreset.TestDebug);
        [SerializeField] private HDCBuildRules release = HDCBuildRules.Defaults(HDCBuildPreset.Release);
        [SerializeField] private string appName = "";
        [SerializeField] private bool iosAppendExport = true;
        [SerializeField] private bool podsFollowMinimumIos = true;
        [SerializeField, TextArea(3, 12)] private string changeLog = "";

        internal static string ProjectRoot => HDCBuildFolders.ProjectRoot;

        internal bool IosAppendExport
        {
            get => iosAppendExport;
            set => iosAppendExport = value;
        }

        internal bool PodsFollowMinimumIos
        {
            get => podsFollowMinimumIos;
            set => podsFollowMinimumIos = value;
        }

        internal string AppName
        {
            get => appName ?? "";
            set => appName = value ?? "";
        }

        internal string ChangeLog
        {
            get => changeLog ?? "";
            set => changeLog = value ?? "";
        }

        internal HDCBuildRules Rules(HDCBuildPreset preset) => preset == HDCBuildPreset.Release ? release : testDebug;

        internal void ResetRules(HDCBuildPreset preset)
        {
            if (preset == HDCBuildPreset.Release)
                release = HDCBuildRules.Defaults(preset);
            else
                testDebug = HDCBuildRules.Defaults(preset);
        }

        internal string OutputFolder(HDCBuildPlatform platform) => HDCBuildFolders.Display(OutputPath(platform));

        internal string OutputPath(HDCBuildPlatform platform) => HDCBuildFolders.For(platform);

        internal void Save()
        {
            EditorUtility.SetDirty(this);
            if (AssetDatabase.Contains(this))
                AssetDatabase.SaveAssetIfDirty(this);
        }

        internal static HDCBuildSettings Find()
        {
            var settings = AssetDatabase.LoadAssetAtPath<HDCBuildSettings>(AssetPath);
            if (settings != null)
                return settings;
            foreach (string guid in AssetDatabase.FindAssets("t:" + nameof(HDCBuildSettings)))
            {
                settings = AssetDatabase.LoadAssetAtPath<HDCBuildSettings>(AssetDatabase.GUIDToAssetPath(guid));
                if (settings != null)
                    return settings;
            }

            return null;
        }

        internal static HDCBuildSettings Load()
        {
            HDCBuildSettings settings = Find();
            return settings != null ? settings : CreateAt(AssetPath);
        }

        internal static HDCBuildSettings CreateAt(string path)
        {
            string blocker = HDCAssetFiles.WhyNotCreate(path);
            if (blocker != null)
            {
                Debug.LogWarning("[HDCTool] " + blocker);
                return null;
            }

            HDCAssetFiles.CreateFolders(Path.GetDirectoryName(path)?.Replace('\\', '/'));
            var settings = CreateInstance<HDCBuildSettings>();
            AssetDatabase.CreateAsset(settings, path);
            AssetDatabase.SaveAssets();
            return settings;
        }
    }
}
