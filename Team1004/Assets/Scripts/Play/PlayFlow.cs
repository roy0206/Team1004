using System;
using System.Collections.Generic;
using Game.Config;
using Game.Cutscene;
using Game.Environment;
using Game.Ledge;
using Game.Player;
using Game.Settings;
using Game.Spawner;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Play
{
    public sealed class PlayFlow : DomainSingleton<PlayFlow>
    {
        private enum FlowStepKind
        {
            Intro,
            Section,
            Cutscene,
            Boss,
            Ending
        }

        private readonly struct FlowStep
        {
            public FlowStepKind Kind { get; }
            public int Index { get; }

            public FlowStep(FlowStepKind kind, int index)
            {
                Kind = kind;
                Index = index;
            }
        }

        private const string PauseAction = "UI/Pause";
        private const string RetryAction = "UI/Retry";
        private const string PlayBgmId = "bgm_play";
        private const string TitleBgmId = "bgm_title";
        private const string ClearSfxId = "stage_clear";
        private const string FailSfxId = "stage_fail";
        private const string HitSfxId = "hit";
        private const string LaneMoveSfxId = "lane_move";
        private const string JumpSfxId = "jump";
        private const string LandSfxId = "land";
        private const string CutsceneTransitionSfxId = "cutscene_transition";
        private const string EndingSfxId = "ending";
        private const string BossClearSfxId = "boss_clear";
        private const string BossBannerFormat = "BOSS {0} — {1}";
        private const string ClearBannerText = "CLEAR";
        private const int DebugSlotCount = 8;

        public const string FinalWaterfallCheckpoint = "FinalWaterfall";

        private static readonly HashSet<string> seenCutscenes = new(StringComparer.Ordinal);
        private static int checkpointStep = -1;
        private static string checkpointTag = string.Empty;

        private static readonly Key[] debugSlotKeys =
        {
            Key.Digit1,
            Key.Digit2,
            Key.Digit3,
            Key.Digit4,
            Key.Digit5,
            Key.Digit6,
            Key.Digit7,
            Key.Digit8
        };

        [SerializeField] private LanePlayer player;
        [SerializeField] private StageScroller scroller;
        [SerializeField] private ObstacleSpawner spawner;
        [SerializeField] private PlayHud hud;
        [SerializeField] private BossTimerView bossTimer;
        [SerializeField] private PausePanel pausePanel;
        [SerializeField] private ResultPanel resultPanel;
        [SerializeField] private CutscenePlayer cutscene;
        [SerializeField] private EnvironmentThing environment;
        [SerializeField] private LedgeDirector ledgeDirector;
        [SerializeField] private string introCutsceneId = CutsceneCatalog.Intro;
        [SerializeField] private string[] sectionCutsceneIds =
        {
            CutsceneCatalog.FishingLine,
            CutsceneCatalog.Walrus,
            CutsceneCatalog.Waterfall
        };
        [SerializeField] private string endingCutsceneId = CutsceneCatalog.Ending;
        [SerializeField] private SceneReference playScene;
        [SerializeField] private SceneReference startScene;
        [SerializeField] private string[] bossNames = { "낚싯줄", "곰", "폭포" };
        [SerializeField] private float bossWorldRampDuration = 0.5f;

        private readonly List<FlowStep> steps = new();
        private bool listening;
        private bool subscribed;
        private bool spawnerReady;
        private float sectionStart;
        private float sectionLength;
        private float sectionDuration;
        private float sectionTime;
        private PlayState stateBeforePause = PlayState.Running;
        private bool flowRunning;
        private bool bannerHold;
        private int pendingStep = -1;

        public PlayState State { get; private set; } = PlayState.Ready;
        public float Distance { get; private set; }
        public int Section { get; private set; } = 1;
        public float SectionDistance => Mathf.Max(0f, Distance - sectionStart);
        public float SectionTime => sectionTime;
        public float SectionDuration => sectionDuration;
        public float SectionRemaining => Mathf.Max(0f, sectionDuration - sectionTime);
        public float Progress01 => sectionDuration <= 0f ? 0f : Mathf.Clamp01(sectionTime / sectionDuration);
        public bool IsTerminal => State == PlayState.Hit || State == PlayState.Failed || State == PlayState.Cleared;
        public LanePlayer Player => player;
        public StageScroller Scroller => scroller;
        public ObstacleSpawner Spawner => spawner;
        public CutscenePlayer Cutscene => cutscene;
        public BossTimerView BossTimer => bossTimer;
        public EnvironmentThing Environment => environment;
        public LedgeDirector Ledge => ledgeDirector;
        public ILedgeHandler LedgeHandler { get; set; }
        public bool IsWorldHeld => LedgeHandler != null && LedgeHandler.IsHoldingWorld;
        public bool IsLedgeQteActive => LedgeHandler != null && LedgeHandler.IsQteActive;
        public float WorldSpeedScale { get; private set; } = 1f;
        public PlayHud Hud => hud;
        public ResultPanel ResultPanel => resultPanel;
        public bool IsResultVisible => resultPanel != null && resultPanel.gameObject.activeSelf;
        public bool IsBannerVisible => hud != null && hud.IsBannerVisible;
        public bool IsBossInterrupted { get; private set; }
        public bool BossHoldsWorld { get; private set; }
        public bool IsDebugEnabled => GameConfig.Current.DebugEnabled;
        public static IReadOnlyCollection<string> SeenCutscenes => seenCutscenes;
        public static int CheckpointStep => checkpointStep;
        public static string CheckpointTag => checkpointTag;

        public Func<int, Awaitable<bool>> BossHandler { get; set; }
        public Func<int, bool> BossHoldsWorldQuery { get; set; }

        public event Action<PlayState> StateChanged;
        public event Action<int> SectionStarted;

        protected override void OnUnregistering()
        {
            Time.timeScale = 1f;
            Unsubscribe();

            if (LedgeHandler != null)
                LedgeHandler.Failed -= OnLedgeFailed;
        }

        private void OnEnable()
        {
            if (!InputManager.TryGetInstance(out var input) || !input.IsInitialized)
            {
                Debug.LogWarning("[PlayFlow] InputManager is not initialized. Pause input is disabled.", this);
                return;
            }

            input.AddListener(PauseAction, InputPhase.Performed, OnPauseInput);
            input.AddListener(RetryAction, InputPhase.Performed, OnRetryInput);
            listening = true;
        }

        private void OnDisable()
        {
            if (!listening)
                return;

            listening = false;
            if (InputManager.TryGetInstance(out var input))
            {
                input.RemoveListener(PauseAction, InputPhase.Performed, OnPauseInput);
                input.RemoveListener(RetryAction, InputPhase.Performed, OnRetryInput);
            }
        }

        private async void Start()
        {
            Subscribe();
            Time.timeScale = 1f;

            if (AudioManager.TryGetInstance(out var audio) && audio.IsInitialized)
            {
                audio.StopBgm(TitleBgmId);
                audio.PlayBgm(PlayBgmId);
            }

            if (pausePanel != null)
                pausePanel.Hide();

            if (resultPanel != null)
                resultPanel.Hide();

            if (bossTimer != null)
                bossTimer.Hide();

            if (LedgeHandler == null && ledgeDirector != null)
                LedgeHandler = ledgeDirector;

            if (LedgeHandler != null)
                LedgeHandler.Failed += OnLedgeFailed;

            SetState(PlayState.Ready);
            await RunFlowAsync(-1);
        }

        private void Update()
        {
            HandleDebugInput();

            if (State != PlayState.Running)
                return;

            var ledge = LedgeHandler;

            if (ledge != null)
                ledge.Tick(Time.deltaTime);

            if (State != PlayState.Running)
                return;

            var scale = ledge != null ? Mathf.Max(0f, ledge.WorldSpeedScale) : 1f;
            var moving = scale > 0f;

            ApplyWorldSpeedScale(scale, moving);

            if (!moving)
                return;

            var step = Time.deltaTime * scale;

            Distance += GameConfig.Current.ScrollSpeed * step;
            sectionTime += step;

            if (spawnerReady && player != null)
            {
                spawner.SetSpawningEnabled(ledge == null || !ledge.IsSpawnSuspended);

                var playerState = SpawnPlayerState.FromPlayer(
                    player, player.JumpCooldownRemaining, player.AirborneRemaining, player.MoveRemaining);
                spawner.Advance(step, Distance, playerState);
            }
        }

        private void ApplyWorldSpeedScale(float scale, bool moving)
        {
            WorldSpeedScale = scale;

            if (scroller != null)
            {
                scroller.SetScrolling(moving);

                if (!Mathf.Approximately(scroller.SpeedScale, scale))
                    scroller.SetSpeedScale(scale);
            }

            if (environment == null)
                return;

            environment.SetScrolling(moving);

            var target = GameConfig.Current.ScrollSpeed * scale;

            if (!Mathf.Approximately(environment.Speed, target))
                environment.Speed = target;
        }

        private void ResetWorldSpeedScale()
        {
            WorldSpeedScale = 1f;

            if (scroller != null && !Mathf.Approximately(scroller.SpeedScale, 1f))
                scroller.SetSpeedScale(1f);

            if (environment == null)
                return;

            var target = GameConfig.Current.ScrollSpeed;

            if (!Mathf.Approximately(environment.Speed, target))
                environment.Speed = target;
        }

        public void StartRun()
        {
            if (State != PlayState.Ready && State != PlayState.Cutscene && State != PlayState.Boss)
                return;

            SetState(PlayState.Running);
        }

        public void Pause()
        {
            if (State != PlayState.Running && State != PlayState.Cutscene && State != PlayState.Boss)
                return;

            stateBeforePause = State;
            Time.timeScale = 0f;
            SetState(PlayState.Paused);

            if (pausePanel != null)
                pausePanel.Show();
        }

        public void Resume()
        {
            if (State != PlayState.Paused)
                return;

            if (pausePanel != null)
                pausePanel.Hide();

            Time.timeScale = 1f;
            SetState(stateBeforePause);
        }

        public void Clear()
        {
            if (IsTerminal)
                return;

            SetState(PlayState.Cleared);
            PlaySfx(EndingSfxId);
            ReportClear();

            if (resultPanel != null)
                resultPanel.Show(true);
        }

        public void Fail()
        {
            if (State == PlayState.Failed || State == PlayState.Cleared)
                return;

            if (spawnerReady)
                spawner.Stop();

            LedgeHandler?.Stop();

            SetState(PlayState.Failed);
            PlaySfx(FailSfxId);

            if (resultPanel != null)
                resultPanel.Show(false);
        }

        public void Retry()
        {
            LoadScene(playScene);
        }

        public void GoTitle()
        {
            ResetCheckpoint();
            LoadScene(startScene);
        }

        public static void ResetCheckpoint()
        {
            checkpointStep = -1;
            checkpointTag = string.Empty;
        }

        public static void ResetSession()
        {
            seenCutscenes.Clear();
            ResetCheckpoint();
        }

        public void SetBossCheckpoint(string tag)
        {
            if (State != PlayState.Boss || string.IsNullOrEmpty(tag))
                return;

            checkpointTag = tag;
        }

        public void ReportImpact()
        {
            if (State != PlayState.Running && State != PlayState.Boss)
                return;

            RunHitReactionAsync();
        }

        private async Awaitable RunFlowAsync(int startStep)
        {
            if (flowRunning)
                return;

            flowRunning = true;

            try
            {
                BuildSteps();

                var start = startStep;

                if (start < 0)
                    start = checkpointStep < 0 ? 0 : Mathf.Clamp(checkpointStep, 0, steps.Count - 1);
                else
                    start = Mathf.Clamp(start, 0, steps.Count - 1);

                var i = start;
                ApplySkippedSteps(i);

                while (i < steps.Count)
                {
                    if (this == null || IsTerminal)
                        return;

                    var step = steps[i];
                    switch (step.Kind)
                    {
                        case FlowStepKind.Intro:
                            await PlayCutsceneAsync(introCutsceneId);
                            break;
                        case FlowStepKind.Section:
                            checkpointStep = i;
                            checkpointTag = string.Empty;
                            await RunSectionAsync(step.Index);
                            break;
                        case FlowStepKind.Cutscene:
                            await PlayCutsceneAsync(GetSectionCutsceneId(step.Index));
                            break;
                        case FlowStepKind.Boss:
                            checkpointStep = i;
                            await RunBossAsync(step.Index);
                            break;
                        case FlowStepKind.Ending:
                            await PlayCutsceneAsync(endingCutsceneId);
                            break;
                    }

                    if (this == null)
                        return;

                    if (pendingStep >= 0)
                    {
                        i = pendingStep;
                        pendingStep = -1;
                        ApplySkippedSteps(i);
                        continue;
                    }

                    if (IsTerminal)
                        return;

                    i++;
                }

                if (this == null || IsTerminal)
                    return;

                Clear();
            }
            finally
            {
                flowRunning = false;
            }
        }

        public bool DebugJumpTo(int slot)
        {
            if (steps.Count == 0)
                BuildSteps();

            if (!TryGetDebugStep(slot, out var stepIndex))
            {
                Debug.LogWarning($"[PlayFlow] Debug slot {slot} has no matching step.", this);
                return false;
            }

            PrepareDebugJump();

            if (flowRunning)
            {
                pendingStep = stepIndex;
                return true;
            }

            RunFlowFrom(stepIndex);
            return true;
        }

        private async void RunFlowFrom(int stepIndex)
        {
            await RunFlowAsync(stepIndex);
        }

        private bool TryGetDebugStep(int slot, out int stepIndex)
        {
            stepIndex = -1;

            if (slot < 1 || slot > DebugSlotCount)
                return false;

            if (slot == DebugSlotCount)
                return TryFindStep(FlowStepKind.Ending, 0, out stepIndex);

            var kind = slot % 2 == 1 ? FlowStepKind.Section : FlowStepKind.Boss;
            return TryFindStep(kind, (slot + 1) / 2, out stepIndex);
        }

        private bool TryFindStep(FlowStepKind kind, int index, out int stepIndex)
        {
            for (var i = 0; i < steps.Count; i++)
            {
                if (steps[i].Kind != kind || steps[i].Index != index)
                    continue;

                stepIndex = i;
                return true;
            }

            stepIndex = -1;
            return false;
        }

        private void HandleDebugInput()
        {
            var keyboard = Keyboard.current;

            if (keyboard == null)
                return;

            for (var slot = 0; slot < debugSlotKeys.Length; slot++)
            {
                if (!keyboard[debugSlotKeys[slot]].wasPressedThisFrame)
                    continue;

                if (!GameConfig.Current.DebugEnabled)
                {
                    Debug.LogWarning(
                        $"[PlayFlow] Debug jump {slot + 1} was ignored because debugEnabled is false.", this);
                    return;
                }

                DebugJumpTo(slot + 1);
                return;
            }
        }

        private void PrepareDebugJump()
        {
            IsBossInterrupted = true;
            Time.timeScale = 1f;
            bannerHold = false;
            checkpointTag = string.Empty;

            if (pausePanel != null)
                pausePanel.Hide();

            if (resultPanel != null)
                resultPanel.Hide();

            if (bossTimer != null)
                bossTimer.Hide();

            if (hud != null)
            {
                hud.HideBanner();
                hud.HideControlHint();
            }

            if (spawnerReady)
            {
                spawner.Stop();
                spawner.ReleaseAll();
            }

            LedgeHandler?.Stop();

            MarkCutscenesSeen();

            if (cutscene != null && cutscene.IsPlaying)
                cutscene.Skip();

            if (player != null)
            {
                player.QteCaptureInput = false;
                player.CancelJump();
                player.ResetHit();
                player.ResetJumpCooldown();
                player.SnapToLane(player.CurrentLane);
            }

            sectionTime = 0f;
            stateBeforePause = PlayState.Running;
            SetState(PlayState.Ready);
        }

        private void MarkCutscenesSeen()
        {
            if (!string.IsNullOrEmpty(introCutsceneId))
                seenCutscenes.Add(introCutsceneId);

            if (!string.IsNullOrEmpty(endingCutsceneId))
                seenCutscenes.Add(endingCutsceneId);

            if (sectionCutsceneIds == null)
                return;

            for (var i = 0; i < sectionCutsceneIds.Length; i++)
                if (!string.IsNullOrEmpty(sectionCutsceneIds[i]))
                    seenCutscenes.Add(sectionCutsceneIds[i]);
        }

        private void BuildSteps()
        {
            steps.Clear();
            steps.Add(new FlowStep(FlowStepKind.Intro, 0));

            var sectionCount = Mathf.Max(1, GameConfig.Current.SectionCount);
            for (var section = 1; section <= sectionCount; section++)
            {
                steps.Add(new FlowStep(FlowStepKind.Section, section));

                if (section < sectionCount)
                {
                    steps.Add(new FlowStep(FlowStepKind.Cutscene, section));
                    steps.Add(new FlowStep(FlowStepKind.Boss, section));
                }
            }

            steps.Add(new FlowStep(FlowStepKind.Ending, 0));
        }

        private void ApplySkippedSteps(int start)
        {
            Distance = 0f;
            Section = 1;

            for (var i = 0; i < start && i < steps.Count; i++)
            {
                var step = steps[i];
                if (step.Kind == FlowStepKind.Section)
                {
                    Distance += GameConfig.Current.GetSectionLength(step.Index);
                    Section = step.Index;
                }
                else if (step.Kind == FlowStepKind.Cutscene)
                {
                    ApplyCutsceneFinalState(GetSectionCutsceneId(step.Index));
                }
                else if (step.Kind == FlowStepKind.Intro)
                {
                    ApplyCutsceneFinalState(introCutsceneId);
                }
            }

            sectionStart = Distance;
            sectionDuration = GameConfig.Current.GetSectionDuration(Section);
            sectionLength = GameConfig.Current.GetSectionLength(Section);
            sectionTime = 0f;
            RestorePlayerPosition();
        }

        private async Awaitable RunSectionAsync(int section)
        {
            Section = section;
            sectionStart = Distance;
            sectionDuration = GameConfig.Current.GetSectionDuration(section);
            sectionLength = GameConfig.Current.GetSectionLength(section);
            sectionTime = 0f;

            EnsureSpawner();
            if (spawnerReady)
                spawner.BeginSectionByDuration(section - 1, sectionDuration);

            LedgeHandler?.BeginSection(section, sectionDuration);

            try
            {
                SectionStarted?.Invoke(section);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }

            if (environment != null)
                environment.SetHomeland(section >= GameConfig.Current.SectionCount);

            StartRun();

            if (section == 1 && hud != null)
                hud.ShowControlHint(GameConfig.Current.ControlHintDuration);

            while (this != null && !IsTerminal && pendingStep < 0 && !IsSectionFinished())
                await Awaitable.NextFrameAsync();

            if (this != null)
                LedgeHandler?.Stop();
        }

        private bool IsSectionFinished()
        {
            var reached = sectionTime >= sectionDuration;
            if (!spawnerReady)
                return reached;

            return reached && spawner.IsSectionExhausted;
        }

        private async Awaitable RunBossAsync(int index)
        {
            SetState(PlayState.Boss);

            if (bossTimer != null)
                bossTimer.SetBossName(GetBossName(index));

            var holdsWorld = BossHoldsWorldQuery != null && BossHoldsWorldQuery(index);

            if (holdsWorld)
            {
                await RampEnvironmentSpeedAsync(1f, 0f, bossWorldRampDuration);

                if (this == null || IsTerminal || pendingStep >= 0)
                    return;

                SetBossHoldsWorld(true);
            }

            await ShowBannerAsync(string.Format(BossBannerFormat, index, GetBossName(index)),
                GameConfig.Current.BossBannerDuration);

            if (this == null || IsTerminal || pendingStep >= 0)
                return;

            if (BossHandler == null)
            {
                await Awaitable.NextFrameAsync();
                return;
            }

            IsBossInterrupted = false;
            var survived = await BossHandler(index);
            if (this == null || pendingStep >= 0 || IsTerminal)
                return;

            if (!survived)
            {
                Fail();
                return;
            }

            checkpointTag = string.Empty;
            PlaySfx(BossClearSfxId);
            await ShowBannerAsync(ClearBannerText, GameConfig.Current.BossClearBannerDuration);

            if (this == null || IsTerminal || pendingStep >= 0 || !holdsWorld)
                return;

            SetBossHoldsWorld(false);
            await RampEnvironmentSpeedAsync(0f, 1f, bossWorldRampDuration);
        }

        private async Awaitable RampEnvironmentSpeedAsync(float from, float to, float seconds)
        {
            if (environment == null)
                return;

            var full = GameConfig.Current.ScrollSpeed;
            environment.SetScrolling(true);

            if (seconds <= 0f)
            {
                environment.Speed = full * to;
                return;
            }

            var elapsed = 0f;

            while (elapsed < seconds)
            {
                if (this == null || IsTerminal || pendingStep >= 0 || State != PlayState.Boss)
                    return;

                elapsed += Time.deltaTime;
                environment.Speed = full * Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / seconds));
                await Awaitable.NextFrameAsync();
            }

            if (this == null)
                return;

            environment.Speed = full * to;
        }

        private async Awaitable ShowBannerAsync(string text, float seconds)
        {
            if (hud == null || seconds <= 0f)
                return;

            bannerHold = true;
            ApplyInputEnabled();

            await hud.ShowBannerAsync(text, seconds);

            if (this == null)
                return;

            bannerHold = false;
            ApplyInputEnabled();
        }

        private void ApplyInputEnabled()
        {
            if (player == null)
                return;

            player.InputEnabled = !bannerHold && (State == PlayState.Running || State == PlayState.Boss);
        }

        private string GetBossName(int index)
        {
            var i = index - 1;
            return bossNames != null && i >= 0 && i < bossNames.Length ? bossNames[i] : string.Empty;
        }

        private void EnsureSpawner()
        {
            if (spawnerReady || spawner == null)
                return;

            if (spawner.Settings == null)
            {
                Debug.LogWarning("[PlayFlow] ObstacleSpawner has no settings. Obstacles are disabled.", this);
                return;
            }

            var root = scroller != null ? scroller.transform : spawner.ScrollRoot;
            var seed = GameConfig.Current.SpawnSeed;
            if (seed == 0)
                seed = unchecked((int)(DateTime.UtcNow.Ticks & 0x7FFFFFFF));

            spawner.Initialize(spawner.Settings, root, seed);
            spawnerReady = spawner.IsInitialized;
        }

        private async Awaitable PlayCutsceneAsync(string cutsceneId)
        {
            if (cutscene == null || string.IsNullOrEmpty(cutsceneId))
                return;

            if (seenCutscenes.Contains(cutsceneId))
            {
                ApplyCutsceneFinalState(cutsceneId);
                RestorePlayerPosition();
                return;
            }

            if (!CutsceneCatalog.TryCreate(cutsceneId, out var instance))
            {
                Debug.LogWarning($"[PlayFlow] Unknown cutscene id: '{cutsceneId}'.", this);
                return;
            }

            SetState(PlayState.Cutscene);
            PlaySfx(CutsceneTransitionSfxId);
            await cutscene.PlayAsync(instance);
            if (this == null)
                return;

            seenCutscenes.Add(cutsceneId);
            RestorePlayerPosition();
        }

        private void ApplyCutsceneFinalState(string cutsceneId)
        {
            if (cutscene == null || string.IsNullOrEmpty(cutsceneId))
                return;

            if (CutsceneCatalog.TryCreate(cutsceneId, out var instance))
                cutscene.ApplyFinalState(instance);
        }

        private string GetSectionCutsceneId(int section)
        {
            var index = section - 1;
            return sectionCutsceneIds != null && index >= 0 && index < sectionCutsceneIds.Length
                ? sectionCutsceneIds[index]
                : string.Empty;
        }

        private void RestorePlayerPosition()
        {
            if (player != null)
                player.SnapToLane(player.CurrentLane);
        }

        private async void LoadScene(SceneReference scene)
        {
            if (scene == null)
            {
                Debug.LogError("[PlayFlow] Scene reference is not assigned.", this);
                return;
            }

            if (!SceneController.TryGetInstance(out var scenes) || scenes.IsTransitioning)
                return;

            Time.timeScale = 1f;
            await scenes.LoadAsync(scene);
        }

        private void OnRetryInput()
        {
            if (State == PlayState.Failed)
                Retry();
        }

        private void OnPauseInput()
        {
            if (State == PlayState.Running || State == PlayState.Cutscene || State == PlayState.Boss)
            {
                Pause();
                return;
            }

            if (State != PlayState.Paused)
                return;

            if (pausePanel != null && pausePanel.IsSettingsOpen)
                pausePanel.CloseSettings();
            else
                Resume();
        }

        private void OnHit(Hazard hazard)
        {
            if (State != PlayState.Running && State != PlayState.Boss)
                return;

            RunHitReactionAsync();
        }

        private async void RunHitReactionAsync()
        {
            if (spawnerReady)
                spawner.Stop();

            SetState(PlayState.Hit);
            PlaySfx(HitSfxId);

            var config = GameConfig.Current;
            if (player != null)
                await player.PlayHitReactionAsync(config.HitStopDuration, config.HitPushDistance);

            if (this == null)
                return;

            Fail();
        }

        private void OnLedgeFailed(int section)
        {
            ReportImpact();
        }

        private void OnLaneChanged(int lane)
        {
            PlaySfx(LaneMoveSfxId);
        }

        private void OnJumped()
        {
            PlaySfx(JumpSfxId);
        }

        private void OnLanded()
        {
            PlaySfx(LandSfxId);
        }

        public void SetBossHoldsWorld(bool holds)
        {
            BossHoldsWorld = holds;

            if (environment != null)
                environment.SetScrolling(EnvironmentScrolls(State));
        }

        private bool EnvironmentScrolls(PlayState state)
        {
            if (state == PlayState.Running)
                return true;

            return state == PlayState.Boss && !BossHoldsWorld;
        }

        private void SetState(PlayState next)
        {
            State = next;

            ApplyInputEnabled();

            if (next != PlayState.Running)
                ResetWorldSpeedScale();

            if (scroller != null)
                scroller.SetScrolling(next == PlayState.Running);

            if (environment != null)
                environment.SetScrolling(EnvironmentScrolls(next));

            if (hud != null)
                hud.gameObject.SetActive(next != PlayState.Cutscene);

            try
            {
                StateChanged?.Invoke(next);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private static void PlaySfx(string id)
        {
            if (AudioManager.TryGetInstance(out var audio) && audio.IsInitialized)
                audio.PlayGlobal(id);
        }

        private static async void ReportClear()
        {
            await RecordService.ReportClearAsync();
        }

        private void Subscribe()
        {
            if (subscribed)
                return;

            subscribed = true;

            if (player != null)
            {
                player.Hit += OnHit;
                player.LaneChanged += OnLaneChanged;
                player.Jumped += OnJumped;
                player.Landed += OnLanded;
            }
        }

        private void Unsubscribe()
        {
            if (!subscribed)
                return;

            subscribed = false;

            if (player != null)
            {
                player.Hit -= OnHit;
                player.LaneChanged -= OnLaneChanged;
                player.Jumped -= OnJumped;
                player.Landed -= OnLanded;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            seenCutscenes.Clear();
            checkpointStep = -1;
            checkpointTag = string.Empty;
        }
    }
}
