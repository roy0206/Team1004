using System;
using System.Collections;
using Game.Config;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Game.Play.Tests
{
    public sealed class CoreLoopSmokeTests : InputTestFixture
    {
        private const string BootstrapScenePath = "Assets/Scenes/Bootstrap.unity";
        private const string StartSceneName = "Start";
        private const string PlaySceneName = "Play";
        private const string SeedJson = "{\"spawnSeed\": 12345}";


        [UnityTest]
        [Timeout(180000)]
        public IEnumerator BootstrapToPlay_ReachesRunning_AndLaneInputMovesPlayer()
        {
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

            Assert.IsTrue(GameConfig.TryApplyJson(SeedJson, out var seedError), "Seed override failed: " + seedError);
            Assert.IsTrue(SceneController.Instance.TryGetSceneByName(PlaySceneName, out var playScene), "Play scene is not registered.");
            _ = SceneController.Instance.LoadAsync(playScene);

            yield return WaitUntil(() => PlayFlow.HasCurrent, 60f);
            Assert.IsTrue(PlayFlow.HasCurrent, "PlayFlow was not registered.");

            yield return WaitUntil(
                () => PlayFlow.Current.State == PlayState.Running || PlayFlow.Current.State == PlayState.Cutscene,
                20f);

            if (PlayFlow.Current.State == PlayState.Cutscene)
            {
                yield return null;
                PlayFlow.Current.Cutscene.Skip();
            }

            yield return WaitUntil(() => PlayFlow.Current.State == PlayState.Running, 20f);
            Assert.AreEqual(PlayState.Running, PlayFlow.Current.State, "PlayFlow did not reach Running.");
            CollectionAssert.Contains(PlayFlow.SeenCutscenes, Game.Cutscene.CutsceneCatalog.Intro,
                "The intro cutscene did not run before the first section.");
            Assert.IsFalse(PlayFlow.Current.Cutscene.IsPlaying, "The intro cutscene is still playing.");

            var player = PlayFlow.Current.Player;
            Assert.IsNotNull(player, "Player is not wired.");
            Assert.AreEqual(1, player.CurrentLane, "Player should start on the middle lane.");
            Debug.Log("[SmokeTest] Running reached. " + DescribeInput(player));

            var keyboard = Keyboard.current ?? InputSystem.AddDevice<Keyboard>();
            yield return null;
            Debug.Log("[SmokeTest] Keyboard ready. " + DescribeInput(player) + $" keyboardEnabled={keyboard.enabled}");
            InputManager.Instance.TryGetAction("Player/LaneUp", out var laneUp);
            Press(keyboard.upArrowKey);
            Debug.Log($"[SmokeTest] after press: key={keyboard.upArrowKey.isPressed} action={laneUp.IsPressed()} phase={laneUp.phase} lane={player.CurrentLane}");
            yield return null;
            Debug.Log($"[SmokeTest] next frame: key={keyboard.upArrowKey.isPressed} action={laneUp.IsPressed()} phase={laneUp.phase} lane={player.CurrentLane} moving={player.IsMoving}");
            Release(keyboard.upArrowKey);
            yield return null;

            yield return WaitUntil(() => player.CurrentLane == 0, 3f);
            Assert.AreEqual(0, player.CurrentLane, "Lane up input did not move the player. " + DescribeInput(player));

            yield return WaitUntil(() => !player.IsMoving, 3f);
            Assert.IsFalse(player.IsMoving, "Lane move did not finish.");
            Assert.Greater(PlayFlow.Current.Distance, 0f, "Distance did not advance while Running.");
        }

        private static string DescribeInput(Game.Player.LanePlayer player)
        {
            var input = InputManager.Instance;
            var hasAction = input.TryGetAction("Player/LaneUp", out var action);
            var hit = player.Hurtbox != null ? player.Hurtbox.LastHit : null;
            var hitInfo = hit != null
                ? $"lastHit={hit.name}({hit.Kind}) at {hit.transform.position} active={hit.isActiveAndEnabled} playerPos={player.transform.position} "
                : "lastHit=none ";
            return hitInfo + $"state={PlayFlow.Current.State} distance={PlayFlow.Current.Distance:F2} inputEnabled={player.InputEnabled} moving={player.IsMoving} " +
                   $"airborne={player.IsAirborne} mapEnabled={input.IsMapEnabled("Player")} " +
                   $"action={(hasAction ? action.enabled.ToString() : "missing")} " +
                   $"controls={(hasAction ? action.controls.Count : -1)} keyboard={(Keyboard.current != null)} " +
                   $"devices={InputSystem.devices.Count} updateMode={InputSystem.settings.updateMode}";
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
