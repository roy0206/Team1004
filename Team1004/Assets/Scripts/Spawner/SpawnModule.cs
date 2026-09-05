using System;
using System.Collections.Generic;
using Game.Config;
using UnityEngine;

namespace Game.Spawner
{
    public sealed class SpawnModule : Module
    {
        private readonly List<ObstacleThing> activeThings = new List<ObstacleThing>();
        private readonly List<ObstacleDefinition> definitions = new List<ObstacleDefinition>();
        private readonly List<PatternSpec> specs = new List<PatternSpec>();

        private ObstacleSpawnerSettings settings;
        private Transform scrollRoot;
        private SimulationConfig config;
        private StateLayout layout;
        private StateSet frontier;
        private CourseGenerator generator;
        private DeterministicRandom variantRandom;
        private int seed;
        private List<PatternAnalysis> analyses;
        private float sectionTime;
        private float sectionLength;
        private float sectionDuration;
        private float sectionStartDistance;
        private bool hasSectionStart;
        private float progress;

        public event Action<PatternPlacement> PatternSpawned;

        protected override ModuleTick Ticks => ModuleTick.None;

        public bool IsInitialized => generator != null;
        public bool IsRunning { get; private set; }
        public bool SpawningEnabled { get; set; } = true;
        public int SectionIndex => generator != null ? generator.SectionIndex : -1;
        public float SectionTime => sectionTime;
        public float SectionProgress => progress;
        public float SectionDuration => sectionDuration;
        public int ActiveObstacleCount => activeThings.Count;
        public CourseGenerator Generator => generator;
        public SimulationConfig Config => config;
        public IReadOnlyList<PatternAnalysis> Analyses => analyses;
        public IReadOnlyList<PatternSpec> Specs => specs;
        public bool IsSectionExhausted => IsInitialized && (generator.SpawnStopped || progress >= 1f) && activeThings.Count == 0;

        public void Initialize(ObstacleSpawnerSettings settings, Transform scrollRoot, int seed)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            if (scrollRoot == null)
                throw new ArgumentNullException(nameof(scrollRoot));

            if (settings.Library == null || settings.Difficulty == null)
                throw new InvalidOperationException("ObstacleSpawnerSettings needs a PatternLibrary and a DifficultyCurve.");

            this.settings = settings;
            this.scrollRoot = scrollRoot;
            this.seed = seed;
            variantRandom = CreateVariantRandom(seed, 0);

            config = settings.CreateSimulationConfig(GameConfig.Current);
            if (!config.Validate(out var error))
                throw new InvalidOperationException("Spawner simulation config is invalid. " + error);

            settings.Library.BuildSpecs(specs);
            analyses = PatternAnalyzer.AnalyzeAll(specs, config, 1f);

            for (var i = 0; i < analyses.Count; i++)
            {
                var analysis = analyses[i];
                if (!analysis.Solvable)
                    Debug.LogWarning("[ObstacleSpawner] Pattern is not solvable and will never spawn: '" + analysis.Pattern.Id + "'. " + analysis.Error);
                else if (analysis.ManualMismatch)
                    Debug.LogWarning("[ObstacleSpawner] Pattern '" + analysis.Pattern.Id + "' is marked manualJumpRequired=" + analysis.Pattern.ManualJumpRequired + " but the simulator classified it as jumpRequired=" + analysis.JumpRequired + ". The simulator result is used.");
            }

            layout = StateLayout.From(config);
            frontier = new StateSet(layout);
            generator = new CourseGenerator(config, specs, analyses, settings.Difficulty.ToProfile(), seed);
            RegisterPools();
            IsRunning = false;
        }

        public void BeginSection(int sectionIndex, float sectionLength)
        {
            Begin(sectionIndex, sectionLength, 0f);
        }

        public void BeginSectionByDuration(int sectionIndex, float durationSeconds)
        {
            Begin(sectionIndex, 0f, durationSeconds);
        }

        private void Begin(int sectionIndex, float length, float duration)
        {
            EnsureInitialized();
            ReleaseAll();
            generator.BeginSection(sectionIndex);
            variantRandom = CreateVariantRandom(seed, sectionIndex);
            sectionTime = 0f;
            sectionLength = length;
            sectionDuration = duration;
            hasSectionStart = false;
            progress = 0f;
            SpawningEnabled = true;
            IsRunning = true;
        }

