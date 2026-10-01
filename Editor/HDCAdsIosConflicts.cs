#if UNITY_IOS
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor.iOS.Xcode;
using UnityEngine;

namespace HDC.Ads.Editor
{
    /// <summary>
    /// Keeps the iOS build linking when the project also ships another framework built from the AdsMultiplatform
    /// KMP project, such as the one of an older ads library. Two such static frameworks hold the same Compose
    /// UIKit classes and native bridge, so linking both fails on duplicate symbols. While HDC ads are on, the other
    /// framework is left out of the Xcode project, with the native sources that import it, and its copy is deleted
    /// from the export, so no build script copies its resources over those of HDCAds. The C functions of those
    /// sources that the IL2CPP code calls get stand-ins that do nothing, so the scripts calling them still link.
    /// The build log names what was left out.
    /// </summary>
    internal static class HDCAdsIosConflicts
    {
        internal const string StandInsPath = "Libraries/HDCAds/HDCAdsStandIns.mm";

        // A function of the KMP project's native bridge: every framework built from the project defines it.
        private static readonly byte[] BridgeSymbol = Encoding.ASCII.GetBytes("AdsMultiplatformUnityPause");
        private static readonly string[] SourceExtensions = { ".m", ".mm", ".c", ".cpp", ".swift" };
        private static readonly string[] Il2CppOutputFolders = { "Il2CppOutputProject/Source/il2cppOutput", "Classes/Native" };
        private static readonly Regex ExternFunction =
            new Regex(@"^IL2CPP_EXTERN_C\s+(?<type>[\w\s\*]+?)\s*DEFAULT_CALL\s+(?<name>\w+)\s*\(", RegexOptions.Compiled);
        private static readonly HashSet<string> ScalarTypes = new HashSet<string>
        {
            "bool", "int8_t", "uint8_t", "int16_t", "uint16_t", "int32_t", "uint32_t", "int64_t", "uint64_t",
            "intptr_t", "uintptr_t", "float", "double",
        };

        /// <summary>Leaves the conflicting frameworks and the sources importing them out, and writes the stand-ins.</summary>
        internal static void LeaveOut(PBXProject project, string targetGuid, string buildPath, string ownFrameworkPath)
        {
            var frameworks = new List<string>();
            // Unity puts plugin frameworks in Frameworks, or in Libraries in some versions.
            foreach (string bundle in Bundles(Path.Combine(buildPath, "Frameworks")).Concat(Bundles(Path.Combine(buildPath, "Libraries"))).ToList())
            {
                string projectPath = ProjectPath(buildPath, bundle);
                if (projectPath == ownFrameworkPath || !IsBuiltFromKmpProject(bundle))
                    continue;
                Remove(project, projectPath);
                Directory.Delete(bundle, true);
                frameworks.Add(Path.GetFileName(bundle));
            }

            var sourceNames = new List<string>();
            var sourceTexts = new List<string>();
            if (frameworks.Count > 0)
            {
                foreach (string source in Sources(Path.Combine(buildPath, "Libraries")))
                {
                    string text = File.ReadAllText(source);
                    if (!frameworks.Any(framework => Imports(text, Path.GetFileNameWithoutExtension(framework))))
                        continue;
                    Remove(project, ProjectPath(buildPath, source));
                    sourceNames.Add(Path.GetFileName(source));
                    sourceTexts.Add(text);
                }
            }

            var missing = new List<string>();
            List<string> standIns = StandIns(buildPath, sourceTexts, missing);
            if (standIns.Count > 0)
                WriteStandIns(project, targetGuid, buildPath, sourceNames, standIns);
            else
                RemoveStandIns(project, buildPath);

            if (frameworks.Count == 0)
                return;
            Debug.LogWarning($"[HDCAds] {string.Join(", ", frameworks)} cannot link next to HDCAds, as it is built from the same KMP project, " +
                             "so this iOS build leaves it out of the Xcode project and its folder" +
                             (sourceNames.Count > 0 ? $", with the sources that import it: {string.Join(", ", sourceNames)}" : "") +
                             $". {standIns.Count} C functions of those sources do nothing in this build. " +
                             "To build with it instead, turn HDC ads off (HDC > Ads > Disable) and export with Replace.");
            if (missing.Count > 0)
                Debug.LogError($"[HDCAds] No stand-in for {string.Join(", ", missing)}: the iOS link will miss them.");
        }

        /// <summary>Takes the stand-ins out of the project. Returns whether they were in it.</summary>
        internal static bool RemoveStandIns(PBXProject project, string buildPath)
        {
            string guid = project.FindFileGuidByProjectPath(StandInsPath);
            if (guid != null)
                project.RemoveFile(guid);
            string file = Path.Combine(buildPath, StandInsPath);
            if (File.Exists(file))
                File.Delete(file);
            return guid != null;
        }

        // The .xcframework and .framework bundles in a folder, without looking inside them.
        private static IEnumerable<string> Bundles(string folder)
        {
            if (!Directory.Exists(folder))
                yield break;
            foreach (string directory in Directory.GetDirectories(folder))
            {
                if (directory.EndsWith(".xcframework", StringComparison.Ordinal) || directory.EndsWith(".framework", StringComparison.Ordinal))
                {
                    yield return directory;
                    continue;
                }

                foreach (string bundle in Bundles(directory))
                    yield return bundle;
            }
        }

