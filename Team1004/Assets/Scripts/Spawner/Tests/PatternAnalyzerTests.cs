using System.Collections.Generic;
using NUnit.Framework;

namespace Game.Spawner.Tests
{
    public sealed class PatternAnalyzerTests
    {
        private SimulationConfig config;
        private List<ObstacleSpec> obstacles;
        private List<PatternSpec> patterns;

        [SetUp]
        public void SetUp()
        {
            config = SpawnerDefaults.CreateConfig();
            obstacles = SpawnerDefaults.CreateObstacles();
            patterns = SpawnerDefaults.CreatePatterns(obstacles);
        }

        [Test]
        public void DefaultNormalPatterns_AreSolvableFromEveryLaneWithoutJump()
        {
            foreach (var pattern in patterns)
            {
                if (pattern.ManualJumpRequired)
                    continue;

                var analysis = PatternAnalyzer.Analyze(pattern, config, 1f);
                Assert.IsTrue(analysis.IsValid, pattern.Id + ": " + analysis.Error);
                Assert.IsTrue(analysis.SolvableWithoutJump, pattern.Id + " must be solvable without jumping from every lane.");
                Assert.IsFalse(analysis.JumpRequired, pattern.Id + " must not be classified as jump-required.");
            }
        }

        [Test]
        public void DefaultJumpPatterns_RequireJumpAndAreSolvable()
        {
            foreach (var pattern in patterns)
            {
                if (!pattern.ManualJumpRequired)
                    continue;

                var analysis = PatternAnalyzer.Analyze(pattern, config, 1f);
                Assert.IsTrue(analysis.Solvable, pattern.Id + " must be solvable with a jump.");
                Assert.IsTrue(analysis.JumpRequired, pattern.Id + " must be classified as jump-required.");
                Assert.IsFalse(analysis.ManualMismatch, pattern.Id + " manual mark must agree with the simulator.");
            }
        }

        [Test]
        public void DefaultPatterns_HaveReactionMargin()
        {
            foreach (var pattern in patterns)
            {
                var analysis = PatternAnalyzer.Analyze(pattern, config, 1f);
                Assert.GreaterOrEqual(analysis.ReactionMargin, 0.3f, pattern.Id + " reaction margin is too small.");
            }
        }

        [Test]
        public void EverySection_HasPatternsThatMeetItsReactionMarginTarget()
        {
            var profile = SpawnerDefaults.CreateProfile();
            var analyses = PatternAnalyzer.AnalyzeAll(patterns, config, 1f);

            for (var section = 0; section < profile.SectionCount; section++)
            {
                var required = profile.GetSection(section).Sample(1f).MinReactionMargin;
                var usable = 0;

                for (var i = 0; i < analyses.Count; i++)
                    if (analyses[i].Solvable && !analyses[i].JumpRequired && analyses[i].ReactionMargin >= required)
                        usable++;

                Assert.GreaterOrEqual(usable, 8,
                    "Section " + section + " has too few normal patterns above the " + required + "s reaction margin target.");
            }
        }

        [Test]
        public void DefaultPatterns_StaySolvableAtTopSpeed()
        {
            var profile = SpawnerDefaults.CreateProfile();
            var maxSpeed = 0f;
            for (var i = 0; i < profile.SectionCount; i++)
                maxSpeed = System.Math.Max(maxSpeed, profile.GetSection(i).MaxSpeedMultiplier);

            foreach (var pattern in patterns)
            {
                var analysis = PatternAnalyzer.Analyze(pattern, config, maxSpeed);
                Assert.IsTrue(analysis.Solvable, pattern.Id + " must stay solvable at speed x" + maxSpeed);
            }
        }

        [Test]
        public void AllLanesBlockedLongerThanJump_IsRejected()
        {
            var barrier = new ObstacleSpec("Barrier", "Barrier", 1, LaneMask.All(3), 8f, 7f, 0.39f, 1f, 1f, true, true);
            var impossible = new PatternSpec("Impossible_ThreeBarriers", new[]
            {
                new PatternEntrySpec(barrier, 0, 0f),
                new PatternEntrySpec(barrier, 1, 0f),
                new PatternEntrySpec(barrier, 2, 0f)
            });

            var analysis = PatternAnalyzer.Analyze(impossible, config, 1f);
            Assert.IsTrue(analysis.IsValid, analysis.Error);
            Assert.IsFalse(analysis.Solvable, "Three barriers block every lane longer than the jump and must be rejected.");
        }

        [Test]
        public void DefaultNormalPatterns_NeverBlockEveryLaneAtOnce()
        {
            foreach (var pattern in patterns)
            {
                if (pattern.ManualJumpRequired)
                    continue;

                var analysis = PatternAnalyzer.Analyze(pattern, config, 1f);
                Assert.IsTrue(analysis.SolvableWithoutJump,
                    pattern.Id + " blocks every lane at once. Normal sections must never force a jump (design v4).");
            }
        }

