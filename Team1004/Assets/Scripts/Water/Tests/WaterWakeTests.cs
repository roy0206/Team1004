using NUnit.Framework;
using UnityEngine;

namespace Game.Water.Tests
{
    public sealed class WaterWakeTests
    {
        private const float Transfer = 0.15f;
        private const float Influence = 1f;
        private const float SurfaceY = 2f;

        private const float RockMinX = -0.5f;
        private const float RockMaxX = 0.5f;
        private const float RockTopMinY = 0.875f;
        private const float RockTopMaxY = 1.325f;

        private static float Node(float velocity, float nodeX, float nodeY, float horizontal)
        {
            return WaterWake.NodeVelocity(
                velocity, nodeX, nodeY,
                RockMinX, RockMaxX, RockTopMinY, RockTopMaxY,
                horizontal, Influence, Transfer);
        }

        [Test]
        public void ProximityIsFullInsideTheVerticalSpan()
        {
            Assert.AreEqual(1f, WaterWake.Proximity(1f, 0.875f, 1.325f, 1f), 1e-4f);
            Assert.AreEqual(1f, WaterWake.Proximity(1.325f, 0.875f, 1.325f, 1f), 1e-4f);
        }

        [Test]
        public void ProximityFadesLinearlyAboveTheTop()
        {
            Assert.AreEqual(0.5f, WaterWake.Proximity(1.825f, 0.875f, 1.325f, 1f), 1e-4f);
            Assert.AreEqual(0.325f, WaterWake.Proximity(SurfaceY, RockTopMinY, RockTopMaxY, 1f), 1e-4f);
        }

        [Test]
        public void ProximityIsZeroBeyondInfluence()
        {
            Assert.AreEqual(0f, WaterWake.Proximity(2.325f, 0.875f, 1.325f, 1f), 1e-4f);
            Assert.AreEqual(0f, WaterWake.Proximity(3f, 0.875f, 1.325f, 1f), 1e-4f);
        }

        [Test]
        public void ProximityIsZeroWhenTheSurfaceIsBelowTheBody()
        {
            Assert.AreEqual(0f, WaterWake.Proximity(0.5f, 0.875f, 1.325f, 1f), 1e-4f);
        }

        [Test]
        public void MiddleLaneIsOutOfReach()
        {
            var velocity = WaterWake.NodeVelocity(0f, 0f, SurfaceY, -0.5f, 0.5f, -0.225f, 0.225f, -4f, Influence, Transfer);

            Assert.AreEqual(0f, velocity, 1e-6f);
        }

        [Test]
        public void ShapeLiftsAheadOfTheLeadingEdgeAndPushesDownBehind()
        {
            Assert.AreEqual(-WaterWake.BowLift, WaterWake.Shape(RockMinX, RockMinX, RockMaxX, -4f), 1e-4f);
            Assert.AreEqual(0f, WaterWake.Shape(-0.25f, RockMinX, RockMaxX, -4f), 1e-4f);
            Assert.AreEqual(1f, WaterWake.Shape(RockMaxX, RockMinX, RockMaxX, -4f), 1e-4f);
        }

        [Test]
        public void ShapeFollowsTheTravelDirection()
        {
            Assert.AreEqual(-WaterWake.BowLift, WaterWake.Shape(RockMaxX, RockMinX, RockMaxX, 4f), 1e-4f);
            Assert.AreEqual(1f, WaterWake.Shape(RockMinX, RockMinX, RockMaxX, 4f), 1e-4f);
        }

        [Test]
        public void TrailingEdgePushesTheNodeDown()
        {
            Assert.AreEqual(-0.195f, Node(0f, RockMaxX, SurfaceY, -4f), 1e-4f);
        }

        [Test]
        public void LeadingEdgeLiftsTheNode()
        {
            Assert.AreEqual(0.06825f, Node(0f, RockMinX, SurfaceY, -4f), 1e-4f);
        }

        [Test]
        public void NodesOutsideTheHorizontalSpanAreUntouched()
        {
            Assert.AreEqual(0f, Node(0f, -1.2f, SurfaceY, -4f), 1e-6f);
            Assert.AreEqual(0f, Node(0f, 1.2f, SurfaceY, -4f), 1e-6f);
        }

        [Test]
        public void StandingStillMakesNoWake()
        {
            Assert.AreEqual(0f, Node(0f, RockMaxX, SurfaceY, 0f), 1e-6f);
        }

