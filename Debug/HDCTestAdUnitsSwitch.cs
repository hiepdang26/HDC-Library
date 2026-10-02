using UnityEngine;

namespace HDC.Ads.DebugUI
{
    internal static class HDCTestAdUnitsSwitch
    {
        private const string Key = "HDCAds.Debug.TestAdUnits";

        internal static bool IsSaved
        {
            get => PlayerPrefs.GetInt(Key, 0) == 1;
            set
            {
                if (value)
                    PlayerPrefs.SetInt(Key, 1);
                else
                    PlayerPrefs.DeleteKey(Key);
                PlayerPrefs.Save();
            }
        }

        internal static bool ChangedThisSession { get; private set; }

        internal static void Toggle()
        {
            bool on = !HDCAds.Testing.UseTestAdUnits;
            HDCAds.Testing.UseTestAdUnits = on;
            IsSaved = on;
            ChangedThisSession = true;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ApplySaved()
        {
            ChangedThisSession = false;
            if (IsSaved)
                HDCAds.Testing.UseTestAdUnits = true;
        }
    }
}