        [Test]
        public void DefaultProfile_NeverRequestsJumpPatterns()
        {
            var profile = SpawnerDefaults.CreateProfile();

            for (var section = 0; section < profile.SectionCount; section++)
            {
                for (var step = 0; step <= 10; step++)
                {
                    var sample = profile.GetSection(section).Sample(step / 10f);
                    Assert.AreEqual(0f, sample.JumpRequiredRatio, 0.0001f,
                        "Section " + section + " asks for jump-required patterns. Jump forcing belongs to boss 3 only (design v4).");
                }
            }
        }

        [Test]
        public void JumpDuration_MatchesDesignV4()
        {
            Assert.AreEqual(SpawnerDefaults.JumpDurationSeconds, config.JumpDuration, 0.0001f,
                "The simulator must model the design v4 airborne time.");
            Assert.AreEqual(1.6f, SpawnerDefaults.JumpDurationSeconds, 0.0001f);
        }

        [Test]
        public void DefaultObstacles_AreAllSingleLane()
        {
            foreach (var obstacle in obstacles)
                Assert.AreEqual(1, obstacle.LaneSpan, obstacle.Id + " must occupy one lane (design v3, section 10).");
        }

        [Test]
        public void DefaultObstacles_HitboxIsTenToFifteenPercentSmallerThanBody()
        {
            foreach (var obstacle in obstacles)
            {
                var ratio = obstacle.CollisionLength / obstacle.BodyLength;
                Assert.GreaterOrEqual(ratio, 0.85f - 0.001f, obstacle.Id + " hitbox is more than 15% smaller than the sprite.");
                Assert.LessOrEqual(ratio, 0.90f + 0.001f, obstacle.Id + " hitbox is less than 10% smaller than the sprite (design v4, checklist G).");
            }
        }

        [Test]
        public void Rock_IsABoulderThatFillsTheBottomLane()
        {
            var rock = Find(SpawnerDefaults.RockId);

            Assert.AreEqual(ObstacleFitAxis.Height, rock.FitAxis);
            Assert.AreEqual(SpawnerDefaults.LaneSpacing, rock.VisualHeight, 0.0001f, "바위는 레인 높이를 꽉 채운다.");
            Assert.AreEqual(2.44f, rock.BodyLength, 0.0001f);
            Assert.AreEqual(2.13f, rock.CollisionLength, 0.0001f);
            Assert.AreEqual(0.96f, rock.CollisionHeight, 0.0001f);
            Assert.AreEqual(1f, rock.SpeedMultiplier, 0.0001f);
            Assert.AreEqual(5f, rock.SpawnWeight, 0.0001f);
        }

        [Test]
        public void Rock_StartsOnlyInTheBottomLane()
        {
            var rock = Find(SpawnerDefaults.RockId);

            Assert.AreEqual(LaneMask.Of((int)Lane.Bottom), rock.AllowedStartLaneMask,
                "돌은 맨 아랫줄에만 나온다(기획자 답변 3).");
            Assert.IsFalse(rock.CanStartAt((int)Lane.Top, 3));
            Assert.IsFalse(rock.CanStartAt((int)Lane.Middle, 3));
            Assert.IsTrue(rock.CanStartAt((int)Lane.Bottom, 3));
        }

        [Test]
        public void FishAndLog_StartInEveryLane()
        {
            foreach (var id in new[] { SpawnerDefaults.FishId, SpawnerDefaults.LogId })
            {
                var obstacle = Find(id);
                Assert.AreEqual(LaneMask.All(3), obstacle.AllowedStartLaneMask, id + " must be allowed in all three lanes.");
            }
        }

        [Test]
        public void EveryDefaultPattern_ObeysTheLaneRule()
        {
            foreach (var pattern in patterns)
                Assert.IsTrue(pattern.Validate(3, out var error), error);
        }

        [Test]
        public void RockAboveTheBottomLane_IsReportedNotThrown()
        {
            var rock = Find(SpawnerDefaults.RockId);

            foreach (var lane in new[] { 0, 1, 3 })
            {
                var invalid = new PatternSpec("Invalid_RockLane" + lane, new[] { new PatternEntrySpec(rock, lane, 0f) });
                var analysis = PatternAnalyzer.Analyze(invalid, config, 1f);
                Assert.IsFalse(analysis.IsValid, "Rock at lane " + lane + " must be reported as invalid.");
                Assert.IsFalse(analysis.Solvable);
            }
        }

        [Test]
        public void DefaultObstacles_CollisionHeightComesFromTheArtHeight()
        {
            var expected = new Dictionary<string, float>
            {
                { SpawnerDefaults.RockId, SpawnerDefaults.RockVisualHeight * ObstacleVisual.HitboxScale },
                { SpawnerDefaults.FishId, 0.45f * ObstacleVisual.HitboxScale },
                { SpawnerDefaults.LogId, 0.97627f * ObstacleVisual.HitboxScale }
            };

            foreach (var obstacle in obstacles)
                Assert.AreEqual(expected[obstacle.Id], obstacle.CollisionHeight, 0.006f,
                    obstacle.Id + " 충돌 높이는 아트 높이 × " + ObstacleVisual.HitboxScale + "다.");
        }

