using NUnit.Framework;
using UnityEngine;

namespace Game.Water.Tests
{
    public sealed class WaterInteractorModuleTests
    {
        private GameObject host;

        [SetUp]
        public void SetUp()
        {
            host = new GameObject("WaterInteractorModuleTests");
        }

        [TearDown]
        public void TearDown()
        {
            if (host != null)
                Object.DestroyImmediate(host);
        }

        private WaterInteractorModule Create(bool wakeOnly, float wakeScale)
        {
            return new WaterInteractorModule(
                host.transform, new Vector2(1.2f, 1f), new Vector2(0f, 2.35f), 0f, 1f, wakeOnly, wakeScale);
        }

        [Test]
        public void DefaultsTransferVerticalVelocityAtFullWakeScale()
        {
            var module = new WaterInteractorModule(host.transform, Vector2.one, Vector2.zero, 0f, 1f);

            Assert.IsTrue(module.TransfersVerticalVelocity);
            Assert.IsFalse(module.WakeOnly);
            Assert.AreEqual(1f, module.WakeScale, 1e-6f);
        }

        [Test]
        public void WakeOnlyDisablesVerticalTransfer()
        {
            var module = Create(true, 0.5f);

            Assert.IsTrue(module.WakeOnly);
            Assert.IsFalse(module.TransfersVerticalVelocity);
        }

        [Test]
        public void WakeOnlyCanBeToggledAtRuntime()
        {
            var module = Create(true, 0.5f);

            module.WakeOnly = false;

            Assert.IsTrue(module.TransfersVerticalVelocity);
        }

        [Test]
        public void WakeScaleIsKeptNonNegative()
        {
            var module = Create(true, -3f);

            Assert.AreEqual(0f, module.WakeScale, 1e-6f);

            module.WakeScale = 0.3f;

            Assert.AreEqual(0.3f, module.WakeScale, 1e-6f);
        }

        [Test]
        public void OffsetMovesTheBoundsToASurfaceBand()
        {
            host.transform.position = new Vector3(3f, -0.35f, 0f);

            var module = Create(true, 0.5f);
            var bounds = module.Bounds;

            Assert.AreEqual(2f, bounds.center.y, 1e-4f);
            Assert.AreEqual(1.5f, bounds.min.y, 1e-4f);
            Assert.AreEqual(2.5f, bounds.max.y, 1e-4f);
            Assert.AreEqual(1.2f, bounds.size.x, 1e-4f);
        }

        private static int WarmupFrames
        {
            get
            {
                var settings = WaterSettings.currentSettings;
                return settings != null ? settings.InjectionWarmupFrames : 0;
            }
        }

        private static float TeleportDistance
        {
            get
            {
                var settings = WaterSettings.currentSettings;
                return settings != null ? settings.TeleportDistance : 0f;
            }
        }

        [Test]
        public void SettingsCarryTheWarmupData()
        {
            Assert.IsNotNull(WaterSettings.currentSettings, "WaterSettings.asset이 Resources에 있어야 한다.");
            Assert.GreaterOrEqual(WarmupFrames, 1);
            Assert.Greater(TeleportDistance, 0f);
        }

        [Test]
        public void ResetVelocityStartsTheWarmup()
        {
            var module = Create(false, 1f);

            module.ResetVelocity();

            Assert.IsTrue(module.IsWarmingUp);
            Assert.AreEqual(WarmupFrames, module.WarmupRemaining);
        }

        [Test]
        public void NoInjectionDuringWarmupFramesAndInjectionAfter()
        {
            var module = Create(false, 1f);
            module.ResetVelocity();

            for (var i = 0; i < WarmupFrames; i++)
            {
                Assert.IsFalse(module.Step(1f / 60f), $"warm-up {i}번째 프레임에서 주입이 일어났다.");
                Assert.IsFalse(module.IsTouchingWater);
                Assert.AreEqual(WarmupFrames - i - 1, module.WarmupRemaining);
            }

            Assert.IsTrue(module.Step(1f / 60f));
            Assert.IsFalse(module.IsWarmingUp);
        }

        [Test]
        public void APositionJumpIsDetectedAsATeleportAndRestartsTheWarmup()
        {
            var module = Create(false, 1f);
            module.ResetVelocity();

            for (var i = 0; i < WarmupFrames; i++)
                module.Step(1f / 60f);

            Assert.IsTrue(module.Step(1f / 60f));

            host.transform.position = new Vector3(TeleportDistance + 1f, 0f, 0f);

            Assert.IsFalse(module.Step(1f / 60f), "순간이동 프레임에는 주입하지 않는다.");
            Assert.AreEqual(WarmupFrames, module.WarmupRemaining);
            Assert.AreEqual(0f, module.Velocity.x, 1e-6f);
            Assert.AreEqual(0f, module.Velocity.y, 1e-6f);
        }

        [Test]
        public void NormalScrollSpeedIsNotMistakenForATeleport()
        {
            var module = Create(false, 1f);
            module.ResetVelocity();

            for (var i = 0; i < WarmupFrames; i++)
                module.Step(1f / 60f);

            for (var i = 0; i < 30; i++)
            {
                host.transform.position += new Vector3(-4f / 60f, 0f, 0f);
                Assert.IsTrue(module.Step(1f / 60f), "스크롤 이동을 순간이동으로 오해했다.");
            }

            Assert.AreEqual(-4f, module.HorizontalVelocity, 1e-3f);
        }

        [Test]
        public void NotifyTeleportSuppressesInjectionAndClearsVelocity()
        {
            var module = Create(false, 1f);
            module.ResetVelocity();

            for (var i = 0; i < WarmupFrames; i++)
                module.Step(1f / 60f);

            host.transform.position += new Vector3(-4f / 60f, 0f, 0f);
            module.Step(1f / 60f);
            Assert.Less(module.HorizontalVelocity, -1f);

            module.NotifyTeleport();

            Assert.IsTrue(module.IsWarmingUp);
            Assert.AreEqual(0f, module.HorizontalVelocity, 1e-6f);
            Assert.IsFalse(module.Step(1f / 60f));
        }

        [Test]
        public void WorldShiftSpeedIsMeasuredButNotTransferredWhenWakeOnly()
        {
            var sampler = new VelocitySampler(0f);
            sampler.Reset(new Vector2(0f, -0.35f));
            sampler.Sample(new Vector2(0f, -0.35f - 2.357f * 0.02f), 0.02f);

            Assert.Less(sampler.Vertical, -2f);
            Assert.IsFalse(Create(true, 0.5f).TransfersVerticalVelocity);
            Assert.IsTrue(Create(false, 0.5f).TransfersVerticalVelocity);
        }
    }
}
