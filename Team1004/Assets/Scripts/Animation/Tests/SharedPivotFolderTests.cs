using Game.Animation.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Animation.Tests
{
    public sealed class SharedPivotFolderTests
    {
        private const string SalmonFrame = AnimationAssetGenerator.ArtFolder + "/기본/물고기 기본-1.png";
        private const string HitFrame = AnimationAssetGenerator.ArtFolder + "/충돌/충돌.png";
        private const string FishFrame = ObjectArtPostprocessor.ObstacleFishFolder + "/장애물 물고기1.png";
        private const string RockArt = ObjectArtPostprocessor.ArtFolder + "/돌1.png";

        private const int UnionMinX = 755;
        private const int UnionMaxX = 1282;
        private const int UnionMinY = 266;
        private const int UnionMaxY = 545;
        private const int TextureWidth = 1920;
        private const int TextureHeight = 1080;
        private const float UnionPivotX = 0.5307292f;
        private const float UnionPivotY = 0.37592593f;

        [Test]
        public void SalmonFolder_IsRecursiveAndCoversEveryClipFolder()
        {
            var folder = SharedPivotFolders.Salmon;

            Assert.IsTrue(folder.Recursive);
            Assert.IsTrue(folder.Contains(SalmonFrame));
            Assert.IsTrue(folder.Contains(AnimationAssetGenerator.ArtFolder + "/점프/물고기 점프-5.png"));
            Assert.IsTrue(folder.Contains(HitFrame));
            Assert.IsFalse(folder.Contains(RockArt));
            Assert.IsFalse(folder.Contains(FishFrame));
            Assert.IsFalse(folder.Contains(null));
        }

        [Test]
        public void SalmonFolder_ListsTwelveFramesIncludingTheHitPose()
        {
            var frames = AnimationAssetGenerator.EnumerateFramePaths();

            Assert.AreEqual(12, frames.Count);
            Assert.AreEqual(SalmonClipTable.TotalFrameCount, frames.Count);
            CollectionAssert.Contains(frames, HitFrame);
            Assert.AreSame(SharedPivotFolders.Salmon, SharedPivotFolders.Find(HitFrame));
        }

        [Test]
        public void SalmonUnion_MatchesTheRecordedBoxAndPivot()
        {
            var frames = AnimationAssetGenerator.EnumerateFramePaths();

            if (frames.Count == 0)
                Assert.Ignore("연어 프레임이 없다. Drive 동기화 뒤 다시 돌린다.");

            Assert.IsTrue(AnimationAssetGenerator.TryGetAlphaBounds(frames, out var bounds, out var textureSize));

            Assert.AreEqual(TextureWidth, textureSize.x);
            Assert.AreEqual(TextureHeight, textureSize.y);
            Assert.AreEqual(UnionMinX, (int)bounds.xMin);
            Assert.AreEqual(UnionMaxX, (int)bounds.xMax - 1);
            Assert.AreEqual(UnionMinY, (int)bounds.yMin);
            Assert.AreEqual(UnionMaxY, (int)bounds.yMax - 1);
            Assert.AreEqual(528f, bounds.width, 1e-3f);
            Assert.AreEqual(280f, bounds.height, 1e-3f);

            var pivot = AnimationAssetGenerator.AlphaBoundsPivot(bounds, textureSize);
            Assert.AreEqual(UnionPivotX, pivot.x, 1e-6f);
            Assert.AreEqual(UnionPivotY, pivot.y, 1e-6f);
            Assert.AreEqual(UnionPivotX, AnimationAssetGenerator.SalmonFallbackPivot.x, 1e-6f);
            Assert.AreEqual(UnionPivotY, AnimationAssetGenerator.SalmonFallbackPivot.y, 1e-6f);
        }

        [Test]
        public void SalmonUnion_ContainsEveryFrameBox()
        {
            var frames = AnimationAssetGenerator.EnumerateFramePaths();

            if (frames.Count == 0)
                Assert.Ignore("연어 프레임이 없다. Drive 동기화 뒤 다시 돌린다.");

            Assert.IsTrue(AnimationAssetGenerator.TryGetAlphaBounds(frames, out var union, out _));

            for (var i = 0; i < frames.Count; i++)
            {
                Assert.IsTrue(AnimationAssetGenerator.TryGetAlphaBounds(frames[i], out var single, out _), frames[i]);
                Assert.LessOrEqual(union.xMin, single.xMin, frames[i]);
                Assert.LessOrEqual(union.yMin, single.yMin, frames[i]);
                Assert.GreaterOrEqual(union.xMax, single.xMax, frames[i]);
                Assert.GreaterOrEqual(union.yMax, single.yMax, frames[i]);
            }
        }

        [Test]
        public void StaleSiblings_AreQueuedOnlyWhenThePivotActuallyDiffers()
        {
            var frames = AnimationAssetGenerator.EnumerateFramePaths();

            if (frames.Count == 0)
                Assert.Ignore("연어 프레임이 없다. Drive 동기화 뒤 다시 돌린다.");

            var pivot = SharedPivotFolders.Salmon.ResolvePivot();

            Assert.IsEmpty(SalmonArtPostprocessor.FindStaleSiblings(frames[0], pivot));

            var moved = new Vector2(pivot.x + 0.01f, pivot.y);
            var stale = SalmonArtPostprocessor.FindStaleSiblings(frames[0], moved);

            Assert.AreEqual(frames.Count - 1, stale.Count);
            CollectionAssert.DoesNotContain(stale, frames[0]);
        }

        [Test]
        public void ObstacleFishFolder_IsNotRecursiveAndCoversOnlyItsOwnFrames()
        {
            var folder = SharedPivotFolders.ObstacleFish;

            Assert.IsFalse(folder.Recursive);
            Assert.IsTrue(folder.Contains(FishFrame));
            Assert.IsTrue(folder.Contains(ObjectArtPostprocessor.ObstacleFishFolder + "/장애물 물고기2.png"));
            Assert.IsFalse(folder.Contains(RockArt));
            Assert.IsFalse(folder.Contains(ObjectArtPostprocessor.ObstacleFishFolder + "/하위/장애물 물고기1.png"));
            Assert.IsFalse(folder.Contains(ObjectArtPostprocessor.ObstacleFishFolder + "/장애물 물고기1.psd"));
        }

        [Test]
        public void Find_ReturnsTheGroupOnlyForSharedFolders()
        {
            Assert.AreSame(SharedPivotFolders.Salmon, SharedPivotFolders.Find(SalmonFrame));
            Assert.AreSame(SharedPivotFolders.ObstacleFish, SharedPivotFolders.Find(FishFrame));
            Assert.IsNull(SharedPivotFolders.Find(RockArt));
            Assert.IsNull(SharedPivotFolders.Find(ObjectArtPostprocessor.ArtFolder + "/통나무.png"));
        }

        [Test]
        public void ObstacleFishFolder_ListsBothFrames()
        {
            RequireFishArt();

            var frames = SharedPivotFolders.ObstacleFish.EnumerateFrames();
            Assert.AreEqual(2, frames.Count);
            CollectionAssert.Contains(frames, FishFrame);
        }

        [Test]
        public void ObstacleFishFrames_ShareTheUnionPivot()
        {
            RequireFishArt();

            var folder = SharedPivotFolders.ObstacleFish;
            Assert.IsTrue(folder.TryGetBounds(out var bounds, out var textureSize));

            var expected = AnimationAssetGenerator.AlphaBoundsPivot(bounds, textureSize);
            var frames = folder.EnumerateFrames();

            for (var i = 0; i < frames.Count; i++)
            {
                var pivot = ObjectArtPostprocessor.ResolvePivot(frames[i]);
                Assert.AreEqual(expected.x, pivot.x, 1e-5f, frames[i]);
                Assert.AreEqual(expected.y, pivot.y, 1e-5f, frames[i]);
            }
        }

        [Test]
        public void ObstacleFishUnion_ContainsEveryFrameBox()
        {
            RequireFishArt();

            var folder = SharedPivotFolders.ObstacleFish;
            Assert.IsTrue(folder.TryGetBounds(out var union, out _));

            var frames = folder.EnumerateFrames();

            for (var i = 0; i < frames.Count; i++)
            {
                Assert.IsTrue(AnimationAssetGenerator.TryGetAlphaBounds(frames[i], out var single, out _), frames[i]);
                Assert.LessOrEqual(union.xMin, single.xMin, frames[i]);
                Assert.LessOrEqual(union.yMin, single.yMin, frames[i]);
                Assert.GreaterOrEqual(union.xMax, single.xMax, frames[i]);
                Assert.GreaterOrEqual(union.yMax, single.yMax, frames[i]);
            }
        }

        [Test]
        public void UngroupedObjectArt_KeepsItsOwnAlphaBoxPivot()
        {
            if (AssetDatabase.LoadAssetAtPath<Texture2D>(RockArt) == null)
                Assert.Ignore(RockArt + "가 없다. Drive 동기화 뒤 다시 돌린다.");

            Assert.IsTrue(AnimationAssetGenerator.TryGetAlphaBounds(RockArt, out var bounds, out var textureSize));

            var expected = AnimationAssetGenerator.AlphaBoundsPivot(bounds, textureSize);
            var pivot = ObjectArtPostprocessor.ResolvePivot(RockArt);

            Assert.AreEqual(expected.x, pivot.x, 1e-5f);
            Assert.AreEqual(expected.y, pivot.y, 1e-5f);
        }

        [Test]
        public void SalmonPivot_IsTheUnionOfEveryFrame()
        {
            var frames = AnimationAssetGenerator.EnumerateFramePaths();

            if (frames.Count == 0)
                Assert.Ignore("연어 프레임이 없다. Drive 동기화 뒤 다시 돌린다.");

            Assert.IsTrue(AnimationAssetGenerator.TryGetAlphaBounds(frames, out var bounds, out var textureSize));

            var expected = AnimationAssetGenerator.AlphaBoundsPivot(bounds, textureSize);
            var pivot = SharedPivotFolders.Salmon.ResolvePivot();

            Assert.AreEqual(expected.x, pivot.x, 1e-5f);
            Assert.AreEqual(expected.y, pivot.y, 1e-5f);
        }

        [Test]
        public void EverySalmonMetaUsesTheRecomputedUnionPivot()
        {
            var frames = AnimationAssetGenerator.EnumerateFramePaths();

            if (frames.Count == 0)
                Assert.Ignore("연어 프레임이 없다. Drive 동기화 뒤 다시 돌린다.");

            Assert.IsTrue(AnimationAssetGenerator.TryGetAlphaBounds(frames, out var bounds, out var textureSize));

            var expected = AnimationAssetGenerator.AlphaBoundsPivot(bounds, textureSize);

            for (var i = 0; i < frames.Count; i++)
            {
                var importer = AssetImporter.GetAtPath(frames[i]) as TextureImporter;
                Assert.IsNotNull(importer, frames[i]);

                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);

                Assert.AreEqual((int)SpriteAlignment.Custom, settings.spriteAlignment, frames[i]);
                Assert.AreEqual(
                    expected.x,
                    settings.spritePivot.x,
                    0.0005f,
                    frames[i] + "의 pivot이 12장 합집합과 다르다. 다시 임포트하거나 'Team1004/Generate Animation Assets'를 돌린다.");
                Assert.AreEqual(
                    expected.y,
                    settings.spritePivot.y,
                    0.0005f,
                    frames[i] + "의 pivot이 12장 합집합과 다르다. 다시 임포트하거나 'Team1004/Generate Animation Assets'를 돌린다.");
            }
        }

        private static void RequireFishArt()
        {
            if (AssetDatabase.LoadAssetAtPath<Texture2D>(FishFrame) == null)
                Assert.Ignore(ObjectArtPostprocessor.ObstacleFishFolder + "에 PNG가 없다. Drive 동기화 뒤 다시 돌린다.");
        }
    }
}
