using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEditor;

namespace HDC.Tool.Editor
{
    internal static class HDCScrcpy
    {
        private const string ProcessKey = "HDC.Build.ScrcpyProcess";

        internal static string Path => HDCProcess.Find("scrcpy");

        internal static bool IsRunning => Running() != null;

        internal static string Start(string serial)
        {
            string path = Path;
            if (path == null)
                return "scrcpy is not installed. Install it with: brew install scrcpy";
            Stop();
            var environment = new Dictionary<string, string> { { "ADB", HDCAndroidDevices.AdbPath() } };
            Process process = HDCProcess.Start(path, $"--serial {serial} --always-on-top --max-size 1280 --max-fps 45 --video-bit-rate 8M --no-audio", environment);
            if (process == null)
                return "scrcpy did not start.";
            SessionState.SetInt(ProcessKey, process.Id);
            return "Screen preview started.";
        }

        internal static void Stop()
        {
            Process process = Running();
            SessionState.EraseInt(ProcessKey);
            if (process == null)
                return;
            try
            {
                process.Kill();
            }
            catch (InvalidOperationException)
            {
            }
        }

        private static Process Running()
        {
            int id = SessionState.GetInt(ProcessKey, 0);
            if (id <= 0)
                return null;
            try
            {
                Process process = Process.GetProcessById(id);
                return !process.HasExited && process.ProcessName.IndexOf("scrcpy", StringComparison.OrdinalIgnoreCase) >= 0 ? process : null;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
