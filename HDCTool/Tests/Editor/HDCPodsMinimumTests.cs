using System.Text.RegularExpressions;
using HDC.Tool.Editor;
using NUnit.Framework;

namespace HDC.Tool.Tests
{
    public class HDCPodsMinimumTests
    {
        private const string Podfile =
            "source 'https://cdn.cocoapods.org/'\n\nplatform :ios, '15.0'\n\ntarget 'UnityFramework' do\n  pod 'Google-Mobile-Ads-SDK', '~> 13.9'\nend\n";

        [Test]
        public void APodfileWithoutAHookGetsOne()
        {
            string podfile = HDCPodsMinimum.WithHook(Podfile, "15.0");

            StringAssert.StartsWith(Podfile, podfile);
            Assert.AreEqual(1, Count(podfile, "post_install do"));
            StringAssert.Contains("post_install do |installer|\n  hdc_minimum_ios = '15.0'\n", podfile);
            StringAssert.Contains("  installer.pods_project.targets.each { |pod_target| pod_target.build_configurations.each { |config| hdc_raise_minimum.call(config) } }\nend\n", podfile);
        }

        [Test]
        public void AnExistingHookKeepsItsInstallerAndItsCode()
        {
            string podfile = HDCPodsMinimum.WithHook(Podfile + "\npost_install do |pi|\n  puts 'done'\nend\n", "15.0");

            Assert.AreEqual(1, Count(podfile, "post_install do"));
            StringAssert.Contains("post_install do |pi|\n  hdc_minimum_ios = '15.0'\n", podfile);
            StringAssert.Contains("  pi.pods_project.build_configurations.each", podfile);
            StringAssert.Contains("hdc_raise_minimum.call(config) } }\n  puts 'done'\nend\n", podfile);
        }

        [Test]
        public void RunningAgainOnlyChangesTheVersion()
        {
            string podfile = HDCPodsMinimum.WithHook(HDCPodsMinimum.WithHook(Podfile, "15.0"), "16.0");

            Assert.AreEqual(1, Count(podfile, "post_install do"));
            Assert.AreEqual(1, Count(podfile, "hdc_raise_minimum = lambda"));
            StringAssert.Contains("hdc_minimum_ios = '16.0'", podfile);
            StringAssert.DoesNotContain("'15.0'\n  hdc_raise_minimum", podfile);
        }

        [TestCase("13.0", "15.0")]
        [TestCase("16.2", "16.2")]
        public void ThePodsGetTheHigherOfThePodfileAndPlayerSettings(string player, string expected) =>
            Assert.AreEqual(expected, HDCPodsMinimum.Minimum(player, Podfile));

        [Test]
        public void WithoutAPlatformLineThePlayerSettingsCount() => Assert.AreEqual("14.0", HDCPodsMinimum.Minimum("14.0", "target 'UnityFramework' do\nend\n"));

        private static int Count(string text, string part) => Regex.Matches(text, Regex.Escape(part)).Count;
    }
}
