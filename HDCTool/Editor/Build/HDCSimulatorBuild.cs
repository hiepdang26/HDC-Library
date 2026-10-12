using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace HDC.Tool.Editor
{
    internal static class HDCSimulatorBuild
    {
        [PostProcessBuild(1010)]
        private static void MarkLibraries(BuildTarget target, string buildPath)
        {
            if (target != BuildTarget.iOS || PlayerSettings.iOS.sdkVersion != iOSSdkVersion.SimulatorSDK)
                return;
            List<string> marked = HDCSimulatorSupport.MarkExportForSimulator(buildPath);
            if (marked.Count > 0)
                Debug.Log("[HDCTool] " + string.Join(", ", marked) + (marked.Count == 1 ? " has" : " have") +
                          " arm64 code built for devices only. This simulator export marks that code for the simulator, so the build links.");
        }
    }
}
