using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEngine;

namespace HDC.Tool.Editor
{
    internal static class HDCProcess
    {
        private static readonly string[] ExtraFolders = { "/opt/homebrew/bin", "/usr/local/bin" };

        internal sealed class Output
        {
            internal int ExitCode = -1;
            internal string Text = "";
            internal string Error = "";

            internal bool Ok => ExitCode == 0;

            internal string All => (Text + "\n" + Error).Trim();
        }

        internal static Output Run(string file, string arguments, int timeoutMs = 60000, IDictionary<string, string> environment = null)
        {
            var output = new Output();
            var text = new StringBuilder();
            var error = new StringBuilder();
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
                AddEnvironment(info, environment);
                using (var process = new Process { StartInfo = info })
                {
                    process.OutputDataReceived += (_, line) => Append(text, line.Data);
                    process.ErrorDataReceived += (_, line) => Append(error, line.Data);
                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    if (!process.WaitForExit(timeoutMs))
                    {
                        Kill(process);
                        output.Error = $"{Path.GetFileName(file)} did not finish within {timeoutMs / 1000} s.";
                        return output;
                    }

                    process.WaitForExit();
                    output.ExitCode = process.ExitCode;
                }
            }
            catch (Exception exception)
            {
                output.Error = exception.Message;
                return output;
            }

            lock (text)
                output.Text = text.ToString().Trim();
            lock (error)
                output.Error = error.ToString().Trim();
            return output;
        }

        internal static Process Start(string file, string arguments, IDictionary<string, string> environment = null)
        {
            try
            {
                var info = new ProcessStartInfo(file, arguments) { UseShellExecute = false, CreateNoWindow = true };
                AddEnvironment(info, environment);
                return Process.Start(info);
            }
            catch (Exception exception)
            {
                UnityEngine.Debug.LogWarning("[HDCTool] Could not start " + file + ": " + exception.Message);
                return null;
            }
        }

        internal static string Find(string name)
        {
            string executable = Application.platform == RuntimePlatform.WindowsEditor && !name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? name + ".exe" : name;
            var folders = new List<string>(ExtraFolders);
            folders.AddRange((Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator));
            foreach (string folder in folders)
            {
                if (string.IsNullOrWhiteSpace(folder))
                    continue;
                string candidate = Path.Combine(folder.Trim(), executable);
                if (File.Exists(candidate))
                    return candidate;
            }

            return null;
        }

        private static void AddEnvironment(ProcessStartInfo info, IDictionary<string, string> environment)
        {
            string path = info.EnvironmentVariables["PATH"] ?? "";
            foreach (string folder in ExtraFolders)
            {
                if (!path.Contains(folder))
                    path = folder + Path.PathSeparator + path;
            }

            info.EnvironmentVariables["PATH"] = path;
            if (environment == null)
                return;
            foreach (KeyValuePair<string, string> pair in environment)
                info.EnvironmentVariables[pair.Key] = pair.Value;
        }

        private static void Append(StringBuilder builder, string line)
        {
            if (line == null)
                return;
            lock (builder)
                builder.AppendLine(line);
        }

        private static void Kill(Process process)
        {
            try
            {
                process.Kill();
            }
            catch (InvalidOperationException)
            {
            }
        }
    }
}
