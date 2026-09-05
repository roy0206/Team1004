using System;
using NUnit.Framework;
using UnityEngine;

namespace Game.Environment.Tests
{
    public sealed class TileStripTests
    {
        private const float TileWidth = 6.4f;
        private const int TileCount = 4;
        private const float Total = TileWidth * TileCount;

        [Test]
        public void TotalWidthIsZeroForInvalidStrip()
        {
            Assert.AreEqual(0f, TileStrip.TotalWidth(0f, 4));
            Assert.AreEqual(0f, TileStrip.TotalWidth(6.4f, 0));
            Assert.AreEqual(0f, TileStrip.TotalWidth(-1f, 4));
        }

        [Test]
        public void WrapStaysInsideRange()
        {
            var values = new[] { -100f, -Total, -0.0001f, 0f, 0.5f, Total - 0.0001f, Total, Total + 0.5f, 1000f };

            foreach (var value in values)
            {
                var wrapped = TileStrip.Wrap(value, Total);
                Assert.GreaterOrEqual(wrapped, 0f, $"value {value}");
                Assert.Less(wrapped, Total, $"value {value}");
            }
        }

        [Test]
        public void WrapAtExactTotalReturnsZero()
        {
            Assert.AreEqual(0f, TileStrip.Wrap(Total, Total), 1e-4f);
            Assert.AreEqual(0f, TileStrip.Wrap(Total * 3f, Total), 1e-4f);
        }

        [Test]
        public void AdvanceDoesNotAccumulateError()
        {
            const float speed = 4f;
            const float delta = 1f / 60f;
            const int steps = 6000;

            var offset = 0f;

            for (var i = 0; i < steps; i++)
                offset = TileStrip.Advance(offset, speed * delta, Total);

            var expected = TileStrip.Wrap(speed * delta * steps, Total);
            var error = Mathf.Abs(offset - expected);
            error = Mathf.Min(error, Total - error);

            Assert.Less(error, 0.02f, $"offset {offset} expected {expected}");
        }

        [Test]
        public void TilesKeepUniformSpacingForAnyOffset()
        {
            var offsets = new[] { 0f, 0.37f, TileWidth * 0.5f, TileWidth, TileWidth * 2.9f, Total - 0.001f };

            foreach (var offset in offsets)
            {
                var positions = new float[TileCount];

                for (var i = 0; i < TileCount; i++)
                    positions[i] = TileStrip.TileX(TileWidth, TileCount, offset, i);

                Array.Sort(positions);

                for (var i = 1; i < TileCount; i++)
                    Assert.AreEqual(TileWidth, positions[i] - positions[i - 1], 1e-3f, $"offset {offset}");
            }
        }

        [Test]
        public void StripStaysGaplessAndCoversTheScreen()
        {
            const float viewWidth = 12.8f;

            var offset = 0f;
            var edges = new float[TileCount];

            for (var step = 0; step < 400; step++)
            {
                for (var i = 0; i < TileCount; i++)
                    edges[i] = TileStrip.TileX(TileWidth, TileCount, offset, i) - TileWidth * 0.5f;

                Array.Sort(edges);

                for (var i = 1; i < TileCount; i++)
                    Assert.AreEqual(TileWidth, edges[i] - edges[i - 1], 1e-3f, $"offset {offset}");

                Assert.LessOrEqual(edges[0], -viewWidth * 0.5f + 1e-3f, $"offset {offset}");
                Assert.GreaterOrEqual(edges[TileCount - 1] + TileWidth, viewWidth * 0.5f - 1e-3f, $"offset {offset}");

                offset = TileStrip.Advance(offset, 0.17f, Total);
            }
        }

        [Test]
        public void TileMovesLeftAndWrapsByTotalWidth()
        {
            const float step = 0.25f;

            var previous = TileStrip.TileX(TileWidth, TileCount, 0f, 0);
            var offset = 0f;
            var wraps = 0;

            for (var i = 0; i < (int)(Total / step) + 4; i++)
            {
                offset = TileStrip.Advance(offset, step, Total);
                var current = TileStrip.TileX(TileWidth, TileCount, offset, 0);

                if (current > previous)
                {
                    Assert.AreEqual(Total - step, current - previous, 1e-3f);
                    wraps++;
                }
                else
                {
                    Assert.AreEqual(-step, current - previous, 1e-3f);
                }

                previous = current;
            }

            Assert.GreaterOrEqual(wraps, 1);
        }

        [Test]
        public void TileWrapsToTheRightEndOfTheStrip()
        {
            var origin = TileStrip.OriginX(TileWidth, TileCount);

            var beforeWrap = TileStrip.TileX(TileWidth, TileCount, 0.0001f, 0);
            var afterWrap = TileStrip.TileX(TileWidth, TileCount, 0f, 0);

            Assert.AreEqual(origin + Total - 0.0001f + TileWidth * 0.5f, beforeWrap, 1e-3f);
            Assert.AreEqual(origin + TileWidth * 0.5f, afterWrap, 1e-3f);
        }

        [Test]
        public void MinimumTileCountHidesTheWrapOffScreen()
        {
            const float viewWidth = 12.8f;

            foreach (var width in new[] { 4.8f, 6.4f, 8f })
            {
                var count = TileStrip.MinimumTileCount(width, viewWidth);
                Assert.IsTrue(TileStrip.CoversView(width, count, viewWidth), $"width {width} count {count}");
                Assert.IsFalse(TileStrip.CoversView(width, count - 1, viewWidth), $"width {width} count {count - 1}");

                var origin = TileStrip.OriginX(width, count);
                Assert.LessOrEqual(origin + width, -viewWidth * 0.5f + 1e-3f, $"width {width}");
            }
        }

        [Test]
        public void EmptyStripReturnsZero()
        {
            Assert.AreEqual(0f, TileStrip.TileX(6.4f, 0, 3f, 0));
            Assert.AreEqual(0f, TileStrip.Advance(2f, 1f, 0f));
        }
    }
}
