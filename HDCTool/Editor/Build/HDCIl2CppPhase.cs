using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Callbacks;

namespace HDC.Tool.Editor
{
    internal static class HDCIl2CppPhase
    {
        internal const string Output = "$(CONFIGURATION_BUILD_DIR)/libGameAssembly.a";

        private const string EmptyOutputs = "\t\t\toutputPaths = (\n\t\t\t);\n";
        private static readonly string Outputs = "\t\t\toutputPaths = (\n\t\t\t\t\"" + Output + "\",\n\t\t\t);\n";
        private static readonly Regex ScriptPhase = new Regex(@"(?<head>\t\t\w{24} /\* [^*\n]* \*/ = \{\n\t\t\tisa = PBXShellScriptBuildPhase;\n)(?<body>.*?)(?<tail>\n\t\t\};)",
            RegexOptions.Singleline);

        [PostProcessBuild(1001)]
        private static void DeclareOutput(BuildTarget target, string buildPath)
        {
            string project = Path.Combine(buildPath, "Unity-iPhone.xcodeproj", "project.pbxproj");
            if (target != BuildTarget.iOS || !File.Exists(project))
                return;
            string text = File.ReadAllText(project);
            string declared = WithOutput(text);
            if (declared != text)
                File.WriteAllText(project, declared);
        }

        internal static string WithOutput(string project) =>
            ScriptPhase.Replace(project ?? "", match =>
            {
                string body = match.Groups["body"].Value;
                if (!body.Contains("--outputpath=") || !body.Contains("libGameAssembly.a") || body.Contains(Output))
                    return match.Value;
                if (body.Contains(EmptyOutputs))
                    body = body.Replace(EmptyOutputs, Outputs);
                else if (!body.Contains("outputPaths = ("))
                {
                    int run = body.IndexOf("\t\t\trunOnlyForDeploymentPostprocessing", StringComparison.Ordinal);
                    if (run < 0)
                        return match.Value;
                    body = body.Insert(run, Outputs);
                }
                else
                {
                    return match.Value;
                }

                if (!body.Contains("alwaysOutOfDate"))
                    body = "\t\t\talwaysOutOfDate = 1;\n" + body;
                return match.Groups["head"].Value + body + match.Groups["tail"].Value;
            });
    }
}
