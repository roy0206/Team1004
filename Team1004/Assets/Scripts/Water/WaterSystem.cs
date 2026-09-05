using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.LowLevel;

namespace Game.Water
{
    public class WaterSystem
    {
        static WaterSystem _instance;
        protected static WaterSystem instance => _instance ??= new WaterSystem();

        List<Water> _waters = new();
        List<WaterBody> _bodies = new();
        WaterNodeMappingData _waterNodeMappingData = new();
        SimulationData _simulationData = new();

        WaterSettings settings => WaterSettings.currentSettings;
        public static Vector2 simulationCenter => Camera.main.transform.position;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void RuntimeInitializeOnLoad()
        {
            _instance = new WaterSystem();
            PlayerLoopSystem system = new PlayerLoopSystem
            {
                type = typeof(UpdateWaterSystem),
                updateDelegate = () => instance.Update()
            };
            system.RegisterTo<UnityEngine.PlayerLoop.PostLateUpdate>();
            Application.quitting += () => instance._simulationData.Dispose();
        }
        void Register_Internal(Water water) => _waters.Add(water);
        void Deregister_Internal(Water water) => _waters.Remove(water);

        void Update()
        {
            UpdatePhysics();
        }

        bool GetPositions_Internal(Water water, out NativeArray<float> positions, out RangeInt waterRange)
        {
            (positions, waterRange) = (default, default);
            if (!_simulationData.Contains(water)) return false;
            waterRange = _simulationData.mappingData.GetWaterRange(water);
            positions = _simulationData.GetPositions(water);
            return true;
        }

        bool Collide_Internal(Water water, IWaterCollider body)
        {
            if (body == null || water == null) return false;
            int index = _simulationData.IndexOf(water);
            if (index < 0) return false;
            return Collide_Internal(index, water, body, body.Bounds);
        }

        bool Collide_Internal(int index, Water water, IWaterCollider body, Bounds bounds)
        {
            var config = settings;
            if (config == null) return false;
            var waterRange = _simulationData.mappingData.waterRanges[index];
            var intersect = waterRange.Intersect(water.InnerIndexRange(bounds.min.x, bounds.max.x));
            if (intersect.length <= 0) return false;

            var positions = _simulationData.GetPositions(index);
            var velocities = _simulationData.GetVelocities(index);
            var bodyVelocity = body.VerticalVelocity;
            var horizontal = body.HorizontalVelocity;
            var influence = body.SurfaceInfluence;
            var wakeTransfer = config.wakeVelocityTransfer * Mathf.Max(0f, body.WakeScale);
            var cap = config.MaxInjectedVelocity;
            var transfersVertical = body.TransfersVerticalVelocity;
            var surfaceY = water.bounds.yMax;
            var wake = wakeTransfer > 0f && Mathf.Abs(horizontal) > 0f && bounds.size.y > 0f;
            var touched = false;

            for (int i = intersect.start; i < intersect.end; i++)
            {
                int waterIndex = i - waterRange.start;
                float py = positions[waterIndex];
                float v = velocities[waterIndex];
                float nodeX = water.IndexToX(i);
                float nodeY = py + surfaceY;

                if (Mathf.Abs(py) < config.surfaceCollisionDistance && body.OverlapPoint(new Vector2(nodeX, nodeY)))
                {
                    touched = true;
                    if (transfersVertical && (Mathf.Abs(bodyVelocity) > Mathf.Abs(v) || bodyVelocity * v < 0))
                    {
                        var injected = bodyVelocity * config.collisionVelocityTransfer
                            * (1f - Mathf.Abs(py) / config.surfaceCollisionDistance);
                        v = WaterWake.CapInjection(v, injected, cap);
                        velocities[waterIndex] = v;
                    }
                }

                if (!wake) continue;

                float wakeVelocity = WaterWake.CapInjection(v, WaterWake.NodeVelocity(
                    v, nodeX, nodeY, bounds, horizontal, influence, wakeTransfer), cap);

                if (wakeVelocity != v) velocities[waterIndex] = wakeVelocity;
            }
            return touched;
        }

        bool CollideAll_Internal(IWaterCollider body)
        {
            if (body == null) return false;
            var config = settings;
            if (config == null) return false;
            var bounds = body.Bounds;
            var reach = Mathf.Max(config.surfaceCollisionDistance, body.SurfaceInfluence);
            var touched = false;
            var waters = _simulationData.waters;
            for (int i = 0; i < waters.Count; i++)
            {
                var water = waters[i];
                if (water == null) continue;
                var rect = water.bounds;
                if (bounds.max.x < rect.xMin || bounds.min.x > rect.xMax) continue;
                if (bounds.max.y < rect.yMax - reach || bounds.min.y > rect.yMax + reach) continue;
                touched |= Collide_Internal(i, water, body, bounds);
            }
            return touched;
        }

        void UpdatePhysics()
        {
            _waterNodeMappingData.Clear();
            foreach (var water in _waters) _waterNodeMappingData.Add(water, settings, simulationCenter);
            _simulationData.Update(_waterNodeMappingData);

            foreach (var water in _simulationData.waters)
            {
                var velocities = _simulationData.GetVelocities(water);
                var positions = _simulationData.GetPositions(water);
                for (int i = 0; i< settings.iterationsPerFrame; i++) {
                    var job = new WaterSimulationJobs.VelocityJob(settings, velocities, positions, Time.deltaTime/settings.iterationsPerFrame);
                    var handle = job.Schedule(job.velocities.Length, 8);
                    handle.Complete();
                    var job2 = new WaterSimulationJobs.PositionJob(velocities, positions, Time.deltaTime/settings.iterationsPerFrame);
                    var handle2 = job2.Schedule(job2.velocities.Length, 8);
                    handle2.Complete();
                }
            }
        }

