using NUnit.Framework;
using UnityEngine;

namespace Game.Ledge.Tests
{
    public sealed class LedgeScheduleTests
    {
        private const float LedgeTime = 12.5f;
        private const float SafeTime = 2f;
        private const float Speed = 4f;
        private const float PlayerX = -4.2f;
        private const float SpawnX = 7.5f;
        private const float RiseDuration = 0.7f;
        private const float SettleDuration = 1.2f;
        private const float RetireX = -20f;
        private const float ClearMargin = 1.2f;
        private const float Step = 1f / 60f;

        private static LedgePlan CreatePlan()
        {
            return LedgePlan.Create(LedgeTime, SafeTime, Speed, PlayerX, SpawnX);
        }

        private static LedgeClock CreateClock(bool instantFail = false)
        {
            var clock = new LedgeClock();
            Assert.IsTrue(clock.Begin(CreatePlan(), RiseDuration, SettleDuration, RetireX, ClearMargin, instantFail));
            return clock;
        }

        private static LedgeSignal Run(LedgeClock clock, float seconds, bool airborne)
        {
            var signals = LedgeSignal.None;
            var steps = (int)(seconds / Step);

            for (var i = 0; i < steps; i++)
                signals |= clock.Advance(Step, airborne);

            return signals;
        }

        [Test]
        public void Plan_IsInvalid_WhenLedgeTimeIsZero()
        {
            Assert.IsFalse(LedgePlan.Create(0f, SafeTime, Speed, PlayerX, SpawnX).IsValid);
            Assert.IsFalse(LedgePlan.Create(LedgeTime, SafeTime, 0f, PlayerX, SpawnX).IsValid);
            Assert.IsFalse(LedgePlan.Create(LedgeTime, SafeTime, Speed, PlayerX, PlayerX).IsValid);
        }

        [Test]
        public void Plan_ApproachTime_IsSafeTimeBeforeArrival()
        {
            var plan = CreatePlan();
            Assert.AreEqual(LedgeTime - SafeTime, plan.ApproachTime, 1e-4f);
        }

        [Test]
        public void Plan_StartsFromSpawnX_WhenTravelTimeExceedsSafeTime()
        {
            var plan = CreatePlan();
            var travelTime = (SpawnX - PlayerX) / Speed;

            Assert.Greater(travelTime, SafeTime);
            Assert.AreEqual(LedgeTime - travelTime, plan.StartTime, 1e-4f);
            Assert.AreEqual(SpawnX, plan.StartX, 1e-4f);
            Assert.Less(plan.StartTime, plan.ApproachTime);
        }

        [Test]
        public void Plan_StartsAtApproachTime_WhenSafeTimeExceedsTravelTime()
        {
            var plan = LedgePlan.Create(LedgeTime, 5f, Speed, PlayerX, SpawnX);

            Assert.AreEqual(LedgeTime - 5f, plan.StartTime, 1e-4f);
            Assert.AreEqual(plan.ApproachTime, plan.StartTime, 1e-4f);
            Assert.AreEqual(PlayerX + Speed * 5f, plan.StartX, 1e-4f);
        }

        [Test]
        public void Plan_FrontReachesPlayer_ExactlyAtLedgeTime()
        {
            var plan = CreatePlan();

            Assert.AreEqual(plan.StartX, plan.FrontXAt(plan.StartTime), 1e-4f);
            Assert.AreEqual(PlayerX, plan.FrontXAt(plan.LedgeTime), 1e-4f);
            Assert.AreEqual(plan.LedgeTime, plan.TimeAtFrontX(PlayerX), 1e-4f);
            Assert.Greater(plan.FrontXAt(plan.ApproachTime), PlayerX);
        }

        [Test]
        public void Plan_ClampsStartTimeToZero_AndStillArrivesOnTime()
        {
            var plan = LedgePlan.Create(1f, SafeTime, Speed, PlayerX, SpawnX);

            Assert.AreEqual(0f, plan.StartTime, 1e-4f);
            Assert.AreEqual(PlayerX + Speed, plan.StartX, 1e-4f);
            Assert.AreEqual(PlayerX, plan.FrontXAt(1f), 1e-4f);
        }

        [Test]
        public void Clock_RaisesSpawnAndApproach_AtPlannedTimes()
        {
            var clock = CreateClock();
            var plan = clock.Plan;

            Assert.AreEqual(LedgePhase.Waiting, clock.Phase);

            var beforeStart = Run(clock, plan.StartTime - 0.2f, false);
            Assert.AreEqual(LedgeSignal.None, beforeStart);
            Assert.AreEqual(LedgePhase.Waiting, clock.Phase);

            var toApproach = Run(clock, plan.ApproachTime - clock.Time + 0.05f, false);
            Assert.IsTrue((toApproach & LedgeSignal.Spawned) != 0);
            Assert.IsTrue((toApproach & LedgeSignal.Approaching) != 0);
            Assert.AreEqual(LedgePhase.Approaching, clock.Phase);
            Assert.IsTrue(clock.IsSpawnSuspended);
        }

