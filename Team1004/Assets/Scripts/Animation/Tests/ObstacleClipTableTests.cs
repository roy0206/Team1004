using Game.Animation.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Animation.Tests
{
    public sealed class ObstacleClipTableTests
    {
        [Test]
        public void TableHasTheFishClip()
        {
            Assert.AreEqual(1, ObstacleClipTable.All.Count);
            Assert.AreEqual(2, ObstacleClipTable.TotalFrameCount);
            Assert.AreSame(ObstacleClipTable.Fish, ObstacleClipTable.Find(ObstacleClipTable.FishClipName));
            Assert.IsNull(ObstacleClipTable.Find("Player_Swim"));
        }

        [Test]
        public void FishClipLoopsAtSixFramesPerSecond()
        {
            var definition = ObstacleClipTable.Fish;

            Assert.AreEqual("Obstacle_Fish", definition.AssetName);
            Assert.AreEqual(2, definition.FrameCount);
            Assert.AreEqual(6f, definition.FramesPerSecond, 1e-3f);
            Assert.IsTrue(definition.Loop);
            Assert.AreEqual(FlipbookClipDuration.FromFrames, definition.DurationSource);
            Assert.AreEqual(0f, AnimationAssetGenerator.ResolveDuration(definition, AnimationAssetGenerator.LoadConfigValues()), 1e-4f);
        }

        [Test]
        public void FishClipLivesUnderTheObjectArtFolder()
        {
            var definition = ObstacleClipTable.Fish;

            Assert.AreEqual(ObjectArtPostprocessor.ArtFolder, definition.RootFolder);
            Assert.AreEqual(ObjectArtPostprocessor.ObstacleFishFolder, definition.FolderPath);
            Assert.AreEqual(AnimationAssetGenerator.DesignFolder + "/Obstacle_Fish.asset", definition.AssetPath);
        }

        [Test]
        public void FishFramePathsResolveWithoutADash()
        {
            var frames = AnimationAssetGenerator.GetFramePaths(ObstacleClipTable.Fish);

            Assert.AreEqual(2, frames.Count);
            Assert.AreEqual(ObjectArtPostprocessor.ObstacleFishFolder + "/장애물 물고기1.png", frames[0]);
            Assert.AreEqual(ObjectArtPostprocessor.ObstacleFishFolder + "/장애물 물고기2.png", frames[1]);
        }

        [Test]
        public void FishFrameSpritesExist()
        {
            var frames = AnimationAssetGenerator.GetFramePaths(ObstacleClipTable.Fish);

            if (AssetDatabase.LoadAssetAtPath<Texture2D>(frames[0]) == null)
                Assert.Ignore("장애물 물고기 아트가 없다. Drive 동기화 뒤 다시 돌린다.");

            for (var i = 0; i < frames.Count; i++)
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(frames[i]);
                Assert.IsNotNull(sprite, frames[i] + " 스프라이트가 없다.");
                Assert.AreEqual(AnimationAssetGenerator.PixelsPerUnit, sprite.pixelsPerUnit, 1e-3f, frames[i]);
            }
        }

        [Test]
        public void GeneratedFishClipMatchesTheTable()
        {
            var definition = ObstacleClipTable.Fish;
            var clip = AssetDatabase.LoadAssetAtPath<CustomAnimation>(definition.AssetPath);

            Assert.IsNotNull(clip, definition.AssetPath + "가 없다. 'Team1004/Generate Animation Assets'를 돌린다.");
            Assert.AreEqual(definition.FrameCount, clip.FrameCount);
            Assert.AreEqual(definition.Loop, clip.Loop);
            Assert.AreEqual(definition.FramesPerSecond, clip.FramesPerSecond, 1e-3f);
            Assert.IsTrue(clip.Validate(out var error), error);
        }
    }
}
