using System.Linq;
using HDC.Tool.Editor;
using NUnit.Framework;
using UnityEditor;

namespace HDC.Tool.Tests
{
    public class HDCBuildScenesTests
    {
        private static readonly EditorBuildSettingsScene[] Scenes =
        {
            new EditorBuildSettingsScene("Assets/Splash.unity", true),
            new EditorBuildSettingsScene("Assets/Intro.unity", false),
            new EditorBuildSettingsScene("Assets/Gone.unity", true),
            new EditorBuildSettingsScene("Assets/Game.unity", true),
        };

        [Test]
        public void TheBuildIndexSkipsScenesThatAreOffOrMissing() =>
            CollectionAssert.AreEqual(new[] { 0, -1, -1, 1 }, HDCBuildScenesSection.BuildIndexes(Scenes, path => path != "Assets/Gone.unity"));

        [Test]
        public void ReadingTheListChecksEachSceneFileOnce()
        {
            int checks = 0;
            HDCBuildScenesSection.Facts facts = HDCBuildScenesSection.Read(Scenes, path =>
            {
                checks++;
                return path != "Assets/Gone.unity";
            });

            Assert.AreEqual(Scenes.Length, checks, "one check per scene, not one per scene for every row");
            CollectionAssert.AreEqual(new[] { true, true, false, true }, facts.Exists);
            CollectionAssert.AreEqual(new[] { 0, -1, -1, 1 }, facts.Indexes);
            CollectionAssert.AreEqual(new[] { "Splash", "Intro", "Gone (missing)", "Game" }, facts.Names);
            Assert.AreEqual("Assets/Game.unity", facts.Paths[3].tooltip);
            Assert.AreEqual(3, facts.Enabled);
            Assert.IsTrue(facts.AnyMissing);
            Assert.IsFalse(HDCBuildScenesSection.Read(Scenes, _ => true).AnyMissing);
            Assert.AreEqual(0, HDCBuildScenesSection.Read(new EditorBuildSettingsScene[0], _ => true).Count);
        }

        [Test]
        public void DroppedScenesAreAddedOnceAndTurnedOn()
        {
            EditorBuildSettingsScene[] result = HDCBuildScenesSection.WithScenes(Scenes, new[] { "Assets/Game.unity", "Assets/Shop.unity", "", "Assets/Shop.unity" });

            CollectionAssert.AreEqual(Scenes.Select(scene => scene.path).Concat(new[] { "Assets/Shop.unity" }).ToArray(), result.Select(scene => scene.path).ToArray());
            Assert.IsTrue(result.Last().enabled);
        }

        [Test]
        public void TheListIsRebuiltWhenTheBuildSettingsChange()
        {
            Assert.IsTrue(HDCBuildScenesSection.Same(Scenes, Scenes.Select(scene => new EditorBuildSettingsScene(scene.path, scene.enabled)).ToArray()));
            Assert.IsFalse(HDCBuildScenesSection.Same(Scenes, Scenes.Reverse().ToArray()));
            Assert.IsFalse(HDCBuildScenesSection.Same(Scenes, Scenes.Take(3).ToArray()));
            EditorBuildSettingsScene[] toggled = Scenes.Select(scene => new EditorBuildSettingsScene(scene.path, !scene.enabled)).ToArray();
            Assert.IsFalse(HDCBuildScenesSection.Same(Scenes, toggled));
        }
    }
}
