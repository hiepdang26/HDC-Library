using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace HDC.Ads.Editor
{
    internal sealed class HDCAppleTeam
    {
        internal string Id = "";
        internal string Name = "";
        internal bool Free;

        internal string Title => Name.Length > 0 ? $"{Name} ({Id})" : Id;
    }

    internal static class HDCAppleTeams
    {
        private const int TimeoutMs = 10000;

        private static readonly Regex InnerDict = new Regex("<dict>((?:(?!<dict>).)*?)</dict>", RegexOptions.Singleline);
        private static readonly Regex TeamId = new Regex("<key>teamID</key>\\s*<string>([A-Z0-9]{10})</string>");
        private static readonly Regex TeamName = new Regex("<key>teamName</key>\\s*<string>([^<]*)</string>");
        private static readonly Regex FreeTeam = new Regex("<key>isFreeProvisioningTeam</key>\\s*<(true|integer>1</integer)");

        internal static List<HDCAppleTeam> List() =>
            Application.platform == RuntimePlatform.OSXEditor ? Parse(Read("/usr/bin/defaults", "export com.apple.dt.Xcode -")) : new List<HDCAppleTeam>();

        internal static List<HDCAppleTeam> Parse(string plist)
        {
            var teams = new List<HDCAppleTeam>();
            var seen = new HashSet<string>();
            foreach (Match dict in InnerDict.Matches(plist ?? ""))
            {
                Match id = TeamId.Match(dict.Groups[1].Value);
                if (!id.Success || !seen.Add(id.Groups[1].Value))
                    continue;
                Match name = TeamName.Match(dict.Groups[1].Value);
                teams.Add(new HDCAppleTeam
                {
                    Id = id.Groups[1].Value,
                    Name = name.Success ? WebUtility.HtmlDecode(name.Groups[1].Value) : "",
                    Free = FreeTeam.IsMatch(dict.Groups[1].Value),
                });
            }

            return teams;
        }

        internal static void OpenXcode()
        {
            try
            {
                Process.Start(new ProcessStartInfo("/usr/bin/open", "-b com.apple.dt.Xcode") { UseShellExecute = false, CreateNoWindow = true })?.Dispose();
            }
            catch (Exception)
            {
            }
        }

        private static string Read(string file, string arguments)
        {
            try
            {
                var info = new ProcessStartInfo(file, arguments)
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8,
                    CreateNoWindow = true,
                };
                using (Process process = Process.Start(info))
                {
                    if (process == null)
                        return "";
                    var output = process.StandardOutput.ReadToEndAsync();
                    process.StandardError.ReadToEndAsync();
                    if (!process.WaitForExit(TimeoutMs))
                    {
                        process.Kill();
                        return "";
                    }

                    return process.ExitCode == 0 ? output.Result : "";
                }
            }
            catch (Exception)
            {
                return "";
            }
        }
    }
}
