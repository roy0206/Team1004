using System;
using System.Collections;
using System.Collections.Generic;
using Game.Config;
using Game.Settings;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using WaterSurface = Game.Water.Water;
using WaterSystem = Game.Water.WaterSystem;

namespace Game.Play.Tests
{
    public sealed class FullLoopTests : InputTestFixture
    {
        private const string BootstrapScenePath = "Assets/Scenes/Bootstrap.unity";
        private const string StartSceneName = "Start";
        private const string PlaySceneName = "Play";
        private const string InvincibleJson = "{\"spawnSeed\": 12345, \"debugInvincible\": true}";
        private const string MortalJson = "{\"spawnSeed\": 12345, \"debugInvincible\": false}";
        private const string RockPoolKey = "Rock";
        private const string ClearOnceText = "클리어 1회";
        private const float Speed = 8f;
        private const float LoopDeadlineSeconds = 600f;
        private const float CutsceneSkipFallbackSeconds = 20f;
        private const float WaterSampleRadius = 0.4f;
        private const int BossInputPeriod = 24;
        private const int LedgeInputPeriod = 8;

        private static readonly PlayState[] ExpectedStates =
        {
            PlayState.Cutscene,
            PlayState.Running,
            PlayState.Cutscene,
            PlayState.Boss,
            PlayState.Running,
            PlayState.Cutscene,
            PlayState.Boss,
            PlayState.Running,
            PlayState.Cutscene,
            PlayState.Boss,
            PlayState.Running,
            PlayState.Cutscene,
            PlayState.Cleared
        };

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
        [Timeout(900000)]
        public IEnumerator FullLoop_ClearsGame_AndTitleShowsClearCount()
        {
            yield return LoadBootstrapToStart();

            Assert.IsTrue(GameConfig.TryApplyJson(InvincibleJson, out var error), "Config override failed: " + error);
            Assert.IsTrue(GameConfig.Current.DebugInvincible, "debugInvincible was not applied through the JSON override.");
            Assert.AreEqual(0, RecordService.ClearCount, "PlayerPrefs were not clean before the run.");
            Assert.IsFalse(RecordService.EndingReached);

            yield return LoadPlay();

            var flow = PlayFlow.Current;
            var states = new List<PlayState> { flow.State };
            flow.StateChanged += state =>
            {
                if (states[states.Count - 1] != state)
                    states.Add(state);
            };

            var sections = new List<int>();
            flow.SectionStarted += sections.Add;

            var ledgeExpected = new HashSet<int>();
            var ledgeBlocked = new HashSet<int>();
            var ledgeCleared = new HashSet<int>();
            var ledgeFinished = new HashSet<int>();
            var ledge = flow.LedgeHandler;
            Assert.IsNotNull(ledge, "PlayFlow has no ledge handler. LedgeSet.prefab is not wired into the play scene.");
            Assert.IsNotNull(flow.Ledge, "PlayFlow.ledgeDirector is not wired.");
            Assert.IsNotNull(flow.Ledge.Data, "LedgeDirector has no LedgeData.");
            ledge.Blocked += section => ledgeBlocked.Add(section);
            ledge.Cleared += section => ledgeCleared.Add(section);
            ledge.Finished += section => ledgeFinished.Add(section);

            for (var section = 1; section <= GameConfig.Current.SectionCount; section++)
                if (flow.Ledge.Data.HasLedge(section))
                    ledgeExpected.Add(section);

            var keyboard = Keyboard.current ?? InputSystem.AddDevice<Keyboard>();
            var waterfallBoss = UnityEngine.Object.FindAnyObjectByType<Game.Boss.WaterfallBoss>(FindObjectsInactive.Include);
            Assert.IsNotNull(waterfallBoss, "WaterfallBoss is not in the play scene.");
            var upHeld = false;
            var bossTimerSeen = new HashSet<int>();
            var hintSeen = false;
            var waterChecked = false;
            var waterDisplacement = 0f;
            var frame = 0;
            var cutsceneStartedAt = -1f;
            var deadline = Time.realtimeSinceStartup + LoopDeadlineSeconds;

            while (flow != null && flow.State != PlayState.Cleared && flow.State != PlayState.Failed &&
                   Time.realtimeSinceStartup < deadline)
            {
                Time.timeScale = Speed;
                frame++;

                switch (flow.State)
                {
                    case PlayState.Cutscene:
                        if (cutsceneStartedAt < 0f)
                            cutsceneStartedAt = Time.realtimeSinceStartup;

                        if (Time.realtimeSinceStartup - cutsceneStartedAt > CutsceneSkipFallbackSeconds)
                        {
                            Debug.LogWarning("[FullLoopTest] Space did not finish the cutscene in time. Falling back to Skip().");
                            flow.Cutscene.Skip();
                            cutsceneStartedAt = Time.realtimeSinceStartup;
                        }
                        else if (frame % 2 == 0)
                        {
                            Press(keyboard.enterKey);
                        }
                        else
                        {
                            Release(keyboard.enterKey);
                        }

                        break;

                    case PlayState.Running:
                        cutsceneStartedAt = -1f;

                        if (flow.Hud != null && flow.Hud.IsControlHintVisible)
                            hintSeen = true;

                        if (!waterChecked && flow.Section == 1 && flow.SectionTime > 2.5f)
                        {
                            waterChecked = true;
                            yield return JumpAndSampleWater(flow, keyboard, value => waterDisplacement = Mathf.Max(waterDisplacement, value));
                        }

                        if (ledge.IsHoldingWorld)
                        {
                            if (frame % LedgeInputPeriod == 0)
                                Press(keyboard.upArrowKey);
                            else if (frame % LedgeInputPeriod == 2)
                                Release(keyboard.upArrowKey);
                        }

                        break;

                    case PlayState.Boss:
                        cutsceneStartedAt = -1f;

                        if (flow.BossTimer != null && flow.BossTimer.IsShown)
                            bossTimerSeen.Add(flow.Section);

                        if (waterfallBoss.IsFinalApproach)
                        {
                            if (upHeld)
                            {
                                Release(keyboard.upArrowKey);
                                upHeld = false;
                            }
                            else if (ShouldClearFinalWaterfall(flow.Player, waterfallBoss))
                            {
                                Press(keyboard.upArrowKey);
                                upHeld = true;
                            }

                            break;
                        }

                        if (frame % BossInputPeriod == 0)
                        {
                            Press(keyboard.upArrowKey);
                            upHeld = true;
                        }
                        else if (frame % BossInputPeriod == 2)
                        {
                            Release(keyboard.upArrowKey);
                            upHeld = false;
                        }

                        break;
                }

                yield return null;
            }

            Release(keyboard.enterKey);
            Release(keyboard.upArrowKey);
            Time.timeScale = 1f;

            Assert.IsNotNull(flow, "PlayFlow was destroyed during the loop.");
            Assert.AreEqual(PlayState.Cleared, flow.State,
                "The loop did not reach Cleared. States: " + string.Join(", ", states) + " sections: " + string.Join(", ", sections));

            if (states.Count > 0 && states[0] == PlayState.Ready)
                states.RemoveAt(0);

            CollectionAssert.AreEqual(ExpectedStates, states, "State sequence differs. Actual: " + string.Join(", ", states));
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, sections, "Section sequence differs.");
            CollectionAssert.AreEquivalent(new[] { 1, 2, 3 }, bossTimerSeen, "Boss timer view was not shown for every boss.");
            Assert.IsTrue(hintSeen, "The control hint was not shown when section 1 started.");
            CollectionAssert.AreEquivalent(ledgeExpected, ledgeCleared,
                "Ledges were not cleared in every section that schedules one. Blocked: " + string.Join(", ", ledgeBlocked) +
                " cleared: " + string.Join(", ", ledgeCleared));
            CollectionAssert.AreEquivalent(ledgeExpected, ledgeFinished, "Ledges did not finish rising in every scheduled section.");
            Assert.IsFalse(ledge.IsActive, "A ledge is still active after the loop.");
            Assert.IsTrue(flow.Spawner != null && flow.Spawner.SpawningEnabled,
                "Obstacle spawning stayed suspended after the ledge finished.");
            Assert.IsTrue(waterChecked, "The water sample step did not run.");
            Assert.Greater(waterDisplacement, 0.001f, "The water surface under the player did not move after a jump.");

