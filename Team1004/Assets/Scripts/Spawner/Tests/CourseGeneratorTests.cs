using System.Collections.Generic;
using NUnit.Framework;

namespace Game.Spawner.Tests
{
    public sealed class CourseGeneratorTests
    {
        private const int SeedsPerSection = 13;

        private SimulationConfig config;
        private List<PatternSpec> patterns;
        private List<PatternAnalysis> analyses;
        private DifficultyProfile profile;
        private CourseRunner runner;

        [SetUp]
        public void SetUp()
        {
            config = SpawnerDefaults.CreateConfig();
            patterns = SpawnerDefaults.CreatePatterns(SpawnerDefaults.CreateObstacles());
            analyses = PatternAnalyzer.AnalyzeAll(patterns, config, 1f);
            profile = SpawnerDefaults.CreateProfile();
            runner = new CourseRunner(config, patterns, analyses, profile);
        }

        [Test]
        public void GeneratedCourses_AreSurvivableForEverySectionAndSeed()
        {
            for (var section = 0; section < SpawnerDefaults.SectionCount; section++)
            {
                for (var seed = 1; seed <= SeedsPerSection; seed++)
                {
                    var result = runner.RunSeconds(section, SpawnerDefaults.GetSectionDuration(section), seed);
                    Assert.IsTrue(result.Survivable, "Section " + section + " seed " + seed + " has no surviving path.");

                    if (section < SpawnerDefaults.SectionCount - 1)
                        Assert.Greater(result.Placements.Count, 0, "Section " + section + " seed " + seed + " spawned nothing.");
                }
            }
        }

        [Test]
        public void JumpRequiredPatterns_FollowSafetyRules()
        {
            for (var section = 0; section < SpawnerDefaults.SectionCount; section++)
            {
                for (var seed = 1; seed <= SeedsPerSection; seed++)
                {
                    var result = runner.RunSeconds(section, SpawnerDefaults.GetSectionDuration(section), seed);
                    PatternPlacement previous = null;
                    PatternPlacement previousJump = null;

                    foreach (var placement in result.Placements)
                    {
                        if (placement.IsJumpRequired)
                        {
                            Assert.IsTrue(previous == null || !previous.IsJumpRequired,
                                "Section " + section + " seed " + seed + ": two jump-required patterns in a row.");

                            if (previousJump != null)
                                Assert.GreaterOrEqual(placement.FirstBlockStart - previousJump.LastBlockEnd, config.MinJumpPatternGap - 0.001f,
                                    "Section " + section + " seed " + seed + ": jump-required patterns are too close.");

                            previousJump = placement;
                        }

                        previous = placement;
                    }
                }
            }
        }

        [Test]
        public void SameSeed_ProducesSameCourse()
        {
            for (var section = 0; section < SpawnerDefaults.SectionCount; section++)
            {
                var first = runner.RunSeconds(section, SpawnerDefaults.GetSectionDuration(section), 777);
                var second = runner.RunSeconds(section, SpawnerDefaults.GetSectionDuration(section), 777);

                Assert.AreEqual(first.Placements.Count, second.Placements.Count, "Section " + section + " placement counts differ.");

                for (var i = 0; i < first.Placements.Count; i++)
                {
                    Assert.AreEqual(first.Placements[i].Pattern.Id, second.Placements[i].Pattern.Id);
                    Assert.AreEqual(first.Placements[i].SpawnTime, second.Placements[i].SpawnTime, 0.0001f);
                    Assert.AreEqual(first.Placements[i].ArrivalTime, second.Placements[i].ArrivalTime, 0.0001f);
                }
            }
        }

        [Test]
        public void DifferentSeeds_ProduceDifferentCourses()
        {
            var a = runner.RunSeconds(1, SpawnerDefaults.GetSectionDuration(1), 1);
            var b = runner.RunSeconds(1, SpawnerDefaults.GetSectionDuration(1), 2);
            var same = a.Placements.Count == b.Placements.Count;

            if (same)
                for (var i = 0; i < a.Placements.Count; i++)
                    if (a.Placements[i].Pattern.Id != b.Placements[i].Pattern.Id)
                    {
                        same = false;
                        break;
                    }

            Assert.IsFalse(same, "Different seeds produced an identical course.");
        }

