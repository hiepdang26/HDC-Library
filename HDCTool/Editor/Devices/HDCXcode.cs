using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;

namespace HDC.Tool.Editor
{
    internal static class HDCXcode
    {
        internal const string Scheme = "Unity-iPhone";
        internal const string NotAllowedCode = "-1743";
        private const string ScriptFile = "HDCXcode.applescript";

        internal const string Script = @"on documentIndex(workspacePath)
	tell application ""Xcode""
		repeat with i from 1 to (count of workspace documents)
			try
				set candidatePath to POSIX path of (file of item i of workspace documents as text)
				if candidatePath is workspacePath or candidatePath is (workspacePath & ""/"") then return i
			end try
		end repeat
	end tell
	return 0
end documentIndex

on fileName(workspacePath)
	set AppleScript's text item delimiters to ""/""
	set parts to text items of workspacePath
	set AppleScript's text item delimiters to """"
	repeat with k from (count of parts) to 1 by -1
		if item k of parts is not """" then return item k of parts
	end repeat
	return workspacePath
end fileName

on sameNamedElsewhere(workspacePath)
	set found to """"
	set ownName to my fileName(workspacePath)
	tell application ""Xcode""
		repeat with i from 1 to (count of workspace documents)
			try
				set candidatePath to POSIX path of (file of item i of workspace documents as text)
				if (name of item i of workspace documents) is ownName and candidatePath is not workspacePath and candidatePath is not (workspacePath & ""/"") then set found to candidatePath
			end try
		end repeat
	end tell
	return found
end sameNamedElsewhere

on resolvePackages(workspacePath)
	try
		do shell script ""/usr/bin/xcrun xcodebuild -resolvePackageDependencies -workspace "" & quoted form of workspacePath & "" -scheme "" & quoted form of """ + Scheme + @""" & "" > /dev/null 2>&1""
	end try
end resolvePackages

on openedIndex(workspacePath)
	set i to my documentIndex(workspacePath)
	if i is 0 then error ""The workspace is no longer open in Xcode: "" & workspacePath
	return i
end openedIndex

on run argv
	set workspacePath to item 1 of argv
	set deviceId to item 2 of argv
	set deviceName to item 3 of argv
	set command to item 4 of argv
	set packages to """"
	if (count of argv) > 4 then set packages to item 5 of argv
	if packages is ""resolve"" then my resolvePackages(workspacePath)
	set wasOpen to (my documentIndex(workspacePath)) > 0
	tell application ""Xcode""
		activate
		open (POSIX file workspacePath)
	end tell
	set isLoaded to false
	repeat 240 times
		set i to my documentIndex(workspacePath)
		if i > 0 then
			tell application ""Xcode"" to set isLoaded to loaded of item i of workspace documents
			if isLoaded then exit repeat
		end if
		delay 0.5
	end repeat
	if not isLoaded then error ""Xcode did not finish opening "" & workspacePath
	set other to my sameNamedElsewhere(workspacePath)
	if other is not """" then
		if command is not ""run"" then return ""opened-only|"" & other
		error ""Another workspace with the same name is open in Xcode ("" & other & ""). Xcode tells such workspaces apart only by name, so close that one first.""
	end if
	set i to my openedIndex(workspacePath)
	tell application ""Xcode""
		set active scheme of item i of workspace documents to scheme """ + Scheme + @""" of item i of workspace documents
	end tell
	if deviceId is not """" then
		set chosen to 0
		repeat 60 times
			set i to my openedIndex(workspacePath)
			tell application ""Xcode""
				set destinationCount to count of run destinations of item i of workspace documents
				repeat with k from 1 to destinationCount
					try
						set candidateDevice to device of run destination k of item i of workspace documents
						if (device identifier of candidateDevice) is deviceId then set chosen to k
						if chosen is 0 and (name of candidateDevice) is deviceName and not (generic of candidateDevice) then set chosen to k
						if chosen is 0 and deviceName is ""My Mac"" and (name of run destination k of item i of workspace documents) starts with ""My Mac"" then set chosen to k
					end try
				end repeat
			end tell
			if chosen > 0 then exit repeat
			delay 1
		end repeat
		if chosen is 0 then error ""Xcode has no run destination for "" & deviceName & "". Unlock the iPhone, trust this Mac and check Window > Devices and Simulators in Xcode.""
		tell application ""Xcode""
			set active run destination of item i of workspace documents to run destination chosen of item i of workspace documents
		end tell
	end if
	if command is not ""run"" then return ""opened|""
	if wasOpen then
		set i to my openedIndex(workspacePath)
		tell application ""Xcode""
			try
				stop item i of workspace documents
			end try
		end tell
		delay 1
	end if
	set attempts to 0
	repeat
		set i to my openedIndex(workspacePath)
		tell application ""Xcode"" to run item i of workspace documents
		set isDone to false
		repeat 3600 times
			delay 1
			set i to my openedIndex(workspacePath)
			tell application ""Xcode""
				try
					if loaded of item i of workspace documents then set isDone to completed of last scheme action result of item i of workspace documents
				end try
			end tell
			if isDone then exit repeat
		end repeat
		set i to my openedIndex(workspacePath)
		tell application ""Xcode""
			set statusText to (status of last scheme action result of item i of workspace documents) as text
			set errorText to """"
			try
				set errorText to error message of last scheme action result of item i of workspace documents
				if errorText is missing value then set errorText to """"
			end try
			try
				set errorCount to count of build errors of last scheme action result of item i of workspace documents
				repeat with k from 1 to errorCount
					set errorText to errorText & (message of build error k of last scheme action result of item i of workspace documents) & ""; ""
				end repeat
			end try
		end tell
		set attempts to attempts + 1
		if statusText is ""succeeded"" or attempts > 29 then exit repeat
		if errorText does not contain ""Missing package product"" and errorText does not contain ""Package resolution"" then exit repeat
		my resolvePackages(workspacePath)
		delay 3
	end repeat
	return statusText & ""|"" & errorText
end run
";

        private static readonly StringBuilder Output = new StringBuilder();
        private static Process process;

        internal sealed class Outcome
        {
            internal bool Ok;
            internal string Message = "";
        }

        internal static bool IsBusy => process != null && !process.HasExited;

        internal static string Activity { get; private set; } = "";

        internal static string Start(string workspace, HDCAppleDevice device, bool run)
        {
            if (IsBusy)
                return "Xcode is still busy with " + Activity + ".";
            if (string.IsNullOrEmpty(workspace))
                return "The export has no Xcode project.";
            string script = Path.Combine(HDCBuildSettings.ProjectRoot, "Temp", ScriptFile);
            Directory.CreateDirectory(Path.GetDirectoryName(script));
            File.WriteAllText(script, Script, new UTF8Encoding(false));
            lock (Output)
                Output.Clear();
            bool packages = run && UsesSwiftPackages(workspace) && !PackagesReady(workspace, DerivedData);
            var info = new ProcessStartInfo("/usr/bin/osascript", Arguments(script, workspace, device, run, packages))
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
                CreateNoWindow = true,
            };
            try
            {
                process = new Process { StartInfo = info };
                process.OutputDataReceived += (_, line) => Append(line.Data);
                process.ErrorDataReceived += (_, line) => Append(line.Data);
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
            }
            catch (Exception exception)
            {
                process = null;
                return "Could not start osascript: " + exception.Message;
            }

            string target = device?.Title ?? "the iPhone";
            Activity = !run ? "opening the project" : packages ? "getting the Swift packages ready, then building and running on " + target : "building and running on " + target;
            return null;
        }

        internal static string Arguments(string script, string workspace, HDCAppleDevice device, bool run, bool packages) =>
            $"{Quote(script)} {Quote(workspace)} {Quote(device?.Udid ?? "")} {Quote(device?.Name ?? "")} {(run ? "run" : "open")}" + (packages ? " resolve" : "");

        internal static string DerivedData => Path.Combine(Environment.GetEnvironmentVariable("HOME") ?? "", "Library", "Developer", "Xcode", "DerivedData");

        internal static bool PackagesReady(string workspace, string derivedData)
        {
            if (string.IsNullOrEmpty(workspace) || !Directory.Exists(derivedData))
                return false;
            string wanted = "<string>" + SecurityElement.Escape(workspace.TrimEnd('/')) + "</string>";
            foreach (string folder in Directory.GetDirectories(derivedData))
            {
                string info = Path.Combine(folder, "info.plist");
                if (!File.Exists(info) || !File.ReadAllText(info).Contains(wanted))
                    continue;
                string checkouts = Path.Combine(folder, "SourcePackages", "checkouts");
                if (Directory.Exists(checkouts) && Directory.EnumerateDirectories(checkouts).Any())
                    return true;
            }

            return false;
        }

        internal static bool UsesSwiftPackages(string workspace)
        {
            string folder = Path.GetDirectoryName((workspace ?? "").TrimEnd('/'));
            if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
                return false;
            foreach (string project in Directory.GetDirectories(folder, "*.xcodeproj"))
            {
                string file = Path.Combine(project, "project.pbxproj");
                if (File.Exists(file) && File.ReadAllText(file).Contains("XCRemoteSwiftPackageReference"))
                    return true;
            }

            return false;
        }

        internal static Outcome Poll()
        {
            if (process == null || !process.HasExited)
                return null;
            process.WaitForExit();
            int exitCode = process.ExitCode;
            process.Dispose();
            process = null;
            string text;
            lock (Output)
                text = Output.ToString();
            Outcome outcome = Read(exitCode, text, Activity);
            Activity = "";
            return outcome;
        }

        internal static Outcome Read(int exitCode, string text, string activity)
        {
            string[] lines = (text ?? "").Split('\n').Select(line => line.Trim()).Where(line => line.Length > 0).ToArray();
            if (exitCode != 0)
            {
                string error = lines.LastOrDefault() ?? "osascript failed.";
                if (error.Contains(NotAllowedCode))
                    error = "Unity may not control Xcode. Allow it in System Settings > Privacy & Security > Automation, then try again.";
                else
                    error = Clean(error);
                return new Outcome { Ok = false, Message = "Xcode: " + error };
            }

            string result = lines.LastOrDefault(line => line.Contains("|")) ?? "";
            int bar = result.IndexOf('|');
            string status = bar >= 0 ? result.Substring(0, bar).Trim() : result.Trim();
            string message = bar >= 0 ? result.Substring(bar + 1).Trim() : "";
            switch (status)
            {
                case "opened":
                    return new Outcome { Ok = true, Message = "Xcode has the project open with the iPhone selected. Press Run there." };
                case "opened-only":
                    return new Outcome
                    {
                        Ok = true,
                        Message = "Xcode has the project open, but another workspace with the same name is open too (" + message.TrimEnd('/') +
                                  "), so pick the iPhone and press Run in Xcode yourself.",
                    };
                case "succeeded":
                    return new Outcome { Ok = true, Message = "Xcode built the app and launched it (" + activity + "). Its console shows the app log." };
                case "cancelled":
                    return new Outcome { Ok = false, Message = "The run was cancelled in Xcode." };
                default:
                    message = message.Trim().TrimEnd(';').Trim();
                    return new Outcome { Ok = false, Message = "Xcode run " + (status.Length > 0 ? status : "ended") + (message.Length > 0 ? ": " + message : ". Check the Xcode window.") };
            }
        }

        internal static string Clean(string error)
        {
            const string Prefix = "execution error: ";
            int start = error.IndexOf(Prefix, StringComparison.Ordinal);
            string value = start >= 0 ? error.Substring(start + Prefix.Length) : error;
            int code = value.LastIndexOf(" (-", StringComparison.Ordinal);
            return (code > 0 ? value.Substring(0, code) : value).Trim();
        }

        private static string Quote(string value) => "\"" + (value ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";

        private static void Append(string line)
        {
            if (line == null)
                return;
            lock (Output)
                Output.AppendLine(line);
        }
    }
}
