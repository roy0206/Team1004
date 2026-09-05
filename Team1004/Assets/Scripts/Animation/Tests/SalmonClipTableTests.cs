using System.Collections.Generic;
using Game.Animation.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Animation.Tests
{
    public sealed class SalmonFrameMatcherTests
    {
        [Test]
        public void MatchesPlainName()
        {
            Assert.IsTrue(SalmonFrameMatcher.TryGetFrameIndex("물고기 점프", "물고기 점프-3.png", out var index));
            Assert.AreEqual(3, index);
        }

        [Test]
        public void MatchesSpaceBeforeDash()
        {
            Assert.IsTrue(SalmonFrameMatcher.TryGetFrameIndex("물고기 올라가기", "물고기 올라가기 -1.png", out var index));
            Assert.AreEqual(1, index);
        }

        [Test]
        public void MatchesSpaceAfterDash()
        {
            Assert.IsTrue(SalmonFrameMatcher.TryGetFrameIndex("물고기 내려가기", "물고기 내려가기- 2.png", out var index));
            Assert.AreEqual(2, index);
        }

        [Test]
        public void MatchesWithoutExtension()
        {
            Assert.IsTrue(SalmonFrameMatcher.TryGetFrameIndex("물고기 기본", "물고기 기본-2", out var index));
            Assert.AreEqual(2, index);
        }

        [Test]
        public void MatchesMultiDigitIndex()
        {
            Assert.IsTrue(SalmonFrameMatcher.TryGetFrameIndex("물고기 점프", "물고기 점프 - 12.png", out var index));
            Assert.AreEqual(12, index);
        }

        [Test]
        public void RejectsOtherPrefix()
        {
            Assert.IsFalse(SalmonFrameMatcher.TryGetFrameIndex("물고기 기본", "물고기 점프-1.png", out _));
        }

        [Test]
        public void RejectsLongerPrefixWord()
        {
            Assert.IsFalse(SalmonFrameMatcher.TryGetFrameIndex("물고기 기본", "물고기 기본자세-1.png", out _));
        }

        [Test]
        public void RejectsMissingIndex()
        {
            Assert.IsFalse(SalmonFrameMatcher.TryGetFrameIndex("물고기 점프", "물고기 점프.png", out _));
            Assert.IsFalse(SalmonFrameMatcher.TryGetFrameIndex("물고기 점프", "물고기 점프-.png", out _));
        }

        [Test]
        public void RejectsNonNumericIndex()
        {
            Assert.IsFalse(SalmonFrameMatcher.TryGetFrameIndex("물고기 점프", "물고기 점프-a.png", out _));
            Assert.IsFalse(SalmonFrameMatcher.TryGetFrameIndex("물고기 점프", "물고기 점프-1b.png", out _));
        }

        [Test]
        public void RejectsZeroIndex()
        {
            Assert.IsFalse(SalmonFrameMatcher.TryGetFrameIndex("물고기 점프", "물고기 점프-0.png", out _));
        }

        [Test]
        public void FindFramePicksInconsistentNames()
        {
            var names = new List<string>
            {
                "물고기 올라가기-2.png",
                "물고기 올라가기 -1.png"
            };

            Assert.AreEqual("물고기 올라가기 -1.png", SalmonFrameMatcher.FindFrame(names, "물고기 올라가기", 1));
            Assert.AreEqual("물고기 올라가기-2.png", SalmonFrameMatcher.FindFrame(names, "물고기 올라가기", 2));
            Assert.IsNull(SalmonFrameMatcher.FindFrame(names, "물고기 올라가기", 3));
        }

        [Test]
        public void FindFrameHandlesEmptyInput()
        {
            Assert.IsNull(SalmonFrameMatcher.FindFrame(null, "물고기 기본", 1));
            Assert.IsNull(SalmonFrameMatcher.FindFrame(new List<string>(), "물고기 기본", 1));
            Assert.IsNull(SalmonFrameMatcher.FindFrame(new List<string> { "물고기 기본-1.png" }, "물고기 기본", 0));
        }
    }

    public sealed class SalmonClipTableTests
    {
        [Test]
        public void TableHasFourClips()
        {
            Assert.AreEqual(4, SalmonClipTable.All.Count);
            Assert.AreEqual(11, SalmonClipTable.TotalFrameCount);
        }

        [Test]
        public void ClipNamesAndFoldersAreUnique()
        {
            var names = new HashSet<string>();
            var folders = new HashSet<string>();

            foreach (var definition in SalmonClipTable.All)
            {
                Assert.IsTrue(names.Add(definition.AssetName), definition.AssetName + "가 중복이다.");
                Assert.IsTrue(folders.Add(definition.FolderName), definition.FolderName + "가 중복이다.");
                Assert.IsTrue(definition.AssetPath.StartsWith(AnimationAssetGenerator.DesignFolder + "/"));
                Assert.IsTrue(definition.FolderPath.StartsWith(AnimationAssetGenerator.ArtFolder + "/"));
                Assert.Greater(definition.FrameCount, 0);
                Assert.Greater(definition.FramesPerSecond, 0f);
            }
        }

        [Test]
        public void SwimIsTheOnlyLoop()
        {
            Assert.IsTrue(SalmonClipTable.Swim.Loop);
            Assert.IsFalse(SalmonClipTable.LaneUp.Loop);
            Assert.IsFalse(SalmonClipTable.LaneDown.Loop);
            Assert.IsFalse(SalmonClipTable.Jump.Loop);
        }

        [Test]
        public void DurationSourcesMatchConfig()
        {
            Assert.AreEqual(SalmonClipDuration.FromFrames, SalmonClipTable.Swim.DurationSource);
            Assert.AreEqual(SalmonClipDuration.LaneMove, SalmonClipTable.LaneUp.DurationSource);
            Assert.AreEqual(SalmonClipDuration.LaneMove, SalmonClipTable.LaneDown.DurationSource);
            Assert.AreEqual(SalmonClipDuration.Jump, SalmonClipTable.Jump.DurationSource);

            var config = AnimationAssetGenerator.LoadConfigValues();
            Assert.AreEqual(0f, AnimationAssetGenerator.ResolveDuration(SalmonClipTable.Swim, config), 1e-4f);
            Assert.AreEqual(config.LaneMoveDuration, AnimationAssetGenerator.ResolveDuration(SalmonClipTable.LaneUp, config), 1e-4f);
            Assert.AreEqual(config.LaneMoveDuration, AnimationAssetGenerator.ResolveDuration(SalmonClipTable.LaneDown, config), 1e-4f);
            Assert.AreEqual(config.JumpDuration, AnimationAssetGenerator.ResolveDuration(SalmonClipTable.Jump, config), 1e-4f);
        }

        [Test]
        public void FindReturnsDefinitionByName()
        {
            Assert.AreSame(SalmonClipTable.Jump, SalmonClipTable.Find("Player_Jump"));
            Assert.IsNull(SalmonClipTable.Find("Player_Idle"));
        }
    }

    public sealed class SalmonArtAssetTests
    {
        [Test]
        public void EveryFrameFileExists()
        {
            foreach (var definition in SalmonClipTable.All)
            {
                var paths = AnimationAssetGenerator.GetFramePaths(definition);
                Assert.AreEqual(definition.FrameCount, paths.Count);

                for (var i = 0; i < paths.Count; i++)
                {
                    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(paths[i]);
                    Assert.IsNotNull(sprite, paths[i] + " 스프라이트가 없다.");
                    Assert.AreEqual(AnimationAssetGenerator.PixelsPerUnit, sprite.pixelsPerUnit, 1e-3f, paths[i]);
                }
            }
        }

        [Test]
        public void FramePathsAreUniqueAndUnderArtFolder()
        {
            var paths = AnimationAssetGenerator.EnumerateFramePaths();
            Assert.AreEqual(SalmonClipTable.TotalFrameCount, paths.Count);

            var seen = new HashSet<string>();

            foreach (var path in paths)
            {
                Assert.IsTrue(seen.Add(path), path + "가 중복이다.");
                Assert.IsTrue(AnimationAssetGenerator.IsSalmonFramePath(path), path);
            }
        }

        [Test]
        public void SharedPivotIsBoundsCenter()
        {
            Assert.IsTrue(AnimationAssetGenerator.TryGetSalmonBounds(out var bounds, out var size));
            Assert.Greater(bounds.width, 0f);
            Assert.Greater(bounds.height, 0f);

            var pivot = AnimationAssetGenerator.ResolveSharedPivot();
            Assert.AreEqual(bounds.center.x / size.x, pivot.x, 1e-5f);
            Assert.AreEqual(bounds.center.y / size.y, pivot.y, 1e-5f);
        }

        [Test]
        public void EveryFrameShareOnePivot()
        {
            var pivot = AnimationAssetGenerator.ResolveSharedPivot();

            foreach (var path in AnimationAssetGenerator.EnumerateFramePaths())
            {
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                Assert.IsNotNull(importer, path + "의 TextureImporter가 없다.");

                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);

                Assert.AreEqual((int)SpriteAlignment.Custom, settings.spriteAlignment, path);
                Assert.AreEqual(AnimationAssetGenerator.PixelsPerUnit, settings.spritePixelsPerUnit, 1e-3f, path);
                Assert.AreEqual(pivot.x, settings.spritePivot.x, 1e-4f, path);
                Assert.AreEqual(pivot.y, settings.spritePivot.y, 1e-4f, path);
            }
        }

        [Test]
        public void GeneratedClipsMatchTheTable()
        {
            var config = AnimationAssetGenerator.LoadConfigValues();

            foreach (var definition in SalmonClipTable.All)
            {
                var clip = AssetDatabase.LoadAssetAtPath<CustomAnimation>(definition.AssetPath);
                Assert.IsNotNull(clip, definition.AssetPath + "가 없다. 'Team1004/Generate Animation Assets'를 돌린다.");
                Assert.AreEqual(definition.FrameCount, clip.FrameCount, definition.AssetName);
                Assert.AreEqual(definition.Loop, clip.Loop, definition.AssetName);
                Assert.AreEqual(definition.FramesPerSecond, clip.FramesPerSecond, 1e-3f, definition.AssetName);
                Assert.AreEqual(
                    AnimationAssetGenerator.ResolveDuration(definition, config),
                    clip.DurationOverride,
                    1e-4f,
                    definition.AssetName);
                Assert.IsTrue(clip.Validate(out var error), definition.AssetName + ": " + error);
            }
        }

        [Test]
        public void LegacyIdleClipIsGone()
        {
            Assert.IsNull(AssetDatabase.LoadAssetAtPath<CustomAnimation>(AnimationAssetGenerator.LegacyIdleClipPath));
        }
    }
}
