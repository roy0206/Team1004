using NUnit.Framework;

namespace Game.Config.Tests
{
    public sealed class GameConfigValuesTests
    {
        [Test]
        public void Defaults_MatchDesignV4()
        {
            var values = new GameConfigValues();

            Assert.AreEqual(1.6f, values.JumpDuration, 0.0001f);
            Assert.AreEqual(2.2f, values.JumpHeight, 0.0001f);
            Assert.AreEqual(1.2f, values.JumpCooldown, 0.0001f);
            Assert.AreEqual(1.2f, values.BossTelegraphDuration, 0.0001f);
            Assert.AreEqual(1.5f, values.BossFullLaneTelegraphDuration, 0.0001f);
            Assert.AreEqual(0.6f, values.BossAttackDuration, 0.0001f);
            Assert.AreEqual(0.6f, values.BossRecoveryDuration, 0.0001f);
            Assert.AreEqual(1f, values.BossBannerDuration, 0.0001f);
            Assert.AreEqual(1f, values.BossClearBannerDuration, 0.0001f);
            Assert.AreEqual(12.8f, values.BossLaneBandWidth, 0.0001f);
            Assert.AreEqual(0.9f, values.BossLaneBandHeight, 0.0001f);
            Assert.IsFalse(values.DebugEnabled);
            Assert.IsFalse(values.DebugInvincible);
        }

        [Test]
        public void Defaults_AreValid()
        {
            var values = new GameConfigValues();

            Assert.IsTrue(values.Validate(out var error), error);
        }

        [Test]
        public void Json_RoundTripsNewFields()
        {
            var values = new GameConfigValues();
            GameConfigJson.Populate(
                "{\"debugEnabled\":true,\"bossBannerDuration\":2.5,\"bossClearBannerDuration\":0," +
                "\"bossLaneBandWidth\":10,\"bossLaneBandHeight\":1.1}",
                values);

            Assert.IsTrue(values.DebugEnabled);
            Assert.AreEqual(2.5f, values.BossBannerDuration, 0.0001f);
            Assert.AreEqual(0f, values.BossClearBannerDuration, 0.0001f);
            Assert.AreEqual(10f, values.BossLaneBandWidth, 0.0001f);
            Assert.AreEqual(1.1f, values.BossLaneBandHeight, 0.0001f);
            Assert.IsTrue(values.Validate(out var error), error);

            var clone = values.Clone();
            Assert.IsTrue(clone.DebugEnabled);
            Assert.AreEqual(2.5f, clone.BossBannerDuration, 0.0001f);
            Assert.AreEqual(1.1f, clone.BossLaneBandHeight, 0.0001f);
            StringAssert.Contains("debugEnabled", GameConfigJson.ToJson(clone));
        }

        [Test]
        public void Validate_RejectsNegativeBannerDuration()
        {
            var values = new GameConfigValues();
            GameConfigJson.Populate("{\"bossBannerDuration\":-1}", values);

            Assert.IsFalse(values.Validate(out var error));
            StringAssert.Contains("bossBannerDuration", error);
        }

        [Test]
        public void Validate_RejectsEmptyLaneBand()
        {
            var values = new GameConfigValues();
            GameConfigJson.Populate("{\"bossLaneBandHeight\":0}", values);

            Assert.IsFalse(values.Validate(out var error));
            StringAssert.Contains("bossLaneBandHeight", error);
        }

        [Test]
        public void Validate_RejectsNegativeJumpValues()
        {
            var values = new GameConfigValues();
            GameConfigJson.Populate("{\"jumpHeight\":-0.1}", values);

            Assert.IsFalse(values.Validate(out var error));
            StringAssert.Contains("jumpHeight", error);
        }
    }
}