        private static IEnumerable<string> Sources(string folder) =>
            Directory.Exists(folder)
                ? Directory.GetFiles(folder, "*", SearchOption.AllDirectories).Where(file => SourceExtensions.Contains(Path.GetExtension(file)))
                : Enumerable.Empty<string>();

        private static bool IsBuiltFromKmpProject(string bundle)
        {
            string name = Path.GetFileNameWithoutExtension(bundle);
            string binary = Directory.GetFiles(bundle, name, SearchOption.AllDirectories).FirstOrDefault();
            return binary != null && Contains(binary, BridgeSymbol);
        }

        private static bool Imports(string source, string module)
        {
            string name = Regex.Escape(module);
            return Regex.IsMatch(source, @"^\s*(#\s*(import|include)\s*[<""]" + name + @"/|@import\s+" + name + @"\b|import\s+" + name + @"\s*$)", RegexOptions.Multiline);
        }

        private static void Remove(PBXProject project, string projectPath)
        {
            string guid = project.FindFileGuidByProjectPath(projectPath);
            if (guid != null)
                project.RemoveFile(guid);
        }

        // Stand-ins for the functions that the IL2CPP code declares and the left-out sources define.
        private static List<string> StandIns(string buildPath, List<string> sources, List<string> missing)
        {
            var standIns = new List<string>();
            if (sources.Count == 0)
                return standIns;

            var seen = new HashSet<string>();
            foreach (string folder in Il2CppOutputFolders)
            {
                string path = Path.Combine(buildPath, folder);
                if (!Directory.Exists(path))
                    continue;
                foreach (string file in Directory.GetFiles(path, "*.cpp"))
                {
                    foreach (string line in File.ReadLines(file))
                    {
                        if (!line.StartsWith("IL2CPP_EXTERN_C", StringComparison.Ordinal))
                            continue;
                        Match match = ExternFunction.Match(line);
                        string name = match.Groups["name"].Value;
                        if (!match.Success || !seen.Add(name) || !sources.Any(source => Defines(source, name)))
                            continue;

                        string standIn = StandIn(match.Groups["type"].Value, name);
                        if (standIn != null)
                            standIns.Add(standIn);
                        else
                            missing.Add(name);
                    }
                }
            }

            standIns.Sort(StringComparer.Ordinal);
            return standIns;
        }

        private static bool Defines(string source, string name) =>
            source.Contains(name) && Regex.IsMatch(source, @"\b" + Regex.Escape(name) + @"\s*\([^;{}]*\)\s*\{");

        // A function that takes any arguments and returns zero: the callers' argument types do not matter.
        private static string StandIn(string returnType, string name)
        {
            string type = Regex.Replace(returnType.Trim(), @"\s+", " ");
            if (type == "void")
                return $"void {name}(...) {{}}";
            if (type.EndsWith("*", StringComparison.Ordinal))
                return $"void* {name}(...) {{ return 0; }}";
            return ScalarTypes.Contains(type) ? $"{type} {name}(...) {{ return 0; }}" : null;
        }

        private static void WriteStandIns(PBXProject project, string targetGuid, string buildPath, List<string> sources, List<string> standIns)
        {
            var text = new StringBuilder()
                .AppendLine("// Written by HDCLib for this build. These sources were left out, as they import a framework built from")
                .AppendLine("// the same KMP project as HDCAds, which cannot link next to it:");
            foreach (string source in sources)
                text.AppendLine("//   " + source);
            text.AppendLine("// The functions below stand in for their C functions that scripts call, and do nothing.")
                .AppendLine("#include <stdint.h>")
                .AppendLine()
                .AppendLine("extern \"C\" {");
            foreach (string standIn in standIns)
                text.AppendLine(standIn);
            text.AppendLine("}");

            string file = Path.Combine(buildPath, StandInsPath);
            Directory.CreateDirectory(Path.GetDirectoryName(file) ?? buildPath);
            File.WriteAllText(file, text.ToString());
            string guid = project.FindFileGuidByProjectPath(StandInsPath) ?? project.AddFile(StandInsPath, StandInsPath, PBXSourceTree.Source);
            project.AddFileToBuild(targetGuid, guid);
        }

        private static string ProjectPath(string buildPath, string path)
        {
            string root = Path.GetFullPath(buildPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return Path.GetFullPath(path).Substring(root.Length + 1).Replace('\\', '/');
        }

        private static bool Contains(string path, byte[] pattern)
        {
            var buffer = new byte[1 << 20];
            int kept = 0;
            using (FileStream stream = File.OpenRead(path))
            {
                int read;
                while ((read = stream.Read(buffer, kept, buffer.Length - kept)) > 0)
                {
                    int length = kept + read;
                    if (IndexOf(buffer, length, pattern) >= 0)
                        return true;

                    // Keep the tail, in case the pattern spans two reads.
                    kept = Math.Min(pattern.Length - 1, length);
                    Buffer.BlockCopy(buffer, length - kept, buffer, 0, kept);
                }
            }

            return false;
        }

        private static int IndexOf(byte[] buffer, int length, byte[] pattern)
        {
            int last = length - pattern.Length;
            for (int i = Array.IndexOf(buffer, pattern[0], 0, length); i >= 0 && i <= last; i = Array.IndexOf(buffer, pattern[0], i + 1, length - i - 1))
            {
                int j = 1;
                while (j < pattern.Length && buffer[i + j] == pattern[j])
                    j++;
                if (j == pattern.Length)
                    return i;
            }

            return -1;
        }
    }
}
#endif
