using UnityEditor;

namespace HDC.Ads.Editor
{
    /// <summary>The HDC menu of the Unity menu bar: turning HDC ads on and off, and editing the default configs.</summary>
    internal static class HDCAdsMenu
    {
        private const string Root = "HDC/";

        [MenuItem(Root + "Ads/Enable", false, 1)]
        private static void Enable() => HDCAdsActivation.SetEnabled(true);

        [MenuItem(Root + "Ads/Enable", true)]
        private static bool CanEnable() => !HDCAdsActivation.IsFullyEnabled;

        [MenuItem(Root + "Ads/Disable", false, 2)]
        private static void Disable() => HDCAdsActivation.SetEnabled(false);

        [MenuItem(Root + "Ads/Disable", true)]
        private static bool CanDisable() => HDCAdsActivation.IsPartlyEnabled;

        [MenuItem(Root + "Edit configs/Ads configs", false, 20)]
        private static void EditAdsConfigs() => HDCAdsConfigWindow.Open(HDCAdsConfigWindow.AdsAndroidPage);

        [MenuItem(Root + "Edit configs/Ad core Android configs", false, 21)]
        private static void EditCoreAndroidConfigs() => HDCAdsConfigWindow.Open(HDCAdsConfigWindow.CoreAndroidPage);

        [MenuItem(Root + "Edit configs/Ad core iOS configs", false, 22)]
        private static void EditCoreIosConfigs() => HDCAdsConfigWindow.Open(HDCAdsConfigWindow.CoreIosPage);

        [MenuItem(Root + "Settings asset", false, 40)]
        private static void SelectSettings()
        {
            HDCAdsSettings settings = HDCAdsSettingsAsset.LoadOrCreate();
            Selection.activeObject = settings;
            EditorGUIUtility.PingObject(settings);
        }
    }
}