        [Test]
        public void RepeatedFramesConvergeInsteadOfExploding()
        {
            var velocity = 0f;

            for (var i = 0; i < 240; i++)
                velocity = Node(velocity, RockMaxX, SurfaceY, -4f);

            Assert.AreEqual(-0.6f, velocity, 1e-3f);
            Assert.GreaterOrEqual(velocity, -0.6f);
        }

        [Test]
        public void AnExistingLargeVelocityIsNeverAmplified()
        {
            var velocity = Node(-5f, RockMaxX, SurfaceY, -4f);

            Assert.AreEqual(-5f, velocity, 1e-4f);
            Assert.LessOrEqual(Mathf.Abs(velocity), 5f);
        }

        [Test]
        public void ZeroTransferChangesNothing()
        {
            var velocity = WaterWake.NodeVelocity(
                0.3f, RockMaxX, SurfaceY, RockMinX, RockMaxX, RockTopMinY, RockTopMaxY, -4f, Influence, 0f);

            Assert.AreEqual(0.3f, velocity, 1e-6f);
        }

        [Test]
        public void ApplyWritesOnlyTheNodesInReach()
        {
            var nodeX = new[] { -1.2f, -0.5f, -0.25f, 0f, 0.5f, 1.2f };
            var nodeY = new[] { SurfaceY, SurfaceY, SurfaceY, SurfaceY, SurfaceY, SurfaceY };
            var velocities = new float[nodeX.Length];

            var changed = WaterWake.Apply(
                velocities, nodeX, nodeY,
                RockMinX, RockMaxX, RockTopMinY, RockTopMaxY,
                -4f, Influence, Transfer);

            Assert.AreEqual(3, changed);
            Assert.AreEqual(0f, velocities[0], 1e-6f);
            Assert.Greater(velocities[1], 0f);
            Assert.AreEqual(0f, velocities[2], 1e-6f);
            Assert.Less(velocities[3], 0f);
            Assert.Less(velocities[4], velocities[3]);
            Assert.AreEqual(0f, velocities[5], 1e-6f);
        }

        [Test]
        public void CapInjectionClampsANewInjectionToTheCap()
        {
            Assert.AreEqual(-1.5f, WaterWake.CapInjection(0f, -2.357f * 0.5f * 2f, 1.5f), 1e-4f);
            Assert.AreEqual(1.5f, WaterWake.CapInjection(0f, 9f, 1.5f), 1e-4f);
        }

        [Test]
        public void CapInjectionLeavesSmallInjectionsAlone()
        {
            Assert.AreEqual(-1.18f, WaterWake.CapInjection(0f, -1.18f, 1.5f), 1e-4f);
            Assert.AreEqual(0.42f, WaterWake.CapInjection(-0.1f, 0.42f, 1.5f), 1e-4f);
        }

        [Test]
        public void CapInjectionNeverClipsAnAlreadyLargerVelocity()
        {
            Assert.AreEqual(-4f, WaterWake.CapInjection(-5f, -4f, 1.5f), 1e-4f);
            Assert.AreEqual(-5f, WaterWake.CapInjection(-5f, -9f, 1.5f), 1e-4f);
        }

        [Test]
        public void CapInjectionIsDisabledByANonPositiveCap()
        {
            Assert.AreEqual(-9f, WaterWake.CapInjection(0f, -9f, 0f), 1e-4f);
            Assert.AreEqual(-9f, WaterWake.CapInjection(0f, -9f, -1f), 1e-4f);
        }

        [Test]
        public void CappedWakeStaysInsideTheCap()
        {
            var velocity = 0f;

            for (var i = 0; i < 240; i++)
                velocity = WaterWake.CapInjection(velocity, Node(velocity, RockMaxX, SurfaceY, -40f), 1.5f);

            Assert.LessOrEqual(Mathf.Abs(velocity), 1.5f + 1e-4f);
        }

        [Test]
        public void BoundsOverloadMatchesTheExplicitBoundsOverload()
        {
            var bounds = new Bounds(
                new Vector3((RockMinX + RockMaxX) * 0.5f, (RockTopMinY + RockTopMaxY) * 0.5f, 0f),
                new Vector3(RockMaxX - RockMinX, RockTopMaxY - RockTopMinY, 0f));

            var fromBounds = WaterWake.NodeVelocity(0f, RockMaxX, SurfaceY, bounds, -4f, Influence, Transfer);

            Assert.AreEqual(Node(0f, RockMaxX, SurfaceY, -4f), fromBounds, 1e-4f);
        }
    }
}
