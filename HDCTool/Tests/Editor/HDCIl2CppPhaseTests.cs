using HDC.Tool.Editor;
using NUnit.Framework;

namespace HDC.Tool.Tests
{
    public class HDCIl2CppPhaseTests
    {
        private const string Il2Cpp =
            "\t\tC62A2A42F32E085EF849CF0B /* ShellScript */ = {\n\t\t\tisa = PBXShellScriptBuildPhase;\n\t\t\tbuildActionMask = 2147483647;\n\t\t\tfiles = (\n\t\t\t);\n" +
            "\t\t\trunOnlyForDeploymentPostprocessing = 0;\n\t\t\tshellPath = /bin/sh;\n" +
            "\t\t\tshellScript = \"\\\"$IL2CPP\\\" --compile-cpp --outputpath=\\\"$CONFIGURATION_BUILD_DIR/libGameAssembly.a\\\"\\n\";\n\t\t};\n";

        private const string Pods =
            "\t\t9E5298FFDBEA548B9C039579 /* [CP] Check Pods Manifest.lock */ = {\n\t\t\tisa = PBXShellScriptBuildPhase;\n\t\t\tbuildActionMask = 2147483647;\n" +
            "\t\t\toutputPaths = (\n\t\t\t\t\"$(DERIVED_FILE_DIR)/Pods-UnityFramework-checkManifestLockResult.txt\",\n\t\t\t);\n" +
            "\t\t\trunOnlyForDeploymentPostprocessing = 0;\n\t\t\tshellScript = \"diff Podfile.lock Manifest.lock\";\n\t\t};\n";

        [Test]
        public void TheIl2CppPhaseDeclaresTheLibraryItBuilds()
        {
            string project = HDCIl2CppPhase.WithOutput(Pods + Il2Cpp);

            StringAssert.Contains("isa = PBXShellScriptBuildPhase;\n\t\t\talwaysOutOfDate = 1;\n\t\t\tbuildActionMask", project);
            StringAssert.Contains("\t\t\toutputPaths = (\n\t\t\t\t\"$(CONFIGURATION_BUILD_DIR)/libGameAssembly.a\",\n\t\t\t);\n\t\t\trunOnlyForDeploymentPostprocessing", project);
            StringAssert.StartsWith(Pods, project, "other script phases stay as they are");
        }

        [Test]
        public void AnEmptyOutputListIsFilled()
        {
            string project = HDCIl2CppPhase.WithOutput(Il2Cpp.Replace("\t\t\trunOnlyForDeploymentPostprocessing", "\t\t\toutputPaths = (\n\t\t\t);\n\t\t\trunOnlyForDeploymentPostprocessing"));

            StringAssert.Contains("\t\t\toutputPaths = (\n\t\t\t\t\"$(CONFIGURATION_BUILD_DIR)/libGameAssembly.a\",\n\t\t\t);\n", project);
        }

        [Test]
        public void RunningTwiceChangesNothingMore()
        {
            string once = HDCIl2CppPhase.WithOutput(Il2Cpp);

            Assert.AreEqual(once, HDCIl2CppPhase.WithOutput(once));
        }
    }
}