        public void Advance(float deltaTime, float distance, in SpawnPlayerState player)
        {
            if (!IsInitialized || !IsRunning)
                return;

            if (!hasSectionStart)
            {
                sectionStartDistance = distance;
                hasSectionStart = true;
            }

            sectionTime += Mathf.Max(0f, deltaTime);

            if (sectionDuration > 0f)
                progress = Mathf.Clamp01(sectionTime / sectionDuration);
            else
                progress = sectionLength > 0f ? Mathf.Clamp01((distance - sectionStartDistance) / sectionLength) : 1f;

            MoveAndRecycle(deltaTime);

            if (!SpawningEnabled)
                return;

            if (generator.SpawnStopped || sectionTime < generator.NextDecisionTime)
                return;

            frontier.Clear();
            frontier.Add(player.ToSimState(layout, config));

            if (generator.TryPlaceNext(sectionTime, progress, frontier, out var placement, out _))
                Spawn(placement);
        }

        public void Stop()
        {
            IsRunning = false;
        }

        public void ReleaseAll()
        {
            if (PoolManager.TryGetInstance(out var pool))
            {
                for (var i = 0; i < activeThings.Count; i++)
                {
                    var thing = activeThings[i];
                    if (thing == null)
                        continue;

                    var runtime = thing.Runtime;
                    if (runtime != null)
                        pool.Release(runtime.Instance);
                    else
                        pool.Release(thing.gameObject);
                }
            }

            activeThings.Clear();
        }

        protected override void OnDetached()
        {
            ReleaseAll();
        }

        private void RegisterPools()
        {
            settings.Library.CollectObstacles(definitions);

            if (!PoolManager.TryGetInstance(out var pool))
            {
                Debug.LogWarning("[ObstacleSpawner] PoolManager is not placed. Obstacles cannot be spawned.");
                return;
            }

            for (var i = 0; i < definitions.Count; i++)
            {
                var definition = definitions[i];

                if (definition.Prefab == null)
                {
                    Debug.LogWarning("[ObstacleSpawner] Obstacle definition has no prefab: '" + definition.PoolKey + "'.", definition);
                    continue;
                }

                if (!pool.IsRegistered(definition.PoolKey))
                    pool.Register(definition.PoolKey, definition.Prefab, settings.PrewarmPerObstacle);
            }
        }

        private void MoveAndRecycle(float deltaTime)
        {
            if (activeThings.Count == 0)
                return;

            PoolManager.TryGetInstance(out var pool);
            var scrollSpeed = GameConfig.Current.ScrollSpeed;

            for (var i = activeThings.Count - 1; i >= 0; i--)
            {
                var thing = activeThings[i];
                if (thing == null)
                {
                    activeThings.RemoveAt(i);
                    continue;
                }

                var runtime = thing.Runtime;
                if (runtime == null)
                {
                    activeThings.RemoveAt(i);
                    continue;
                }

                runtime.Advance(deltaTime, scrollSpeed);

                if (!runtime.IsOffScreen(config.ScreenLeftX, settings.DespawnMargin))
                    continue;

                activeThings.RemoveAt(i);
                if (pool != null)
                    pool.Release(runtime.Instance);
            }
        }

        private void Spawn(PatternPlacement placement)
        {
            if (!PoolManager.TryGetInstance(out var pool))
                return;

            var obstacles = placement.Obstacles;

            for (var i = 0; i < obstacles.Count; i++)
            {
                var timing = obstacles[i];
                var position = new Vector3(timing.SpawnCenterX, LaneCenterY(timing.StartLane, timing.Spec.LaneSpan), 0f);

                if (!pool.TryGet(timing.Spec.PoolKey, position, Quaternion.identity, out var instance, scrollRoot))
                {
                    Debug.LogWarning("[ObstacleSpawner] Pool is not registered for obstacle: '" + timing.Spec.PoolKey + "'.");
                    continue;
                }

                if (!instance.TryGetComponent(out ObstacleThing thing))
                {
                    Debug.LogWarning("[ObstacleSpawner] Obstacle prefab has no ObstacleThing on its root: '" + timing.Spec.PoolKey + "'.", instance.GameObject);
                    pool.Release(instance);
                    continue;
                }

                thing.AddModule(new ObstacleRuntimeModule(instance, timing));
                thing.ApplyVariant(NextVariantSeed());
                activeThings.Add(thing);
            }

            PatternSpawned?.Invoke(placement);
        }

        private uint NextVariantSeed()
        {
            variantRandom ??= CreateVariantRandom(seed, 0);
            return variantRandom.NextUInt();
        }

        private static DeterministicRandom CreateVariantRandom(int seed, int sectionIndex)
        {
            return new DeterministicRandom(unchecked(seed * 15731 + sectionIndex * 32749 + 3));
        }

        private static float LaneCenterY(int startLane, int laneSpan)
        {
            var values = GameConfig.Current;
            var sum = 0f;
            var count = Mathf.Max(1, laneSpan);

            for (var i = 0; i < count; i++)
                sum += values.GetLaneY(startLane + i);

            return sum / count;
        }

        private void EnsureInitialized()
        {
            if (!IsInitialized)
                throw new InvalidOperationException("ObstacleSpawner.Initialize must be called first.");
        }
    }
}
