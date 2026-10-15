using System.Collections.Generic;
using System.IO;
using HDC.Ads;
using HDC.Tool.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HDC.Tool.Tests
{
    public class HDCBuildStampTests
    {
        [Test]
        public void TheSameRulesGiveTheSameStamp()
        {
            HDCBuildRules rules = HDCBuildRules.Defaults(HDCBuildPreset.TestDebug);

            Assert.AreEqual(HDCBuildStamp.Text(rules), HDCBuildStamp.Text(rules.Copy()), "an unchanged preset keeps Unity's cached scenes");
        }

        [TestCase("debugPanel")]
        [TestCase("debugLog")]
        [TestCase("googleTestAds")]
        [TestCase("googleTestAdUnits")]
        public void EachRuleTheScenesGetChangesTheStamp(string rule)
        {
            HDCBuildRules rules = HDCBuildRules.Defaults(HDCBuildPreset.TestDebug);
            string before = HDCBuildStamp.Text(rules);
            System.Reflection.FieldInfo field = typeof(HDCBuildRules).GetField(rule);
            field.SetValue(rules, !(bool)field.GetValue(rules));

            Assert.AreNotEqual(before, HDCBuildStamp.Text(rules));
        }

        [Test]
        public void TheAdjustEnvironmentChangesTheStamp()
        {
            HDCBuildRules rules = HDCBuildRules.Defaults(HDCBuildPreset.TestDebug);
            string before = HDCBuildStamp.Text(rules);
            rules.adjustEnvironment = rules.adjustEnvironment == HDCAdjustEnvironment.Sandbox ? HDCAdjustEnvironment.Production : HDCAdjustEnvironment.Sandbox;

            Assert.AreNotEqual(before, HDCBuildStamp.Text(rules));
        }

        [Test]
        public void TheStampIsAResourceOnlyWhileTheBuildRuns()
        {
            bool folderBefore = AssetDatabase.IsValidFolder(HDCBuildStamp.Folder);
            List<string> made = HDCBuildStamp.Write(HDCBuildRules.Defaults(HDCBuildPreset.Release));
            try
            {
                var stamp = AssetDatabase.LoadAssetAtPath<TextAsset>(HDCBuildStamp.AssetPath);
                Assert.IsNotNull(stamp);
                StringAssert.Contains("\"debugPanel\": false", stamp.text);
                Assert.AreEqual(HDCBuildStamp.Guid, AssetDatabase.AssetPathToGUID(HDCBuildStamp.AssetPath), "a fixed GUID keeps the stamp the same between builds");
            }
            finally
            {
                HDCBuildStamp.Remove(made);
            }

            Assert.IsFalse(File.Exists(HDCBuildStamp.AssetPath));
            Assert.AreEqual(folderBefore, AssetDatabase.IsValidFolder(HDCBuildStamp.Folder), "a folder the stamp made is removed again");
        }
    }
}