        [Test]
        public void Clock_Blocks_WhenPlayerIsGrounded_AndHoldsFrontAtPlayer()
        {
            var clock = CreateClock();
            var signals = Run(clock, LedgeTime + 0.5f, false);

            Assert.IsTrue((signals & LedgeSignal.Blocked) != 0);
            Assert.AreEqual(LedgePhase.Blocked, clock.Phase);
            Assert.IsTrue(clock.IsHoldingWorld);
            Assert.AreEqual(LedgeTime, clock.Time, 1e-3f);
            Assert.AreEqual(PlayerX, clock.FrontX, 1e-3f);
        }

        [Test]
        public void Clock_DoesNotAdvanceTime_WhileBlocked()
        {
            var clock = CreateClock();
            Run(clock, LedgeTime + 0.1f, false);

            var held = clock.Time;
            Run(clock, 3f, false);

            Assert.AreEqual(held, clock.Time, 1e-4f);
            Assert.AreEqual(LedgePhase.Blocked, clock.Phase);
        }

        [Test]
        public void Clock_ResumesFromBlocked_WhenPlayerJumps()
        {
            var clock = CreateClock();
            Run(clock, LedgeTime + 0.1f, false);

            var resumed = clock.Advance(Step, true);

            Assert.IsTrue((resumed & LedgeSignal.Resumed) != 0);
            Assert.AreEqual(LedgePhase.Passing, clock.Phase);
            Assert.IsFalse(clock.IsHoldingWorld);
        }

        [Test]
        public void Clock_ClearsAfterLanding_WhenPlayerIsAirborneOnArrival()
        {
            var clock = CreateClock();
            var plan = clock.Plan;

            Run(clock, plan.LedgeTime - 0.5f, false);
            Run(clock, 1f, true);

            Assert.AreEqual(LedgePhase.Passing, clock.Phase);
            Assert.Less(clock.FrontX, PlayerX - ClearMargin);

            var cleared = clock.Advance(Step, false);

            Assert.IsTrue((cleared & LedgeSignal.Cleared) != 0);
            Assert.AreEqual(LedgePhase.Rising, clock.Phase);
        }

        [Test]
        public void Clock_ReBlocks_WhenPlayerLandsBeforeTheWallPasses()
        {
            var clock = CreateClock();
            var plan = clock.Plan;

            Run(clock, plan.LedgeTime - 0.1f, false);
            Run(clock, 0.2f, true);

            Assert.AreEqual(LedgePhase.Passing, clock.Phase);
            Assert.Greater(clock.FrontX, PlayerX - ClearMargin);

            var blocked = clock.Advance(Step, false);

            Assert.IsTrue((blocked & LedgeSignal.Blocked) != 0);
            Assert.AreEqual(LedgePhase.Blocked, clock.Phase);
        }

        [Test]
        public void Clock_FinishesRise_AfterCameraMoveDuration()
        {
            var clock = CreateClock();
            Run(clock, LedgeTime - 0.5f, false);
            Run(clock, 1f, true);
            clock.Advance(Step, false);

            Assert.AreEqual(LedgePhase.Rising, clock.Phase);
            Assert.AreEqual(0f, clock.RiseProgress01, 1e-3f);
            Assert.IsTrue(clock.IsSpawnSuspended);

            var half = Run(clock, RiseDuration * 0.5f, false);
            Assert.AreEqual(LedgeSignal.None, half);
            Assert.AreEqual(0.5f, clock.RiseProgress01, 0.05f);

            var finished = Run(clock, RiseDuration, false);
            Assert.IsTrue((finished & LedgeSignal.Finished) != 0);
            Assert.AreEqual(LedgePhase.Retiring, clock.Phase);
            Assert.IsFalse(clock.IsSpawnSuspended);
            Assert.IsFalse(clock.IsHoldingWorld);
        }

        [Test]
        public void Clock_Retires_AfterTheLedgeLeavesTheScreen()
        {
            var clock = CreateClock();
            Run(clock, LedgeTime - 0.5f, false);
            Run(clock, 1f, true);
            clock.Advance(Step, false);
            Run(clock, RiseDuration + 0.1f, false);

            Assert.AreEqual(LedgePhase.Retiring, clock.Phase);

            var retired = Run(clock, 12f, false);

            Assert.IsTrue((retired & LedgeSignal.Retired) != 0);
            Assert.AreEqual(LedgePhase.Done, clock.Phase);
            Assert.IsFalse(clock.IsActive);
            Assert.LessOrEqual(clock.FrontX, RetireX);
        }

