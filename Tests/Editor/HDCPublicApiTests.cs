using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using NUnit.Framework;

namespace HDC.Ads.Tests
{
    public class HDCPublicApiTests
    {
        private const string ApprovedFile = "PublicApi.txt";
        private const string AcceptVariable = "HDC_ACCEPT_API";
        private const int ListedChanges = 40;

        private static readonly string[] AssemblyNames = { "HDC.Ads", "HDC.Ads.Settings", "HDC.Ads.Firebase", "HDC.Ads.Setup", "HDC.Ads.Debug" };

        private const string FileHeader =
            "# The public API of HDCLib's runtime assemblies: what game code can compile against.\n" +
            "# HDCPublicApiTests compares the assemblies with this file. A public type or member missing here fails the\n" +
            "# test, and so does one listed here that is gone. Update this file in the same commit as the API change, by\n" +
            "# hand or by running the tests once with " + AcceptVariable + "=1.\n" +
            "# One section per assembly; members sit under their type, indented.\n";

        [Test]
        public void PublicApiMatchesTheApprovedList()
        {
            string approvedPath = Path.GetFullPath(HDCTestPaths.Tests + "/" + ApprovedFile);
            Dictionary<string, List<string>> approved = File.Exists(approvedPath)
                ? Sections(File.ReadAllLines(approvedPath))
                : new Dictionary<string, List<string>>();
            Dictionary<string, Assembly> loaded = AppDomain.CurrentDomain.GetAssemblies()
                .Where(assembly => AssemblyNames.Contains(assembly.GetName().Name))
                .ToDictionary(assembly => assembly.GetName().Name);
            Assert.IsTrue(loaded.ContainsKey("HDC.Ads"), "HDC.Ads is not loaded");

            var current = new Dictionary<string, List<string>>();
            foreach (string name in AssemblyNames)
            {
                if (loaded.TryGetValue(name, out Assembly assembly))
                    current[name] = HDCApiListing.Lines(assembly).ToList();
                else if (approved.TryGetValue(name, out List<string> kept))
                    current[name] = kept;
            }

            string text = Write(current);
            if (Environment.GetEnvironmentVariable(AcceptVariable) == "1")
            {
                File.WriteAllText(approvedPath, text);
                Assert.Pass("Wrote the current API to " + approvedPath);
            }

            Assert.IsTrue(File.Exists(approvedPath), $"There is no {ApprovedFile}: run the tests once with {AcceptVariable}=1 to write it.");
            var added = new List<string>();
            var removed = new List<string>();
            foreach (string name in AssemblyNames)
            {
                HashSet<string> now = Keys(current.TryGetValue(name, out List<string> lines) ? lines : new List<string>());
                HashSet<string> before = Keys(approved.TryGetValue(name, out List<string> approvedLines) ? approvedLines : new List<string>());
                added.AddRange(now.Except(before).OrderBy(key => key, StringComparer.Ordinal).Select(key => name + ": " + key));
                removed.AddRange(before.Except(now).OrderBy(key => key, StringComparer.Ordinal).Select(key => name + ": " + key));
            }

            if (added.Count == 0 && removed.Count == 0)
                return;
            string received = Path.Combine(Path.GetTempPath(), "HDC.Ads.Tests", "PublicApi.received.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(received));
            File.WriteAllText(received, text);

            var message = new StringBuilder();
            message.AppendLine($"The public API changed: {added.Count} added, {removed.Count} removed.");
            Append(message, "Added (public now, not in the list):", added);
            Append(message, "Removed (in the list, gone now):", removed);
            message.AppendLine($"If the change is meant, update {ApprovedFile} in the same commit: run the tests once with {AcceptVariable}=1,");
            message.Append("or copy the current API from ").Append(received).Append('.');
            Assert.Fail(message.ToString());
        }

        private static void Append(StringBuilder message, string title, List<string> keys)
        {
            if (keys.Count == 0)
                return;
            message.AppendLine(title);
            foreach (string key in keys.Take(ListedChanges))
                message.Append("  ").AppendLine(key);
            if (keys.Count > ListedChanges)
                message.AppendLine($"  … and {keys.Count - ListedChanges} more");
        }

        private static string Write(Dictionary<string, List<string>> sections)
        {
            var text = new StringBuilder(FileHeader);
            foreach (string name in AssemblyNames)
            {
                if (!sections.TryGetValue(name, out List<string> lines))
                    continue;
                text.Append('\n').Append('[').Append(name).Append("]\n");
                foreach (string line in lines)
                    text.Append(line).Append('\n');
            }

            return text.ToString();
        }

        private static Dictionary<string, List<string>> Sections(IEnumerable<string> lines)
        {
            var sections = new Dictionary<string, List<string>>();
            List<string> section = null;
            foreach (string line in lines)
            {
                if (line.Length == 0 || line.StartsWith("#"))
                    continue;
                if (line.StartsWith("[") && line.EndsWith("]"))
                    sections[line.Substring(1, line.Length - 2)] = section = new List<string>();
                else
                    section?.Add(line.TrimEnd());
            }

            return sections;
        }

        private static HashSet<string> Keys(IEnumerable<string> lines)
        {
            var keys = new HashSet<string>(StringComparer.Ordinal);
            string type = "";
            foreach (string line in lines)
            {
                if (line.StartsWith(HDCApiListing.Indent))
                {
                    keys.Add(type + " → " + line.Trim());
                    continue;
                }

                int bases = line.IndexOf(" : ", StringComparison.Ordinal);
                type = bases < 0 ? line : line.Substring(0, bases);
                keys.Add(line);
            }

            return keys;
        }
    }
}