            yield return WaitUntil(() => RecordService.ClearCount == 1, 5f);
            Assert.AreEqual(1, RecordService.ClearCount, "ClearCount did not increase by one.");
            Assert.IsTrue(RecordService.EndingReached, "EndingReached is false after clearing.");
            Assert.IsTrue(flow.IsResultVisible, "ResultPanel is not visible after clearing.");

            flow.GoTitle();

            yield return WaitUntil(
                () => SceneController.HasInstance &&
                      SceneController.Instance.CurrentScene != null &&
                      SceneController.Instance.CurrentScene.Scene.Name == StartSceneName &&
                      !SceneController.Instance.IsTransitioning,
                60f);
            Assert.AreEqual(StartSceneName, SceneController.Instance.CurrentScene?.Scene.Name, "Title scene was not entered after GoTitle.");

            yield return null;
            yield return null;

            var found = false;
            var texts = UnityEngine.Object.FindObjectsByType<Text>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (var i = 0; i < texts.Length; i++)
            {
                if (texts[i].text == ClearOnceText)
                {
                    found = true;
                    break;
                }
            }

            Assert.IsTrue(found, "Title does not show '" + ClearOnceText + "'.");
            Assert.AreEqual(-1, PlayFlow.CheckpointStep, "GoTitle did not clear the checkpoint.");
        }