        [Test]
        public void FinalSection_StopsSpawningBeforeTheEnd()
        {
            var lastSection = SpawnerDefaults.SectionCount - 1;
            var result = runner.RunSeconds(lastSection, SpawnerDefaults.GetSectionDuration(lastSection), 5);
            var stop = profile.GetSection(lastSection).SpawnStopProgress;
            var stopTime = result.DurationSeconds * stop;

            foreach (var placement in result.Placements)
                Assert.LessOrEqual(placement.SpawnTime, stopTime + 0.001f, "Final section spawned after the stop progress.");
        }

        [Test]
        public void FirstPattern_RespectsStartGraceFromEveryStartState()
        {
            Assert.GreaterOrEqual(config.SectionStartGrace, 1.5f, "Section start grace must be at least 1.5 seconds.");

            for (var section = 0; section < SpawnerDefaults.SectionCount; section++)
            {
                for (var seed = 1; seed <= SeedsPerSection; seed++)
                {
                    var generator = new CourseGenerator(config, patterns, analyses, profile, seed);
                    var layout = generator.Layout;
                    var lockOptions = layout.MoveTicks > 1 ? new[] { 0, layout.MoveTicks - 1 } : new[] { 0 };

                    for (var lane = 0; lane < layout.LaneCount; lane++)
                    {
                        foreach (var lockTicks in lockOptions)
                        {
                            generator.BeginSection(section);
                            var frontier = new StateSet(layout);
                            frontier.Add(lane, lockTicks, 0);

                            var placed = generator.TryPlaceNext(0f, 0f, frontier, out var placement, out var reason);
                            var label = "section " + section + " seed " + seed + " lane " + lane + " lock " + lockTicks;
                            Assert.IsTrue(placed, label + ": first pattern was rejected (" + reason + ").");
                            Assert.GreaterOrEqual(placement.FirstBlockStart, config.SectionStartGrace - 0.001f,
                                label + ": first obstacle arrives before the section start grace.");

                            var sample = generator.SampleDifficulty(0f);
                            var endTick = BlockTimeline.LastBlockTick(placement.Obstacles, config) + 2;
                            var timeline = BlockTimeline.Build(placement.Obstacles, null, config, 0, endTick);
                            var freezeTo = config.TimeToTick(placement.LastAppearTime) + config.DurationToTicks(sample.MinReactionMargin);
                            var check = new StateSet(layout);
                            check.Add(lane, lockTicks, 0);
                            Reachability.Propagate(check, 0, endTick, timeline, PropagationOptions.Frozen(0, freezeTo, true));
                            Assert.IsFalse(check.IsEmpty, label + ": first pattern is not survivable with the reaction margin.");
                        }
                    }
                }
            }
        }

        [Test]
        public void ZeroJumpChance_NeverSpawnsJumpRequiredPatterns()
        {
            var sections = new List<SectionProfile>();

            for (var i = 0; i < SpawnerDefaults.SectionCount; i++)
            {
                SpawnerDefaults.GetSectionEndpoints(i, out var start, out var end, out var stop);
                sections.Add(SectionProfile.Linear(WithoutJump(start), WithoutJump(end), stop));
            }

            var noJumpRunner = new CourseRunner(config, patterns, analyses, new DifficultyProfile(sections));

            for (var section = 0; section < SpawnerDefaults.SectionCount; section++)
            {
                for (var seed = 1; seed <= SeedsPerSection; seed++)
                {
                    var result = noJumpRunner.RunSeconds(section, SpawnerDefaults.GetSectionDuration(section), seed);
                    Assert.IsTrue(result.Survivable, "Section " + section + " seed " + seed + " without jump patterns is not survivable.");
                    Assert.AreEqual(0, result.JumpRequiredCount, "Section " + section + " seed " + seed + " spawned a jump-required pattern with jumpRequiredRatio 0.");
                    Assert.Greater(result.Placements.Count, 0, "Section " + section + " seed " + seed + " spawned nothing.");
                }
            }
        }

        [Test]
        public void DefaultProfile_HasNoSectionSpeedMultiplier()
        {
            for (var section = 0; section < profile.SectionCount; section++)
            {
                var sectionProfile = profile.GetSection(section);

                for (var i = 0; i <= 10; i++)
                    Assert.AreEqual(1f, sectionProfile.Sample(i / 10f).SpeedMultiplier, 0.0001f,
                        "Section " + section + " must not scale the obstacle approach speed (기획자 답변 4).");
            }
        }

