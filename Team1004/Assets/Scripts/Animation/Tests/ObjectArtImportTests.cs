using System.Collections.Generic;
using Game.Animation.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Animation.Tests
{
    public sealed class ObjectArtImportTests
    {
        private List<string> paths;

        [SetUp]
        public void SetUp()
        {
            paths = new List<string>();

            if (!AssetDatabase.IsValidFolder(ObjectArtPostprocessor.ArtFolder))
                return;

            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { ObjectArtPostprocessor.ArtFolder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);

                if (ObjectArtPostprocessor.IsObjectArtPath(path))
                    paths.Add(path);
            }
        }

        [Test]
        public void IsObjectArtPath_AcceptsOnlyPngsInTheObjectFolder()
        {
            Assert.IsTrue(ObjectArtPostprocessor.IsObjectArtPath(ObjectArtPostprocessor.ArtFolder + "/돌1.png"));
            Assert.IsFalse(ObjectArtPostprocessor.IsObjectArtPath(ObjectArtPostprocessor.ArtFolder + "/돌1.psd"));
            Assert.IsFalse(ObjectArtPostprocessor.IsObjectArtPath(AnimationAssetGenerator.ArtFolder + "/기본/물고기 기본-1.png"));
            Assert.IsFalse(ObjectArtPostprocessor.IsObjectArtPath(null));
        }

        [Test]
        public void IsObjectArtPath_CoversSubfolders()
        {
            Assert.IsTrue(ObjectArtPostprocessor.IsObjectArtPath(ObjectArtPostprocessor.ObstacleFishFolder + "/장애물 물고기1.png"));
            Assert.IsTrue(ObjectArtPostprocessor.IsObjectArtPath(ObjectArtPostprocessor.ObstacleFishFolder + "/장애물 물고기2.png"));
        }

        [Test]
        public void EveryFishFrame_IsCollectedByTheObjectArtScan()
        {
            RequireArt();

            var frames = AnimationAssetGenerator.GetFramePaths(ObstacleClipTable.Fish);

            for (var i = 0; i < frames.Count; i++)
            {
                Assert.IsTrue(ObjectArtPostprocessor.IsObjectArtPath(frames[i]), frames[i]);
                CollectionAssert.Contains(paths, frames[i], frames[i] + "가 오브젝트 아트 스캔에 잡히지 않는다.");
            }
        }

        [Test]
        public void EveryObjectArtPng_HasAnAlphaBoundingBox()
        {
            RequireArt();

            foreach (var path in paths)
            {
                Assert.IsTrue(AnimationAssetGenerator.TryGetAlphaBounds(path, out var bounds, out var size), path);
                Assert.Greater(bounds.width, 0f, path);
                Assert.Greater(bounds.height, 0f, path);
                Assert.LessOrEqual(bounds.xMax, size.x, path);
                Assert.LessOrEqual(bounds.yMax, size.y, path);
            }
        }

        [Test]
        public void EveryObjectArtPng_ImportsAsASpriteWithTheExpectedPivot()
        {
            RequireArt();

            foreach (var path in paths)
            {
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                Assert.IsNotNull(importer, path);

                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);

                Assert.AreEqual(TextureImporterType.Sprite, settings.textureType, path);
                Assert.AreEqual((int)SpriteImportMode.Single, settings.spriteMode, path);
                Assert.AreEqual(AnimationAssetGenerator.PixelsPerUnit, settings.spritePixelsPerUnit, 0.001f, path);
                Assert.AreEqual(SpriteMeshType.Tight, settings.spriteMeshType, path);
                Assert.IsFalse(settings.mipmapEnabled, path);
                Assert.AreEqual(TextureImporterCompression.Uncompressed, importer.textureCompression, path);

                var expected = ObjectArtPostprocessor.ResolvePivot(path);
                Assert.AreEqual(expected.x, settings.spritePivot.x, 0.0005f, path);
                Assert.AreEqual(expected.y, settings.spritePivot.y, 0.0005f, path);
                Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Sprite>(path), path);
            }
        }

        [Test]
        public void ObjectArtPivots_AreNotShared()
        {
            RequireArt();

            var pivots = new List<Vector2>();

            foreach (var path in paths)
                pivots.Add(ObjectArtPostprocessor.ResolvePivot(path));

            var distinct = false;

            for (var i = 1; i < pivots.Count && !distinct; i++)
                distinct = (pivots[i] - pivots[0]).sqrMagnitude > 0.000001f;

            Assert.IsTrue(pivots.Count < 2 || distinct,
                "Object art must use a per-file alpha box pivot, not the shared salmon pivot.");
        }

        [Test]
        public void ObjectArtPivots_AreSharedOnlyInsideAFolderGroup()
        {
            RequireArt();

            foreach (var path in paths)
            {
                var group = SharedPivotFolders.Find(path);
                var pivot = ObjectArtPostprocessor.ResolvePivot(path);

                if (group == null)
                {
                    Assert.IsTrue(AnimationAssetGenerator.TryGetAlphaBounds(path, out var bounds, out var size), path);
                    var own = AnimationAssetGenerator.AlphaBoundsPivot(bounds, size);
                    Assert.AreEqual(own.x, pivot.x, 1e-5f, path);
                    Assert.AreEqual(own.y, pivot.y, 1e-5f, path);
                    continue;
                }

                var shared = group.ResolvePivot();
                Assert.AreEqual(shared.x, pivot.x, 1e-5f, path);
                Assert.AreEqual(shared.y, pivot.y, 1e-5f, path);
            }
        }

        [Test]
        public void ObstacleFishPivot_DiffersFromTheRockAndLogPivots()
        {
            RequireArt();

            var fishFrames = AnimationAssetGenerator.GetFramePaths(ObstacleClipTable.Fish);

            if (fishFrames.Count == 0 || AssetDatabase.LoadAssetAtPath<Texture2D>(fishFrames[0]) == null)
                Assert.Ignore("장애물 물고기 아트가 없다. Drive 동기화 뒤 다시 돌린다.");

            var fishPivot = ObjectArtPostprocessor.ResolvePivot(fishFrames[0]);

            foreach (var path in paths)
            {
                if (SharedPivotFolders.Find(path) != null)
                    continue;

                var pivot = ObjectArtPostprocessor.ResolvePivot(path);
                Assert.Greater((pivot - fishPivot).sqrMagnitude, 1e-6f, path + "가 물고기와 pivot을 공유하면 안 된다.");
            }
        }

        private void RequireArt()
        {
            if (paths.Count == 0)
                Assert.Ignore("Assets/GameAssets/Art/오브젝트에 PNG가 없다. Drive 동기화 뒤 다시 돌린다.");
        }
    }
}