        [UnityTest]
        [Timeout(300000)]
        public IEnumerator FailurePath_HitShowsResult_AndRetryRestartsFromCheckpoint()
        {
            yield return LoadBootstrapToStart();

            Assert.IsTrue(GameConfig.TryApplyJson(MortalJson, out var error), "Config override failed: " + error);
            Assert.IsFalse(GameConfig.Current.DebugInvincible);

            yield return LoadPlay();

            var flow = PlayFlow.Current;
            var states = new List<PlayState> { flow.State };
            flow.StateChanged += state =>
            {
                if (states[states.Count - 1] != state)
                    states.Add(state);
            };

            yield return WaitUntil(() => flow.State == PlayState.Running || flow.State == PlayState.Cutscene, 20f);

            if (flow.State == PlayState.Cutscene)
            {
                yield return null;
                flow.Cutscene.Skip();
            }

            yield return WaitUntil(() => flow.State == PlayState.Running, 20f);
            Assert.AreEqual(PlayState.Running, flow.State, "Section 1 did not start.");
            Assert.AreEqual(1, flow.Section);

            var player = flow.Player;
            Assert.IsNotNull(player);
            yield return WaitUntil(() => !player.IsMoving && !player.IsAirborne, 3f);

            Assert.IsTrue(PoolManager.TryGetInstance(out var pool), "PoolManager is missing.");
            Assert.IsTrue(pool.IsRegistered(RockPoolKey), "The spawner did not register the Rock pool.");
            Assert.IsTrue(
                pool.TryGet(RockPoolKey, player.transform.position, Quaternion.identity, out var rock),
                "Could not take a Rock from the pool.");
            Assert.IsTrue(rock.IsValid);

            yield return WaitUntil(() => flow.State == PlayState.Failed, 10f);
            Assert.AreEqual(PlayState.Failed, flow.State, "Planting a hazard on the player did not fail the run. States: " + string.Join(", ", states));

            var hitIndex = states.IndexOf(PlayState.Hit);
            var failedIndex = states.IndexOf(PlayState.Failed);
            Assert.GreaterOrEqual(hitIndex, 0, "Hit state was not entered. States: " + string.Join(", ", states));
            Assert.Greater(failedIndex, hitIndex, "Failed must follow Hit. States: " + string.Join(", ", states));
            Assert.IsTrue(flow.IsResultVisible, "ResultPanel is not visible after failing.");
            Assert.AreEqual(1, PlayFlow.CheckpointStep, "Checkpoint should point at section 1.");

            var previous = flow;
            var keyboard = Keyboard.current ?? InputSystem.AddDevice<Keyboard>();
            yield return null;
            Press(keyboard.rKey);
            yield return null;
            Release(keyboard.rKey);

            yield return WaitUntil(() => PlayFlow.HasCurrent && !ReferenceEquals(PlayFlow.Current, previous), 60f);
            Assert.IsTrue(PlayFlow.HasCurrent && !ReferenceEquals(PlayFlow.Current, previous), "R did not reload the play scene.");

            var retried = PlayFlow.Current;
            var retriedStates = new List<PlayState> { retried.State };
            retried.StateChanged += state =>
            {
                if (retriedStates[retriedStates.Count - 1] != state)
                    retriedStates.Add(state);
            };

            yield return WaitUntil(() => retried.State == PlayState.Running, 20f);
            Assert.AreEqual(PlayState.Running, retried.State, "Retry did not reach Running. States: " + string.Join(", ", retriedStates));
            Assert.AreEqual(1, retried.Section, "Retry should restart section 1 from its checkpoint.");
            CollectionAssert.DoesNotContain(retriedStates, PlayState.Cutscene, "The intro cutscene must be skipped after a retry.");
            Assert.Less(retried.Distance, 1f, "Distance should restart from the section checkpoint.");
        }

