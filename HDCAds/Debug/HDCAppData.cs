using System;
using System.Collections.Generic;
using System.IO;
#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif
using UnityEngine;

namespace HDC.Ads.DebugUI
{
    internal static class HDCAppData
    {
        private const string AndroidNativeLibraries = "lib";
        private const string AndroidPlayerPrefsSuffix = ".v2.playerprefs.xml";
        private const string IosLibrary = "Library";
        private const string IosPreferences = "Preferences";

        internal static void Clear()
        {
            bool testAdUnits = HDCTestAdUnitsSwitch.IsSaved;
            PlayerPrefs.DeleteAll();
#if UNITY_IOS && !UNITY_EDITOR
            HDCAds_ClearUserDefaults();
#endif
            HDCTestAdUnitsSwitch.IsSaved = testAdUnits;
            PlayerPrefs.Save();

            string androidData = AndroidDataFolder();
            foreach (string folder in Folders(androidData))
                Empty(folder, path => Keep(path, androidData));
        }

        internal static int Empty(string folder, Func<string, bool> keep)
        {
            if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
                return 0;

            int removed = 0;
            foreach (string entry in Directory.GetFileSystemEntries(folder))
            {
                if (keep != null && keep(entry))
                    continue;
                try
                {
                    FileAttributes attributes = File.GetAttributes(entry);
                    bool link = (attributes & FileAttributes.ReparsePoint) != 0;
                    if ((attributes & FileAttributes.Directory) != 0 && !link)
                    {
                        removed += Empty(entry, keep);
                        if (Directory.GetFileSystemEntries(entry).Length == 0)
                            Directory.Delete(entry);
                    }
                    else
                    {
                        File.Delete(entry);
                        removed++;
                    }
                }
                catch (IOException)
                {
                }
                catch (UnauthorizedAccessException)
                {
                }
            }

            return removed;
        }

        private static IEnumerable<string> Folders(string androidData)
        {
            var folders = new List<string> { Application.persistentDataPath, Application.temporaryCachePath };
            if (!string.IsNullOrEmpty(androidData))
                folders.Add(androidData);
#if UNITY_IOS && !UNITY_EDITOR
            string container = Path.GetDirectoryName(Application.persistentDataPath);
            if (!string.IsNullOrEmpty(container))
            {
                folders.Add(Path.Combine(container, IosLibrary));
                folders.Add(Path.Combine(container, "tmp"));
            }
#endif
            return folders;
        }

        private static bool Keep(string path, string androidData)
        {
            string name = Path.GetFileName(path);
            string parent = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(androidData))
                return (name == AndroidNativeLibraries && parent == androidData) || name.EndsWith(AndroidPlayerPrefsSuffix, StringComparison.Ordinal);
            return name == IosPreferences && Path.GetFileName(parent) == IosLibrary && Application.platform == RuntimePlatform.IPhonePlayer;
        }

        private static string AndroidDataFolder()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (AndroidJavaObject activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
            using (AndroidJavaObject info = activity.Call<AndroidJavaObject>("getApplicationInfo"))
            {
                return info.Get<string>("dataDir");
            }
#else
            return null;
#endif
        }

#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void HDCAds_ClearUserDefaults();
#endif
    }
}
