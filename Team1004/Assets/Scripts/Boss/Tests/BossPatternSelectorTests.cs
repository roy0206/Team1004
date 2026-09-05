using System.Collections.Generic;
using NUnit.Framework;

namespace Game.Boss.Tests
{
    public sealed class BossPatternSelectorTests
    {
        private static List<BossPattern> ThreeLanes()
        {
            return new List<BossPattern>
            {
                new("Top", BossLanes.Mask(0)),
                new("Middle", BossLanes.Mask(1)),
                new("Bottom", BossLanes.Mask(2))
            };
        }

        [Test]
        public void EmptyListPicksNothing()
        {
            var selector = new BossPatternSelector(new List<BossPattern>(), 1, true);

            Assert.IsFalse(selector.TryPick(out var pattern));
            Assert.IsNull(pattern);
        }

        [Test]
        public void SameSeedGivesSameSequence()
        {
            var a = new BossPatternSelector(ThreeLanes(), 42, true);
            var b = new BossPatternSelector(ThreeLanes(), 42, true);

            for (var i = 0; i < 20; i++)
            {
                a.TryPick(out var pa);
                b.TryPick(out var pb);
                Assert.AreEqual(pa.Label, pb.Label);
            }
        }

        [Test]
        public void NoRepeatNeverPicksLastTwice()
        {
            var selector = new BossPatternSelector(ThreeLanes(), 7, false);
            BossPattern last = null;

            for (var i = 0; i < 50; i++)
            {
                Assert.IsTrue(selector.TryPick(out var pattern));

                if (last != null)
                    Assert.AreNotSame(last, pattern);

                last = pattern;
            }
        }

        [Test]
        public void NoRepeatFallsBackToLastWhenItIsTheOnlyCandidate()
        {
            var patterns = new List<BossPattern> { new("Only", BossLanes.Mask(1)) };
            var selector = new BossPatternSelector(patterns, 3, false);

            Assert.IsTrue(selector.TryPick(out var first));
            Assert.IsTrue(selector.TryPick(out var second));
            Assert.AreSame(first, second);
        }

        [Test]
        public void FilterExcludesPatterns()
        {
            var patterns = ThreeLanes();
            patterns.Add(new BossPattern("All", BossLanes.All(3), 1f, true));
            var selector = new BossPatternSelector(patterns, 11, true);

            for (var i = 0; i < 40; i++)
            {
                Assert.IsTrue(selector.TryPick(candidate => !candidate.RequiresJump, out var pattern));
                Assert.IsFalse(pattern.RequiresJump);
            }
        }

        [Test]
        public void ZeroWeightIsNeverPicked()
        {
            var patterns = new List<BossPattern>
            {
                new("Never", BossLanes.Mask(0), 0f),
                new("Always", BossLanes.Mask(1), 1f)
            };
            var selector = new BossPatternSelector(patterns, 5, true);

            for (var i = 0; i < 20; i++)
            {
                Assert.IsTrue(selector.TryPick(out var pattern));
                Assert.AreEqual("Always", pattern.Label);
            }
        }

        [Test]
        public void LaneHelpersDescribeMasks()
        {
            var mask = BossLanes.Mask(0, 2);

            Assert.AreEqual(2, BossLanes.Count(mask));
            Assert.IsTrue(BossLanes.Contains(mask, 0));
            Assert.IsFalse(BossLanes.Contains(mask, 1));
            Assert.AreEqual(0, BossLanes.First(mask));
            Assert.AreEqual(2, BossLanes.Last(mask));
            Assert.AreEqual(7, BossLanes.All(3));
        }
    }
}
