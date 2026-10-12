using System;
using System.IO;
using System.Security;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace HDC.Tool.Editor
{
    internal sealed class HDCAppName : IPostprocessBuildWithReport, IPostGenerateGradleAndroidProject
    {
        private static readonly Regex IosName = new Regex(@"(<key>CFBundleDisplayName</key>\s*<string>)[^<]*(</string>)");
        private static readonly Regex AndroidName = new Regex("(<string name=\"app_name\">)[^<]*(</string>)");

        public int callbackOrder => 900;

        public void OnPostprocessBuild(BuildReport report)
        {
            string name = Wanted();
            if (report.summary.platform != BuildTarget.iOS || name == null)
                return;
            string plist = Path.Combine(report.summary.outputPath, "Info.plist");
            if (File.Exists(plist))
                File.WriteAllText(plist, WithIosName(File.ReadAllText(plist), name));
        }

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            string name = Wanted();
            if (name == null)
                return;
            string strings = Path.Combine(path, "..", "launcher", "src", "main", "res", "values", "strings.xml");
            if (File.Exists(strings))
                File.WriteAllText(strings, WithAndroidName(File.ReadAllText(strings), name));
        }

        internal static string Wanted()
        {
            string name = HDCBuildSettings.Find()?.AppName?.Trim();
            return string.IsNullOrEmpty(name) ? null : name;
        }

        internal static string WithIosName(string plist, string name)
        {
            string value = SecurityElement.Escape(name);
            if (IosName.IsMatch(plist))
                return IosName.Replace(plist, match => match.Groups[1].Value + value + match.Groups[2].Value, 1);
            int dict = plist.IndexOf("<dict>", StringComparison.Ordinal);
            return dict < 0 ? plist : plist.Insert(dict + "<dict>".Length, "\n\t<key>CFBundleDisplayName</key>\n\t<string>" + value + "</string>");
        }

        internal static string WithAndroidName(string resources, string name)
        {
            string value = AndroidText(name);
            return AndroidName.IsMatch(resources) ? AndroidName.Replace(resources, match => match.Groups[1].Value + value + match.Groups[2].Value, 1) : resources;
        }

        internal static string AndroidText(string name)
        {
            string value = name.Replace("\\", "\\\\").Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;")
                .Replace("'", "\\'").Replace("\"", "\\\"");
            return value.StartsWith("@", StringComparison.Ordinal) || value.StartsWith("?", StringComparison.Ordinal) ? "\\" + value : value;
        }
    }
}
