using System;
using System.Collections.Generic;
using Game.Config;
using UnityEngine;

namespace Game.Spawner
{
    public sealed class ObstacleSpawner : MonoThing
    {
        [SerializeField] private ObstacleSpawnerSettings settings;
        [SerializeField] private Transform scrollRoot;

        private SpawnModule spawn;

        public ObstacleSpawnerSettings Settings => settings;
        public Transform ScrollRoot => scrollRoot;
        public SpawnModule Spawn => EnsureModule();
        public bool IsInitialized => spawn != null && spawn.IsInitialized;
        public bool IsRunning => spawn != null && spawn.IsRunning;
        public bool SpawningEnabled => spawn == null || spawn.SpawningEnabled;
        public bool IsSectionExhausted => spawn != null && spawn.IsSectionExhausted;
        public int ActiveObstacleCount => spawn != null ? spawn.ActiveObstacleCount : 0;
        public int SectionIndex => spawn != null ? spawn.SectionIndex : -1;
        public float SectionTime => spawn != null ? spawn.SectionTime : 0f;
        public float SectionProgress => spawn != null ? spawn.SectionProgress : 0f;
        public IReadOnlyList<PatternAnalysis> Analyses => spawn != null ? spawn.Analyses : null;

        public event Action<PatternPlacement> PatternSpawned
        {
            add => EnsureModule().PatternSpawned += value;
            remove => EnsureModule().PatternSpawned -= value;
        }

        private void Awake()
        {
            EnsureModule();
        }

        private void OnDisable()
        {
            if (spawn != null)
                spawn.ReleaseAll();
        }

        public void Initialize(int seed)
        {
            Initialize(settings, scrollRoot, seed);
        }

        public void Initialize(ObstacleSpawnerSettings settings, Transform scrollRoot, int seed)
        {
            if (settings != null)
                this.settings = settings;

            if (scrollRoot != null)
                this.scrollRoot = scrollRoot;

            EnsureModule().Initialize(this.settings, this.scrollRoot, seed);
        }

        public void BeginSection(int sectionIndex)
        {
            BeginSection(sectionIndex, GameConfig.Current.GetSectionLength(sectionIndex + 1));
        }

        public void BeginSection(int sectionIndex, float sectionLength)
        {
            EnsureModule().BeginSection(sectionIndex, sectionLength);
        }

        public void BeginSectionByDuration(int sectionIndex, float durationSeconds)
        {
            EnsureModule().BeginSectionByDuration(sectionIndex, durationSeconds);
        }

        public void Advance(float deltaTime, float distance, in SpawnPlayerState player)
        {
            EnsureModule().Advance(deltaTime, distance, player);
        }

        public void Stop()
        {
            if (spawn != null)
                spawn.Stop();
        }

        public void SetSpawningEnabled(bool enabled)
        {
            EnsureModule().SpawningEnabled = enabled;
        }

        public void ReleaseAll()
        {
            if (spawn != null)
                spawn.ReleaseAll();
        }

        private SpawnModule EnsureModule()
        {
            if (spawn == null)
                spawn = AddModule(new SpawnModule());

            return spawn;
        }
    }
}
