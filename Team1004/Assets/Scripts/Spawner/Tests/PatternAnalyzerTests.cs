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
            var log = Find(SpawnerDefaults.LogId);
            var impossible = new PatternSpec("Impossible_ThreeLogs", new[]
            {
                new PatternEntrySpec(log, 0, 0f),
                new PatternEntrySpec(log, 1, 0f),
                new PatternEntrySpec(log, 2, 0f)
            });

            var analysis = PatternAnalyzer.Analyze(impossible, config, 1f);
            Assert.IsTrue(analysis.IsValid, analysis.Error);
            Assert.IsFalse(analysis.Solvable, "Three logs block every lane longer than the jump and must be rejected.");
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
        public void AllLanesBlocked_WhileJumpOnCooldown_IsNotSurvivable()
        {
            var longCooldown = config.Clone();
            longCooldown.JumpCooldown = 6f;

            var pattern = FindPattern("Jump_RocksAllLanes");
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

        [Test]
        public void InvalidLane_IsReportedNotThrown()
        {
            var rock = Find(SpawnerDefaults.RockId);
            var invalid = new PatternSpec("Invalid_RockLane3", new[] { new PatternEntrySpec(rock, 3, 0f) });
            var analysis = PatternAnalyzer.Analyze(invalid, config, 1f);
            Assert.IsFalse(analysis.IsValid);
            Assert.IsFalse(analysis.Solvable);
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