        [Test]
        public void DefaultObstacles_NeverReachTheNeighbourLane()
        {
            foreach (var obstacle in obstacles)
                Assert.IsFalse(
                    obstacle.ReachesNeighbourLane(SpawnerDefaults.LaneSpacing, SpawnerDefaults.PlayerHitboxHeight),
                    obstacle.Id + " 충돌 상자가 이웃 레인의 플레이어에 닿는다. 시뮬레이터의 이산 레인 가정이 깨진다.");
        }

        [Test]
        public void DefaultObstacles_KeepTheDesignSpawnWeights()
        {
            Assert.AreEqual(SpawnerDefaults.RockSpawnWeight, Find(SpawnerDefaults.RockId).SpawnWeight, 0.0001f);
            Assert.AreEqual(SpawnerDefaults.FishSpawnWeight, Find(SpawnerDefaults.FishId).SpawnWeight, 0.0001f);
            Assert.AreEqual(SpawnerDefaults.LogSpawnWeight, Find(SpawnerDefaults.LogId).SpawnWeight, 0.0001f);
            Assert.AreEqual(5f, SpawnerDefaults.RockSpawnWeight, 0.0001f);
            Assert.AreEqual(3f, SpawnerDefaults.FishSpawnWeight, 0.0001f);
            Assert.AreEqual(2f, SpawnerDefaults.LogSpawnWeight, 0.0001f);
        }

        [Test]
        public void ObstacleMix_TargetsMatchTheSpawnWeights()
        {
            var mix = ObstacleMix.FromPatterns(patterns);
            Assert.AreEqual(3, mix.TypeCount);

            var sum = 0f;
            for (var i = 0; i < mix.TypeCount; i++)
                sum += mix.TargetShare(i);

            Assert.AreEqual(1f, sum, 0.0001f);

            for (var i = 0; i < mix.TypeCount; i++)
            {
                var expected = mix.GetId(i) == SpawnerDefaults.RockId ? 0.5f : mix.GetId(i) == SpawnerDefaults.FishId ? 0.3f : 0.2f;
                Assert.AreEqual(expected, mix.TargetShare(i), 0.0001f, mix.GetId(i) + " target share");
            }
        }

        [Test]
        public void ObstacleMix_BoostsTheTypeThatIsBehind()
        {
            var mix = ObstacleMix.FromPatterns(patterns);
            var fishOnly = FindPattern("Fish_Top");
            var rockOnly = FindPattern("Rock_Bottom");

            Assert.AreEqual(1f, mix.Multiplier(rockOnly), 0.0001f, "빈 구간에서는 보정이 없다.");

            var placement = PlacementBuilder.Build(fishOnly, config, 1f, 0f, 3f, false, 1);
            for (var i = 0; i < 6; i++)
                mix.Record(placement);

            Assert.Greater(mix.Multiplier(rockOnly), 1f, "물고기만 나온 뒤에는 돌 패턴 가중치가 올라야 한다.");
            Assert.Less(mix.Multiplier(fishOnly), 1f, "이미 많이 나온 종류는 가중치가 내려야 한다.");
        }

        [Test]
        public void AllLanesBlocked_WhileJumpOnCooldown_IsNotSurvivable()
        {
            var longCooldown = config.Clone();
            longCooldown.JumpCooldown = 6f;

            var pattern = FindPattern("Jump_FishTop_FishMiddle_RockBottom");
            var arrival = PlacementBuilder.EarliestArrivalDelay(pattern, longCooldown, 1f);
            var placement = PlacementBuilder.Build(pattern, longCooldown, 1f, 0f, arrival, true, 3);
            var layout = StateLayout.From(longCooldown);
            var endTick = BlockTimeline.LastBlockTick(placement.Obstacles, longCooldown) + 2;
            var timeline = BlockTimeline.Build(placement.Obstacles, null, longCooldown, 0, endTick);

            var onCooldown = new StateSet(layout);
            onCooldown.Add(layout.TopLane, 0, layout.CooldownTicks);
            Reachability.Propagate(onCooldown, 0, endTick, timeline, PropagationOptions.Free);
            Assert.IsTrue(onCooldown.IsEmpty, "A player on a long jump cooldown cannot survive a fully blocked pattern.");

            var ready = new StateSet(layout);
            ready.Add(layout.TopLane, 0, 0);
            Reachability.Propagate(ready, 0, endTick, timeline, PropagationOptions.Free);
            Assert.IsFalse(ready.IsEmpty, "A player with the jump ready survives by jumping.");
        }

        private ObstacleSpec Find(string id)
        {
            foreach (var obstacle in obstacles)
                if (obstacle.Id == id)
                    return obstacle;

            Assert.Fail("Obstacle not found: " + id);
            return null;
        }

        private PatternSpec FindPattern(string id)
        {
            foreach (var pattern in patterns)
                if (pattern.Id == id)
                    return pattern;

            Assert.Fail("Pattern not found: " + id);
            return null;
        }
    }
}
