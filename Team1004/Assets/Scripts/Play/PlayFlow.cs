using System;
using System.Collections.Generic;
using Game.Config;
using Game.Cutscene;
using Game.Environment;
using Game.Player;
using Game.Settings;
using Game.Spawner;
using UnityEngine;

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

        private static readonly HashSet<string> seenCutscenes = new(StringComparer.Ordinal);
        private static int checkpointStep = -1;

        [SerializeField] private LanePlayer player;
        [SerializeField] private StageScroller scroller;
        [SerializeField] private ObstacleSpawner spawner;
        [SerializeField] private PlayHud hud;
        [SerializeField] private BossTimerView bossTimer;
        [SerializeField] private PausePanel pausePanel;
        [SerializeField] private ResultPanel resultPanel;
        [SerializeField] private CutscenePlayer cutscene;
        [SerializeField] private EnvironmentThing environment;
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
        [SerializeField] private string[] bossNames = { "낚싯줄", "바다코끼리", "폭포" };

        private readonly List<FlowStep> steps = new();
        private bool listening;
        private bool subscribed;
        private bool spawnerReady;
        private float sectionStart;
        private float sectionLength;
        private float sectionDuration;
        private float sectionTime;
        private PlayState stateBeforePause = PlayState.Running;

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
        public PlayHud Hud => hud;
        public ResultPanel ResultPanel => resultPanel;
        public bool IsResultVisible => resultPanel != null && resultPanel.gameObject.activeSelf;
        public static IReadOnlyCollection<string> SeenCutscenes => seenCutscenes;
        public static int CheckpointStep => checkpointStep;

        public Func<int, Awaitable<bool>> BossHandler { get; set; }

        public event Action<PlayState> StateChanged;
        public event Action<int> SectionStarted;

        protected override void OnUnregistering()
        {
            Time.timeScale = 1f;
            Unsubscribe();
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

            SetState(PlayState.Ready);
            await RunFlowAsync();
        }

        private void Update()
        {
            if (State != PlayState.Running)
                return;

            Distance += GameConfig.Current.ScrollSpeed * Time.deltaTime;
            sectionTime += Time.deltaTime;

            if (spawnerReady && player != null)
            {
                var playerState = SpawnPlayerState.FromPlayer(
                    player, player.JumpCooldownRemaining, player.AirborneRemaining, player.MoveRemaining);
                spawner.Advance(Time.deltaTime, Distance, playerState);
            }
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
            checkpointStep = -1;
            LoadScene(startScene);
        }

        public static void ResetCheckpoint()
        {
            checkpointStep = -1;
        }

        public static void ResetSession()
        {
            seenCutscenes.Clear();
            checkpointStep = -1;
        }

        private async Awaitable RunFlowAsync()
        {
            BuildSteps();

            var start = Mathf.Clamp(checkpointStep, 0, steps.Count - 1);
            if (checkpointStep < 0)
                start = 0;

            ApplySkippedSteps(start);

            for (var i = start; i < steps.Count; i++)
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
            }

            if (this == null || IsTerminal)
                return;

            Clear();
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

            while (this != null && !IsTerminal && !IsSectionFinished())
                await Awaitable.NextFrameAsync();
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

            if (BossHandler == null)
            {
                await Awaitable.NextFrameAsync();
                return;
            }

            var survived = await BossHandler(index);
            if (this == null || IsTerminal)
                return;

            if (survived)
                PlaySfx(BossClearSfxId);
            else
                Fail();
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

        private async void OnHit(Hazard hazard)
        {
            if (State != PlayState.Running && State != PlayState.Boss)
                return;

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

        private void SetState(PlayState next)
        {
            State = next;

            if (player != null)
                player.InputEnabled = next == PlayState.Running || next == PlayState.Boss;

            if (scroller != null)
                scroller.SetScrolling(next == PlayState.Running);

            if (environment != null)
                environment.SetScrolling(next == PlayState.Running || next == PlayState.Boss);

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
        }
    }
}
