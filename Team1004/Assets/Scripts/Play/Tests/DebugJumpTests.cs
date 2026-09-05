using System;
using System.Collections;
using Game.Boss;
using Game.Config;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Game.Play.Tests
{
    public sealed class DebugJumpTests : InputTestFixture
    {
        private const string BootstrapScenePath = "Assets/Scenes/Bootstrap.unity";
        private const string StartSceneName = "Start";
        private const string PlaySceneName = "Play";
        private const string DebugInvincibleJson =
            "{\"spawnSeed\": 12345, \"debugEnabled\": true, \"debugInvincible\": true}";
        private const string DebugMortalJson =
            "{\"spawnSeed\": 12345, \"debugEnabled\": true, \"debugInvincible\": false}";
        private const float SpawnWaitSpeed = 4f;

        public override void Setup()
        {
            base.Setup();
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            PlayFlow.ResetSession();
            Time.timeScale = 1f;
        }

        public override void TearDown()
        {
            Time.timeScale = 1f;
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            base.TearDown();
        }

        [UnityTest]
        [Timeout(300000)]
        public IEnumerator DebugKeyTwo_JumpsToFirstBoss_WithTimerAndNoObstacles()
        {
            yield return LoadBootstrapToStart();

            Assert.IsTrue(GameConfig.TryApplyJson(DebugInvincibleJson, out var error), "Config override failed: " + error);
            Assert.IsTrue(GameConfig.Current.DebugEnabled, "debugEnabled was not applied.");

            yield return LoadPlay();

            var flow = PlayFlow.Current;
            var keyboard = Keyboard.current ?? InputSystem.AddDevice<Keyboard>();

            yield return WaitUntil(() => flow.State == PlayState.Running || flow.State == PlayState.Cutscene, 30f);

            if (flow.State == PlayState.Cutscene)
            {
                yield return null;
                flow.Cutscene.Skip();
            }

            yield return WaitUntil(() => flow.State == PlayState.Running, 30f);
            Assert.AreEqual(PlayState.Running, flow.State, "Section 1 did not start.");

            var spawner = flow.Spawner;
            var spawnedAny = false;

            if (spawner != null)
            {
                Time.timeScale = SpawnWaitSpeed;
                yield return WaitUntil(() => spawner.ActiveObstacleCount > 0 || flow.State != PlayState.Running, 40f);
                Time.timeScale = 1f;
                spawnedAny = spawner.ActiveObstacleCount > 0;
                Assert.IsTrue(spawnedAny, "No obstacle was spawned before the debug jump.");
            }

            Press(keyboard.digit2Key);
            yield return null;
            Release(keyboard.digit2Key);

            yield return WaitUntil(() => flow.State == PlayState.Boss, 10f);
            Assert.AreEqual(PlayState.Boss, flow.State, "Debug key 2 did not enter the boss step.");
            Assert.AreEqual(1, flow.Section, "Debug key 2 should land on the boss after section 1.");

            Assert.IsTrue(flow.IsBannerVisible, "The boss banner was not shown when the boss step started.");
            StringAssert.Contains("BOSS 1", flow.Hud.BannerText, "The boss banner text is wrong.");

            if (spawnedAny)
                Assert.AreEqual(0, spawner.ActiveObstacleCount, "Obstacles were not released by the debug jump.");

            Assert.IsFalse(flow.Player.HasHit, "The debug jump left the player in a hit state.");

            yield return WaitUntil(() => flow.BossTimer != null && flow.BossTimer.IsShown, 20f);
            Assert.IsTrue(flow.BossTimer.IsShown, "The boss timer was not shown after the banner.");
            Assert.IsFalse(flow.IsBannerVisible, "The boss banner did not disappear before the boss started.");

            if (spawner != null)
                Assert.AreEqual(0, spawner.ActiveObstacleCount, "Obstacles came back during the boss.");
        }

        [UnityTest]
        [Timeout(300000)]
        public IEnumerator DebugKey_WithDebugDisabled_IsIgnored()
        {
            yield return LoadBootstrapToStart();

            Assert.IsTrue(GameConfig.TryApplyJson("{\"spawnSeed\": 12345, \"debugEnabled\": false}", out var error),
                "Config override failed: " + error);

            yield return LoadPlay();

            var flow = PlayFlow.Current;
            var keyboard = Keyboard.current ?? InputSystem.AddDevice<Keyboard>();

            yield return WaitUntil(() => flow.State == PlayState.Running || flow.State == PlayState.Cutscene, 30f);

            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("Debug jump 2 was ignored"));
            Press(keyboard.digit2Key);
            yield return null;
            Release(keyboard.digit2Key);
            yield return null;

            Assert.AreNotEqual(PlayState.Boss, flow.State, "The debug jump ran while debugEnabled was false.");
        }

        [UnityTest]
        [Timeout(600000)]
        public IEnumerator WalrusTwoLaneBand_MissesUncoveredLane_AndHitsCoveredLane()
        {
            yield return LoadBootstrapToStart();

            Assert.IsTrue(GameConfig.TryApplyJson(DebugMortalJson, out var error), "Config override failed: " + error);

            yield return LoadPlay();

            var flow = PlayFlow.Current;
            var player = flow.Player;
            var keyboard = Keyboard.current ?? InputSystem.AddDevice<Keyboard>();

            yield return WaitUntil(() => flow.State == PlayState.Running || flow.State == PlayState.Cutscene, 30f);

            Press(keyboard.digit4Key);
            yield return null;
            Release(keyboard.digit4Key);

            yield return WaitUntil(() => flow.State == PlayState.Boss, 15f);
            Assert.AreEqual(PlayState.Boss, flow.State, "Debug key 4 did not enter the second boss.");

            var boss = UnityEngine.Object.FindFirstObjectByType<WalrusBoss>(FindObjectsInactive.Include);
            Assert.IsNotNull(boss, "The walrus boss is not in the play scene.");

            yield return WaitUntil(() => boss.IsActive, 15f);
            Assert.IsTrue(boss.IsActive, "The walrus boss did not begin.");
            Assert.IsNotNull(boss.LaneHazard, "The walrus boss has no lane hazard.");

            var laneCount = GameConfig.Current.LaneCount;
            var safeChecked = false;
            var hitChecked = false;
            var watchedMask = 0;
            var expectHit = false;
            var watching = false;
            var wasArmed = false;
            var lastMask = -1;

            while (boss.IsActive && !hitChecked && !flow.IsTerminal)
            {
                var mask = boss.DangerMask;
                var armed = boss.IsHazardArmed;

                if (!armed && mask != 0 && mask != lastMask)
                {
                    lastMask = mask;
                    var count = BossLanes.Count(mask);
                    var safeLane = FirstLaneOutside(mask, laneCount);
                    var coveredLane = BossLanes.First(mask);

                    if (count == 2 && !safeChecked && safeLane >= 0)
                    {
                        watching = true;
                        expectHit = false;
                        watchedMask = mask;
                        player.SnapToLane(safeLane);
                    }
                    else if (count == 2 && safeChecked)
                    {
                        watching = true;
                        expectHit = true;
                        watchedMask = mask;
                        player.SnapToLane(coveredLane);
                    }
                    else if (safeLane >= 0)
                    {
                        watching = false;
                        player.SnapToLane(safeLane);
                    }
                }

                if (watching && armed && !wasArmed)
                {
                    var playerLane = player.CurrentLane;
                    var covered = BossLanes.Contains(watchedMask, playerLane);
                    Assert.AreEqual(expectHit, covered,
                        $"The player lane {playerLane} does not match the expected coverage of mask {watchedMask}.");

                    for (var lane = 0; lane < laneCount; lane++)
                    {
                        var laneArmed = boss.LaneHazard.IsLaneArmed(lane);
                        Assert.AreEqual(BossLanes.Contains(watchedMask, lane), laneArmed,
                            $"Lane {lane} armed state does not match the telegraphed band mask {watchedMask}.");
                    }
                }

                if (watching && wasArmed && !armed)
                {
                    if (expectHit)
                    {
                        hitChecked = true;
                        Assert.IsTrue(player.HasHit, "The player inside the telegraphed band was not hit.");
                    }
                    else
                    {
                        safeChecked = true;
                        Assert.IsFalse(player.HasHit, "The player in the uncovered lane was hit by a 2 lane attack.");
                    }

                    watching = false;
                }

                if (expectHit && watching && player.HasHit)
                {
                    hitChecked = true;
                    watching = false;
                }

                wasArmed = armed;
                yield return null;
            }

            if (!hitChecked && watching && expectHit && player.HasHit)
                hitChecked = true;

            var summary = $" (flow={flow.State}, boss stage={boss.Stage}, outcome={boss.Outcome}, playerHit={player.HasHit}, watching={watching}, expectHit={expectHit})";
            Assert.IsTrue(safeChecked, "No two lane attack was observed with the player in the uncovered lane." + summary);
            Assert.IsTrue(hitChecked, "No two lane attack was observed with the player inside the band." + summary);
            Assert.IsTrue(player.HasHit, "The player inside the telegraphed band was not hit." + summary);
        }

        private static int FirstLaneOutside(int mask, int laneCount)
        {
            for (var lane = 0; lane < laneCount; lane++)
                if (!BossLanes.Contains(mask, lane))
                    return lane;

            return -1;
        }

        private static IEnumerator SettlePreviousScene()
        {
            if (!SceneController.HasInstance)
                yield break;

            yield return WaitUntil(() => !SceneController.HasInstance || !SceneController.Instance.IsTransitioning, 30f);
            yield return null;
        }

        private static IEnumerator LoadBootstrapToStart()
        {
            yield return SettlePreviousScene();
            SceneManager.LoadScene(BootstrapScenePath);

            yield return WaitUntil(
                () => SceneController.HasInstance &&
                      SceneController.Instance.CurrentScene != null &&
                      SceneController.Instance.CurrentScene.Scene.Name == StartSceneName,
                60f);
            Assert.IsTrue(SceneController.HasInstance, "SceneController was not registered.");
            Assert.AreEqual(StartSceneName, SceneController.Instance.CurrentScene?.Scene.Name, "Start scene was not entered.");

            yield return WaitUntil(() => !SceneController.Instance.IsTransitioning, 30f);
            Assert.IsFalse(SceneController.Instance.IsTransitioning, "Start scene transition did not finish.");
        }

        private static IEnumerator LoadPlay()
        {
            Assert.IsTrue(SceneController.Instance.TryGetSceneByName(PlaySceneName, out var playScene),
                "Play scene is not registered.");
            _ = SceneController.Instance.LoadAsync(playScene);

            yield return WaitUntil(() => PlayFlow.HasCurrent, 60f);
            Assert.IsTrue(PlayFlow.HasCurrent, "PlayFlow was not registered.");
        }

        private static IEnumerator WaitUntil(Func<bool> condition, float timeoutSeconds)
        {
            var elapsed = 0f;

            while (!condition() && elapsed < timeoutSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }
    }
}