        [Test]
        public void Clock_InstantFail_RaisesFailedInsteadOfBlocking()
        {
            var clock = CreateClock(true);
            var signals = Run(clock, LedgeTime + 0.5f, false);

            Assert.IsTrue((signals & LedgeSignal.Failed) != 0);
            Assert.AreEqual(LedgePhase.Failed, clock.Phase);
            Assert.IsFalse(clock.IsHoldingWorld);
            Assert.IsFalse(clock.IsActive);
        }

        [Test]
        public void Clock_Reset_ReturnsToIdle()
        {
            var clock = CreateClock();
            Run(clock, LedgeTime + 0.5f, false);
            clock.Reset();

            Assert.AreEqual(LedgePhase.Idle, clock.Phase);
            Assert.AreEqual(0f, clock.Time, 1e-4f);
            Assert.IsFalse(clock.IsActive);
            Assert.IsFalse(clock.IsHoldingWorld);
            Assert.IsFalse(clock.IsSpawnSuspended);
            Assert.AreEqual(LedgeSignal.None, clock.Advance(Step, false));
        }

        [Test]
        public void Data_ReportsLedgeSectionsFromDefaults()
        {
            var data = ScriptableObject.CreateInstance<LedgeData>();

            try
            {
                Assert.IsTrue(data.HasLedge(1));
                Assert.IsTrue(data.HasLedge(2));
                Assert.IsFalse(data.HasLedge(3));
                Assert.IsFalse(data.HasLedge(4));
                Assert.IsFalse(data.HasLedge(0));
                Assert.AreEqual(3, data.EntryCount);
                Assert.AreEqual(2, data.CountForSection(1));
                Assert.AreEqual(1, data.CountForSection(2));
                Assert.AreEqual(5f, data.GetLedgeTime(1), 1e-4f);
                Assert.AreEqual(15f, data.GetLedgeTime(2), 1e-4f);
                Assert.AreEqual(1, data.HintSection);
                Assert.AreEqual(0, data.HintEntryIndex);
            }
            finally
            {
                Object.DestroyImmediate(data);
            }
        }

        [Test]
        public void Data_ListsSectionOneEntriesInOrder()
        {
            var data = ScriptableObject.CreateInstance<LedgeData>();

            try
            {
                Assert.AreEqual(1, data.GetEntry(0).Section);
                Assert.AreEqual(5f, data.GetEntry(0).Time, 1e-4f);
                Assert.AreEqual(1, data.GetEntry(1).Section);
                Assert.AreEqual(12.5f, data.GetEntry(1).Time, 1e-4f);
                Assert.AreEqual(2, data.GetEntry(2).Section);
                Assert.AreEqual(15f, data.GetEntry(2).Time, 1e-4f);
                Assert.IsNull(data.GetEntry(3));
            }
            finally
            {
                Object.DestroyImmediate(data);
            }
        }

        [Test]
        public void Clock_StartTime_PrimesTheSectionClock()
        {
            var plan = CreatePlan();
            var clock = new LedgeClock();

            Assert.IsTrue(clock.Begin(plan, RiseDuration, SettleDuration, RetireX, ClearMargin, false, plan.StartTime));
            Assert.AreEqual(plan.StartTime, clock.Time, 1e-4f);
            Assert.AreEqual(plan.StartX, clock.FrontX, 1e-3f);

            var spawned = clock.Advance(0f, false);

            Assert.IsTrue((spawned & LedgeSignal.Spawned) != 0);
            Assert.AreEqual(LedgePhase.Incoming, clock.Phase);

            Run(clock, LedgeTime - clock.Time + 0.1f, false);

            Assert.AreEqual(LedgePhase.Blocked, clock.Phase);
            Assert.AreEqual(LedgeTime, clock.Time, 1e-2f);
        }

        [Test]
        public void FirstLedgeRetires_BeforeTheSecondLedgeIsNeeded()
        {
            const float firstLedgeTime = 5f;
            const float secondLedgeTime = 12.5f;

            var first = LedgePlan.Create(firstLedgeTime, SafeTime, Speed, PlayerX, SpawnX);
            var clock = new LedgeClock();
            Assert.IsTrue(clock.Begin(first, RiseDuration, SettleDuration, RetireX, ClearMargin, false));

            Run(clock, firstLedgeTime - 0.5f, false);
            Run(clock, 1f, true);
            clock.Advance(Step, false);
            Run(clock, 12f, false);

            Assert.AreEqual(LedgePhase.Done, clock.Phase);

            var second = LedgePlan.Create(secondLedgeTime, SafeTime, Speed, PlayerX, SpawnX);
            Assert.Less(clock.Time, second.StartTime);
        }
    }
}