        [Test]
        public void DefaultProfile_UsesTheDesignReactionMargins()
        {
            var expected = new[] { 1.3f, 1.0f, 0.8f, 1.3f };

            for (var section = 0; section < expected.Length; section++)
            {
                var sectionProfile = profile.GetSection(section);

                for (var i = 0; i <= 10; i++)
                    Assert.AreEqual(expected[section], sectionProfile.Sample(i / 10f).MinReactionMargin, 0.0001f,
                        "Section " + section + " reaction margin target (기획자 답변 5).");
            }
        }

        [Test]
        public void GeneratedCourses_ApproachTheDesignObstacleRatio()
        {
            var ids = new[] { SpawnerDefaults.RockId, SpawnerDefaults.FishId, SpawnerDefaults.LogId };
            var targets = new[] { 0.5f, 0.3f, 0.2f };
            var counts = new int[ids.Length];
            var total = 0;

            for (var section = 0; section < SpawnerDefaults.SectionCount; section++)
            {
                for (var seed = 1; seed <= SeedsPerSection; seed++)
                {
                    var result = runner.RunSeconds(section, SpawnerDefaults.GetSectionDuration(section), seed);

                    foreach (var placement in result.Placements)
                    {
                        foreach (var obstacle in placement.Obstacles)
                        {
                            for (var i = 0; i < ids.Length; i++)
                            {
                                if (obstacle.Spec.Id != ids[i])
                                    continue;

                                counts[i]++;
                                total++;
                            }
                        }
                    }
                }
            }

            Assert.Greater(total, 0, "No obstacle was spawned.");

            for (var i = 0; i < ids.Length; i++)
                Assert.AreEqual(targets[i], counts[i] / (float)total, 0.07f,
                    ids[i] + " 등장 비율이 돌 5 : 물고기 3 : 통나무 2에서 벗어났다 (" + counts[i] + "/" + total + ").");
        }

        [Test]
        public void GeneratedCourses_OnlyPutRocksInTheBottomLane()
        {
            for (var section = 0; section < SpawnerDefaults.SectionCount; section++)
            {
                for (var seed = 1; seed <= SeedsPerSection; seed++)
                {
                    var result = runner.RunSeconds(section, SpawnerDefaults.GetSectionDuration(section), seed);

                    foreach (var placement in result.Placements)
                        foreach (var obstacle in placement.Obstacles)
                            if (obstacle.Spec.Id == SpawnerDefaults.RockId)
                                Assert.AreEqual((int)Lane.Bottom, obstacle.StartLane,
                                    "Section " + section + " seed " + seed + ": 돌이 맨 아랫줄이 아닌 곳에 나왔다.");
                }
            }
        }

        [Test]
        public void DefaultGapCurves_StayInsideDesignCandidateRanges()
        {
            var ranges = new[] { new[] { 2.0f, 2.5f }, new[] { 1.6f, 2.0f }, new[] { 1.3f, 1.7f }, new[] { 2.5f, 3.0f } };

            for (var section = 0; section < ranges.Length; section++)
            {
                var sectionProfile = profile.GetSection(section);
                for (var i = 0; i <= 10; i++)
                {
                    var gap = sectionProfile.Sample(i / 10f).PatternGap;
                    Assert.GreaterOrEqual(gap, ranges[section][0] - 0.001f, "Section " + section + " gap below the design candidate range.");
                    Assert.LessOrEqual(gap, ranges[section][1] + 0.001f, "Section " + section + " gap above the design candidate range.");
                }
            }
        }

        private static DifficultySample WithoutJump(in DifficultySample sample)
        {
            return new DifficultySample(
                sample.SpeedMultiplier, sample.PatternGap, sample.MinReactionMargin, 0f,
                sample.Tier1Weight, sample.Tier2Weight, sample.Tier3Weight, sample.SpawnEnabled);
        }

        [Test]
        public void Generator_RejectsCandidatesThePlayerCannotSurvive()
        {
            var generator = new CourseGenerator(config, patterns, analyses, profile, 3);
            generator.BeginSection(2);

            var layout = generator.Layout;
            var frontier = new StateSet(layout);
            frontier.Add(layout.TopLane, 0, layout.CooldownTicks);

            var placed = generator.TryPlaceNext(0f, 0.5f, frontier, out var placement, out _);
            Assert.IsTrue(placed, "A normal pattern must be available even while the jump is on cooldown.");
            Assert.IsFalse(placement.IsJumpRequired, "Jump-required patterns must not spawn while the player is on cooldown.");
        }
    }
}