        private static bool ShouldClearFinalWaterfall(Game.Player.LanePlayer player, Game.Boss.WaterfallBoss boss)
        {
            if (player == null || player.IsMoving || player.IsAirborne)
                return false;

            if (player.CurrentLane != player.TopLane)
                return true;

            if (!player.CanJump)
                return false;

            var remaining = boss.FinalApproachRemaining;
            return remaining >= 0f && remaining <= GameConfig.Current.JumpDuration * 0.75f;
        }

        private IEnumerator JumpAndSampleWater(PlayFlow flow, Keyboard keyboard, Action<float> report)
        {
            var player = flow.Player;
            var surface = flow.Environment != null ? flow.Environment.Surface : null;
            Assert.IsNotNull(surface, "Environment water surface is not wired.");

            for (var lane = player.CurrentLane; lane > player.TopLane; lane--)
            {
                Press(keyboard.upArrowKey);
                yield return null;
                Release(keyboard.upArrowKey);
                yield return WaitUntil(() => !player.IsMoving, 3f);
            }

            Assert.AreEqual(player.TopLane, player.CurrentLane, "Player did not reach the top lane.");
            yield return WaitUntil(() => player.CanJump, 3f);

            Press(keyboard.upArrowKey);
            yield return null;
            Release(keyboard.upArrowKey);
            yield return WaitUntil(() => player.IsAirborne, 3f);
            Assert.IsTrue(player.IsAirborne, "Up on the top lane did not jump.");

            var elapsed = 0f;
            while (elapsed < 2f)
            {
                Time.timeScale = Speed;
                elapsed += Time.deltaTime;
                report(SampleSurface(surface, player.transform.position.x));
                yield return null;
            }
        }

        private static float SampleSurface(WaterSurface surface, float x)
        {
            if (surface == null)
                return 0f;

            if (!WaterSystem.GetPositions(surface, out var positions, out var range) || range.length <= 0)
                return 0f;

            var target = surface.OuterIndexRange(x - WaterSampleRadius, x + WaterSampleRadius);
            var start = Mathf.Max(range.start, target.start);
            var end = Mathf.Min(range.end, target.end);
            var max = 0f;

            for (var i = start; i < end; i++)
            {
                var index = i - range.start;
                if (index < 0 || index >= positions.Length)
                    continue;

                max = Mathf.Max(max, Mathf.Abs(positions[index]));
            }

            return max;
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
            Assert.IsTrue(SceneController.Instance.TryGetSceneByName(PlaySceneName, out var playScene), "Play scene is not registered.");
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