        public static void Register(Water water) => instance.Register_Internal(water);
        public static void Deregister(Water water) => instance.Deregister_Internal(water);
        public static bool Collide(Water water, IWaterCollider body) => instance.Collide_Internal(water, body);
        public static bool CollideAll(IWaterCollider body) => instance.CollideAll_Internal(body);
        public static bool GetPositions(Water water, out NativeArray<float> positions, out RangeInt range)
            => instance.GetPositions_Internal(water, out positions, out range);

        class WaterNodeMappingData
        {
            public int totalNodeCount;
            public List<Water> waters = new();
            public List<RangeInt> nodeRanges = new();
            public List<RangeInt> waterRanges = new();

            public void Add(Water water, WaterSettings settings, Vector2 simulationCenter)
            {
                float r = settings.simulationDistance;
                float nodePerUnit = settings.nodePerUnit;
                if (!water.simulatable) return;

                float y = water.bounds.yMax - simulationCenter.y;
                float sqrt = Mathf.Sqrt(r * r - y * y);
                if (float.IsNaN(sqrt)) return;
                RangeInt range = water.OuterIndexRange(simulationCenter.x - sqrt, simulationCenter.x + sqrt);

                waters.Add(water);
                nodeRanges.Add(new RangeInt(totalNodeCount, range.length));
                waterRanges.Add(range);
                totalNodeCount += range.length;
            }

            public RangeInt GetNodeRange(Water water) => nodeRanges[waters.IndexOf(water)];
            public RangeInt GetWaterRange(Water water) => waterRanges[waters.IndexOf(water)];

            public void CopyFrom(WaterNodeMappingData other)
            {
                totalNodeCount = other.totalNodeCount;
                waters.Clear();
                nodeRanges.Clear();
                waterRanges.Clear();
                waters.AddRange(other.waters);
                nodeRanges.AddRange(other.nodeRanges);
                waterRanges.AddRange(other.waterRanges);
            }

            public void Clear()
            {
                totalNodeCount = 0;
                waters.Clear();
                nodeRanges.Clear();
                waterRanges.Clear();
            }
        }
        class SimulationData : IDisposable
        {
            public NativeArray<float> velocities;
            public NativeArray<float> positions;
            public WaterNodeMappingData mappingData = new();
            public List<Water> waters => mappingData.waters;
            public void Update(WaterNodeMappingData newMappingData)
            {
                NativeArray<float> newVelocities = new (newMappingData.totalNodeCount, Allocator.Persistent);
                NativeArray<float> newPositions = new (newMappingData.totalNodeCount, Allocator.Persistent);

                foreach (var water in newMappingData.waters)
                {
                    if (!mappingData.waters.Contains(water)) continue;
                    RangeInt nodeRange = mappingData.GetNodeRange(water);
                    RangeInt waterRange = mappingData.GetWaterRange(water);
                    RangeInt newNodeRange = newMappingData.GetNodeRange(water);
                    RangeInt newWaterRange = newMappingData.GetWaterRange(water);
                    RangeInt waterIntersect = waterRange.Intersect(newWaterRange);
                    if (waterIntersect.length == 0) continue;

                    NativeArray<float> source =
                        velocities.GetSubArray(nodeRange.start + waterIntersect.start - waterRange.start, waterIntersect.length);
                    NativeArray<float> target =
                        newVelocities.GetSubArray(newNodeRange.start + waterIntersect.start - newWaterRange.start, waterIntersect.length);
                    target.CopyFrom(source);

                    source = positions.GetSubArray(nodeRange.start + waterIntersect.start - waterRange.start, waterIntersect.length);
                    target = newPositions.GetSubArray(newNodeRange.start + waterIntersect.start - newWaterRange.start, waterIntersect.length);
                    target.CopyFrom(source);
                }

                mappingData.CopyFrom(newMappingData);
                if (velocities.IsCreated) velocities.Dispose();
                if (positions.IsCreated) positions.Dispose();
                velocities = newVelocities;
                positions = newPositions;
            }

            public NativeArray<float> GetVelocities(Water water) => velocities.GetSubArray( mappingData.GetNodeRange(water));
            public NativeArray<float> GetPositions(Water water) => positions.GetSubArray( mappingData.GetNodeRange(water));

            public NativeArray<float> GetVelocities(int index) => velocities.GetSubArray(mappingData.nodeRanges[index]);
            public NativeArray<float> GetPositions(int index) => positions.GetSubArray(mappingData.nodeRanges[index]);

            public int IndexOf(Water water) => mappingData.waters.IndexOf(water);
            public bool Contains(Water water) => mappingData.waters.Contains(water);
            public void Dispose()
            {
                if (velocities.IsCreated) velocities.Dispose();
                if (positions.IsCreated) positions.Dispose();
            }
        }
        struct UpdateWaterSystem
        {
        }
    }
}
