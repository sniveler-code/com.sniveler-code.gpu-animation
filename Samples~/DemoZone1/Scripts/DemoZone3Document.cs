using System.Collections.Generic;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UIElements;

namespace SnivelerCode.GpuAnimation.DemoZone3
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class DemoZone3Document : MonoBehaviour
    {
        [SerializeField] private List<Transform> cameraPositions;
        [SerializeField] private Transform cameraTransform;

        private struct HashMap
        {
            public int RedWarriors;

            public int BlueWarriors;

            // False Sharing
            public int4 Trash1;
            public int4 Trash2;
            public int4 Trash3;
            public int2 Trash4;
        }

        private EntityManager _entityManager;
        private Label _blueWarriorsCount;
        private Label _redWarriorsCount;

        private Label _fpsLabel;
        private Label _gcLabel;

        private float _accumulatedTime;
        private int _framesCount;
        private long _lastFrameMemory;
        private long _currentAlloc;

        private void Start()
        {
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

            var queryRedWarriors = _entityManager.CreateEntityQuery(
                typeof(Demo3SpawnerData),
                typeof(Demo3DebugRedWarriorTag)
            );

            var queryBlueWarriors = _entityManager.CreateEntityQuery(
                typeof(Demo3SpawnerData),
                typeof(Demo3DebugBlueWarriorTag)
            );

            var document = GetComponent<UIDocument>();

            _fpsLabel = document.rootVisualElement.Q<Label>("StatsFps");
            _gcLabel = document.rootVisualElement.Q<Label>("StatsGc");

            document.rootVisualElement.Q<Button>("blueWarriors").clicked += () =>
                TriggerSpawner(queryBlueWarriors);

            document.rootVisualElement.Q<Button>("redWarriors").clicked += () =>
                TriggerSpawner(queryRedWarriors);

            _blueWarriorsCount = document.rootVisualElement.Q<Label>("blueWarriorsCount");
            _redWarriorsCount = document.rootVisualElement.Q<Label>("redWarriorsCount");
        }

        private void Update()
        {
            _accumulatedTime += Time.unscaledDeltaTime;
            _framesCount++;

            long currentMemory = Profiler.GetMonoUsedSizeLong();
            if (currentMemory > _lastFrameMemory)
            {
                _currentAlloc = currentMemory - _lastFrameMemory;
            }
            else if (currentMemory < _lastFrameMemory)
            {
                _currentAlloc = 0;
            }

            _lastFrameMemory = currentMemory;

            if (_accumulatedTime >= 0.5f)
            {
                float fps = _framesCount / _accumulatedTime;
                _fpsLabel.text = $"FPS: {Mathf.RoundToInt(fps)}";
                _accumulatedTime = 0f;
                _framesCount = 0;

                long allocKb = math.max(0, _currentAlloc);
                _gcLabel.text = $"GC Alloc: {allocKb / 1024.0:000} KB";
            }
        }

        private void TriggerSpawner(EntityQuery query)
        {
            if (query.IsEmpty) return;
            var entities = query.ToEntityArray(Allocator.Temp);
            foreach (var entity in entities)
            {
                var spawnData = _entityManager.GetComponentData<Demo3SpawnerData>(entity);
                spawnData.Progress = spawnData.SpawnTime;
                _entityManager.SetComponentData(entity, spawnData);
            }

            entities.Dispose();
        }

        private void LateUpdate()
        {
            if (Time.frameCount % 16 != 0) return;

            var unitsQuery = _entityManager.CreateEntityQuery(typeof(Demo3CombatData));
            var configHandle = _entityManager.GetComponentTypeHandle<Demo3UnitConfig>(true);
            var combatHandle = _entityManager.GetComponentTypeHandle<Demo3CombatData>(true);

            const int workerCount = JobsUtility.MaxJobThreadCount + 1;
            var threadResults = new NativeArray<HashMap>(workerCount, Allocator.TempJob);

            var job = new CalcHashChunkJob
            {
                ThreadResults = threadResults,
                ConfigHandle = configHandle,
                CombatHandle = combatHandle
            }.ScheduleParallel(unitsQuery, default);

            job.Complete();

            HashMap hashMap = default;
            for (int i = 0; i < threadResults.Length; i++)
            {
                hashMap.BlueWarriors += threadResults[i].BlueWarriors;
                hashMap.RedWarriors += threadResults[i].RedWarriors;
            }

            _blueWarriorsCount.text = hashMap.BlueWarriors.ToString();
            _redWarriorsCount.text = hashMap.RedWarriors.ToString();

            threadResults.Dispose();
        }

        [BurstCompile]
        private struct CalcHashChunkJob : IJobChunk
        {
            [NativeDisableParallelForRestriction] public NativeArray<HashMap> ThreadResults;
            [NativeSetThreadIndex] private int _threadIndex;
            [ReadOnly] public ComponentTypeHandle<Demo3UnitConfig> ConfigHandle;
            [ReadOnly] public ComponentTypeHandle<Demo3CombatData> CombatHandle;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask,
                in v128 chunkEnabledMask)
            {
                var configArray = chunk.GetNativeArray(ref ConfigHandle);
                var combatArray = chunk.GetNativeArray(ref CombatHandle);
                var localStats = ThreadResults[_threadIndex];

                for (int i = 0; i < configArray.Length; i++)
                {
                    ref readonly Demo3UnitConfigBlob staticData = ref configArray[i].Value.Value;
                    if (staticData.UnityType == Demo3UnitType.Melee && combatArray[i].Team == Demo3Faction.Blue)
                    {
                        localStats.BlueWarriors++;
                    }

                    if (staticData.UnityType == Demo3UnitType.Melee && combatArray[i].Team == Demo3Faction.Red)
                    {
                        localStats.RedWarriors++;
                    }
                }

                ThreadResults[_threadIndex] = localStats;
            }
        }
    }
}
