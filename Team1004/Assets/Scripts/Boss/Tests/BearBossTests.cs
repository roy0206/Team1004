using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Game.Boss.Tests
{
    public sealed class BearBossPatternTests
    {
        private BearBossData data;

        [SetUp]
        public void SetUp()
        {
            data = ScriptableObject.CreateInstance<BearBossData>();
            data.EnsureDefaultPatterns();
        }

        [TearDown]
        public void TearDown()
        {
            if (data != null)
                Object.DestroyImmediate(data);

            data = null;
        }

        [Test]
        public void DefaultPatternsMatchTheDesignTable()
        {
            var expected = new (string Label, int Mask, float Weight)[]
            {
                (BearBossData.SequenceLabel, 7, 25f),
                (BearBossData.Side1Label, 1, 15f),
                (BearBossData.Side2Label, 2, 15f),
                (BearBossData.Side3Label, 4, 15f),
                (BearBossData.Side12Label, 3, 15f),
                (BearBossData.Side23Label, 6, 15f)
            };

            Assert.AreEqual(expected.Length, data.Patterns.Count);

            var total = 0f;

            for (var i = 0; i < expected.Length; i++)
            {
                var pattern = data.Patterns[i];
                Assert.AreEqual(expected[i].Label, pattern.Label);
                Assert.AreEqual(expected[i].Mask, pattern.LaneMask);
                Assert.AreEqual(expected[i].Weight, pattern.Weight, 0.0001f);
                Assert.IsFalse(pattern.RequiresJump);
                total += pattern.Weight;
            }

            Assert.AreEqual(100f, total, 0.0001f);
        }

        [Test]
        public void NoSidePatternCoversAllThreeLanes()
        {
            for (var i = 0; i < data.Patterns.Count; i++)
            {
                var pattern = data.Patterns[i];

                if (BearBossData.IsSequenceLabel(pattern.Label))
                    continue;

                Assert.Less(pattern.LaneCount, 3, pattern.Label);
                Assert.AreNotEqual(5, pattern.LaneMask, pattern.Label);
            }
        }

        [Test]
        public void PickFrequenciesFollowTheWeights()
        {
            var selector = new BossPatternSelector(data.Patterns, 12345, true);
            var counts = new Dictionary<string, int>();
            const int rolls = 60000;

            for (var i = 0; i < rolls; i++)
            {
                Assert.IsTrue(selector.TryPick(out var pattern));
                counts.TryGetValue(pattern.Label, out var count);
                counts[pattern.Label] = count + 1;
            }

            for (var i = 0; i < data.Patterns.Count; i++)
            {
                var pattern = data.Patterns[i];
                counts.TryGetValue(pattern.Label, out var count);
                var ratio = count / (float)rolls;
                Assert.AreEqual(pattern.Weight / 100f, ratio, 0.02f, pattern.Label);
            }
        }

        [Test]
        public void ImmediateRepeatIsForbidden()
        {
            var selector = new BossPatternSelector(data.Patterns, 4242, false);
            BossPattern last = null;

            for (var i = 0; i < 500; i++)
            {
                Assert.IsTrue(selector.TryPick(out var pattern));

                if (last != null)
                    Assert.AreNotEqual(last.Label, pattern.Label);

                last = pattern;
            }
        }
    }

    public sealed class BearSequenceTimelineTests
    {
        private const float Interval = 0.5f;
        private const float Active = 0.18f;
        private const float ArmLead = 0.12f;
        private const float ImminentLead = 0.2f;

        private static BearSequenceTimeline Create()
        {
            return new BearSequenceTimeline(3, Interval, Active, ArmLead, ImminentLead);
        }

        [Test]
        public void DurationsMatchTheDesignNote()
        {
            var timeline = Create();

            Assert.AreEqual(0.5f, timeline.TelegraphDuration, 0.0001f);
            Assert.AreEqual(1.18f, timeline.AttackDuration, 0.0001f);
            Assert.AreEqual(1.68f, timeline.TotalDuration, 0.0001f);
        }

        [Test]
        public void WarnAndHitTimesAreHalfSecondApart()
        {
            var timeline = Create();

            Assert.AreEqual(0f, timeline.GetWarnTime(0), 0.0001f);
            Assert.AreEqual(0.5f, timeline.GetWarnTime(1), 0.0001f);
            Assert.AreEqual(1f, timeline.GetWarnTime(2), 0.0001f);

            Assert.AreEqual(0.5f, timeline.GetHitTime(0), 0.0001f);
            Assert.AreEqual(1f, timeline.GetHitTime(1), 0.0001f);
            Assert.AreEqual(1.5f, timeline.GetHitTime(2), 0.0001f);

            Assert.AreEqual(0.68f, timeline.GetHitEndTime(0), 0.0001f);
            Assert.AreEqual(1.18f, timeline.GetHitEndTime(1), 0.0001f);
            Assert.AreEqual(1.68f, timeline.GetHitEndTime(2), 0.0001f);
        }

        [Test]
        public void OnlyOneLaneIsLethalAtATime()
        {
            var timeline = Create();

            for (var time = 0f; time <= timeline.TotalDuration + 0.5f; time += 0.005f)
                Assert.LessOrEqual(BossLanes.Count(timeline.GetLethalMask(time)), 1, time.ToString());
        }

        [Test]
        public void LethalWindowsCoverExactlyTheHitTimes()
        {
            var timeline = Create();

            Assert.AreEqual(0, timeline.GetLethalMask(0.49f));
            Assert.AreEqual(BossLanes.Mask(0), timeline.GetLethalMask(0.5f));
            Assert.AreEqual(BossLanes.Mask(0), timeline.GetLethalMask(0.67f));
            Assert.AreEqual(0, timeline.GetLethalMask(0.68f));
            Assert.AreEqual(BossLanes.Mask(1), timeline.GetLethalMask(1.05f));
            Assert.AreEqual(0, timeline.GetLethalMask(1.2f));
            Assert.AreEqual(BossLanes.Mask(2), timeline.GetLethalMask(1.55f));
            Assert.AreEqual(0, timeline.GetLethalMask(1.68f));
        }

        [Test]
        public void VisibleMaskShowsTheNextWarningWhileTheCurrentLaneIsLethal()
        {
            var timeline = Create();

            Assert.AreEqual(BossLanes.Mask(0), timeline.GetVisibleMask(0.1f));
            Assert.AreEqual(BossLanes.Mask(0, 1), timeline.GetVisibleMask(0.55f));
            Assert.AreEqual(BossLanes.Mask(1), timeline.GetVisibleMask(0.7f));
            Assert.AreEqual(BossLanes.Mask(1, 2), timeline.GetVisibleMask(1.05f));
            Assert.AreEqual(BossLanes.Mask(2), timeline.GetVisibleMask(1.3f));
            Assert.AreEqual(0, timeline.GetVisibleMask(1.7f));
        }

        [Test]
        public void AdvanceRaisesEveryEventOnceInOrder()
        {
            var timeline = Create();
            var listener = new Recorder();

            for (var frame = 0; frame < 200; frame++)
                timeline.Advance(frame / 60f, listener);

            Assert.AreEqual(3, listener.ArmEnters.Count);
            Assert.AreEqual(3, listener.Imminents.Count);
            Assert.AreEqual(3, listener.HitBegins.Count);
            Assert.AreEqual(3, listener.HitEnds.Count);

            for (var step = 0; step < 3; step++)
            {
                Assert.AreEqual(step, listener.ArmEnters[step]);
                Assert.AreEqual(step, listener.Imminents[step]);
                Assert.AreEqual(step, listener.HitBegins[step].Step);
                Assert.AreEqual(step, listener.HitEnds[step].Step);
                Assert.AreEqual(BossLanes.Mask(step), listener.HitBegins[step].ArmedMask);
            }

            Assert.AreEqual(BossLanes.Mask(1), listener.HitBegins[0].WarnMask);
            Assert.AreEqual(BossLanes.Mask(2), listener.HitBegins[1].WarnMask);
            Assert.AreEqual(0, listener.HitBegins[2].WarnMask);
            Assert.AreEqual(0, listener.HitEnds[2].WarnMask);
        }

        [Test]
        public void ArmEnterLeadsEachHitByTheArmLead()
        {
            var timeline = Create();

            for (var step = 0; step < timeline.StepCount; step++)
                Assert.AreEqual(timeline.GetHitTime(step) - ArmLead, timeline.GetArmEnterTime(step), 0.0001f);
        }

        [Test]
        public void ResetReplaysEveryEvent()
        {
            var timeline = Create();
            var first = new Recorder();
            var second = new Recorder();

            for (var frame = 0; frame < 200; frame++)
                timeline.Advance(frame / 60f, first);

            timeline.Reset();

            for (var frame = 0; frame < 200; frame++)
                timeline.Advance(frame / 60f, second);

            Assert.AreEqual(first.HitBegins.Count, second.HitBegins.Count);
            Assert.AreEqual(first.HitEnds.Count, second.HitEnds.Count);
        }

        private sealed class Recorder : IBearSequenceListener
        {
            public List<int> ArmEnters { get; } = new();
            public List<int> Imminents { get; } = new();
            public List<(int Step, int WarnMask, int ArmedMask)> HitBegins { get; } = new();
            public List<(int Step, int WarnMask)> HitEnds { get; } = new();

            public void OnArmEnter(int step)
            {
                ArmEnters.Add(step);
            }

            public void OnImminent(int step)
            {
                Imminents.Add(step);
            }

            public void OnHitBegin(int step, int warnMask, int armedMask)
            {
                HitBegins.Add((step, warnMask, armedMask));
            }

            public void OnHitEnd(int step, int warnMask)
            {
                HitEnds.Add((step, warnMask));
            }
        }
    }
}
