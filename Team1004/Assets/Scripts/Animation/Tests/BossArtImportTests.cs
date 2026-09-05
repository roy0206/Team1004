using Game.Animation.Editor;
using NUnit.Framework;
using UnityEngine;

namespace Game.Animation.Tests
{
    public sealed class BossArtImportTests
    {
        private const string WaveFrame1 = BossArtPostprocessor.WaveFolder + "/물결1.png";
        private const string WaveFrame2 = BossArtPostprocessor.WaveFolder + "/물결 2.png";
        private const string HookFrame = BossArtPostprocessor.HookPath;

        private const int UnionMinX = 33;
        private const int UnionMaxX = 1919;
        private const int UnionMinY = 229;
        private const int UnionMaxY = 563;
        private const float UnionPivotX = 0.5085937f;
        private const float UnionPivotY = 0.36712963f;
        private const float Tolerance = 1e-5f;

        [Test]
        public void BossArtPaths_AreRecognised()
        {
            Assert.IsTrue(BossArtPostprocessor.IsBossArtPath(WaveFrame1));
            Assert.IsTrue(BossArtPostprocessor.IsBossArtPath(HookFrame));
            Assert.IsTrue(BossArtPostprocessor.IsHookPath(HookFrame));
            Assert.IsFalse(BossArtPostprocessor.IsHookPath(WaveFrame1));
            Assert.IsFalse(BossArtPostprocessor.IsBossArtPath(ObjectArtPostprocessor.ArtFolder + "/돌1.png"));
            Assert.IsFalse(BossArtPostprocessor.IsBossArtPath(BossArtPostprocessor.ArtFolder + "/-곰-/곰 발 양옆.png"));
            Assert.IsFalse(BossArtPostprocessor.IsBossArtPath(null));
        }

        [Test]
        public void WaveFolder_IsASharedPivotGroupOfTwoFrames()
        {
            var folder = SharedPivotFolders.BossWave;

            Assert.IsFalse(folder.Recursive);
            Assert.AreSame(folder, SharedPivotFolders.Find(WaveFrame1));
            Assert.AreSame(folder, SharedPivotFolders.Find(WaveFrame2));
            Assert.IsNull(SharedPivotFolders.Find(HookFrame));

            var frames = folder.EnumerateFrames();

            if (frames.Count == 0)
                Assert.Ignore("보스 물결 프레임이 없다. Drive 동기화 뒤 다시 돌린다.");

            Assert.AreEqual(2, frames.Count);
            Assert.AreEqual(BossClipTable.TotalFrameCount, frames.Count);
            CollectionAssert.Contains(frames, WaveFrame1);
            CollectionAssert.Contains(frames, WaveFrame2);
        }

        [Test]
        public void WaveUnion_MatchesTheRecordedBoxAndPivot()
        {
            var frames = SharedPivotFolders.BossWave.EnumerateFrames();

            if (frames.Count == 0)
                Assert.Ignore("보스 물결 프레임이 없다. Drive 동기화 뒤 다시 돌린다.");

            Assert.IsTrue(AnimationAssetGenerator.TryGetAlphaBounds(frames, out var bounds, out var textureSize));

            Assert.AreEqual(1920, textureSize.x);
            Assert.AreEqual(1080, textureSize.y);
            Assert.AreEqual(UnionMinX, Mathf.RoundToInt(bounds.xMin));
            Assert.AreEqual(UnionMaxX, Mathf.RoundToInt(bounds.xMax) - 1);
            Assert.AreEqual(UnionMinY, Mathf.RoundToInt(bounds.yMin));
            Assert.AreEqual(UnionMaxY, Mathf.RoundToInt(bounds.yMax) - 1);

            var pivot = SharedPivotFolders.BossWave.ResolvePivot();

            Assert.AreEqual(UnionPivotX, pivot.x, Tolerance);
            Assert.AreEqual(UnionPivotY, pivot.y, Tolerance);
            Assert.AreEqual(UnionPivotX, BossArtPostprocessor.WaveFallbackPivot.x, Tolerance);
            Assert.AreEqual(UnionPivotY, BossArtPostprocessor.WaveFallbackPivot.y, Tolerance);
        }

        [Test]
        public void HookPivot_IsTheHandPickedAttachPointNotTheAlphaCentre()
        {
            var pivot = BossArtPostprocessor.ResolvePivot(HookFrame);

            Assert.AreEqual(BossArtPostprocessor.HookPivot.x, pivot.x, Tolerance);
            Assert.AreEqual(BossArtPostprocessor.HookPivot.y, pivot.y, Tolerance);

            if (!AnimationAssetGenerator.TryGetAlphaBounds(HookFrame, out _, out var textureSize))
                Assert.Ignore("낚시바늘 아트가 없다. Drive 동기화 뒤 다시 돌린다.");

            Assert.AreEqual(1920, textureSize.x);
            Assert.AreEqual(1080, textureSize.y);
            Assert.Less(pivot.y, 0.5f);
        }

        [Test]
        public void RapidClip_ReadsBothWaveFrames()
        {
            var definition = BossClipTable.Rapid;

            Assert.AreEqual("Boss_Rapid", definition.AssetName);
            Assert.AreEqual(2, definition.FrameCount);
            Assert.IsTrue(definition.Loop);
            Assert.AreEqual(BossArtPostprocessor.WaveFolder, definition.FolderPath);

            var paths = AnimationAssetGenerator.GetFramePaths(definition);

            Assert.AreEqual(WaveFrame1, paths[0]);
            Assert.AreEqual(WaveFrame2, paths[1]);
        }
    }
}
