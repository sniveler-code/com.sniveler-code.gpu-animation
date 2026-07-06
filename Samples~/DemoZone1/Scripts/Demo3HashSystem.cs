using System.Collections.Generic;
using SnivelerCode.GpuAnimation.DemoZone3;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace SnivelerCode.GpuAnimation.DemoZone3
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(Demo3CombatDecisionSystem))]
    public partial struct Demo3HashSystem : ISystem
    {
        private EntityQuery _aliveUnitsQuery;
        private int _microGridWidth;
        private int _microGridHeight;

        public NativeArray<Demo3SpatialData> SortedSpatialData;
        public NativeArray<int2> MicroGridOffsets;
        public NativeArray<HeatmapCell> Heatmap;
        public int MicroGridWidth => _microGridWidth;
        public int MicroGridHeight => _microGridHeight;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _aliveUnitsQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<LocalTransform, Demo3CombatData>()
                .WithNone<Demo3DeadData>()
                .Build(ref state);

            state.RequireForUpdate(_aliveUnitsQuery);
            state.RequireForUpdate<Demo3BattleData>();
        }


        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            if (SortedSpatialData.IsCreated) SortedSpatialData.Dispose();
            if (MicroGridOffsets.IsCreated) MicroGridOffsets.Dispose();
            if (Heatmap.IsCreated) Heatmap.Dispose();
        }

        public void OnUpdate(ref SystemState state)
        {
            var battle = SystemAPI.GetSingleton<Demo3BattleData>();
            int entityCount = _aliveUnitsQuery.CalculateEntityCount();

            if (entityCount == 0) return;

            const float mapSize = 200f;
            _microGridWidth = (int) math.ceil(mapSize / battle.MicroCellSize);
            _microGridHeight = (int) math.ceil(mapSize / battle.MicroCellSize);
            int totalMicroCells = _microGridWidth * _microGridHeight;

            if (!SortedSpatialData.IsCreated || SortedSpatialData.Length < entityCount)
            {
                if (SortedSpatialData.IsCreated) SortedSpatialData.Dispose();
                SortedSpatialData =
                    new NativeArray<Demo3SpatialData>(math.max(entityCount, 50000), Allocator.Persistent);
            }

            if (!MicroGridOffsets.IsCreated || MicroGridOffsets.Length < totalMicroCells)
            {
                if (MicroGridOffsets.IsCreated) MicroGridOffsets.Dispose();
                MicroGridOffsets = new NativeArray<int2>(totalMicroCells, Allocator.Persistent);
            }

            if (!Heatmap.IsCreated)
            {
                Heatmap = new NativeArray<HeatmapCell>(battle.GridSize.x * battle.GridSize.y, Allocator.Persistent);
            }

            var populateJob = new PopulateSpatialDataJob
            {
                MicroCellSize = battle.MicroCellSize,
                GridWidth = _microGridWidth,
                GridHeight = _microGridHeight,
                MapOffset = new float2(100f, 100f),
                SpatialData = SortedSpatialData
            }.ScheduleParallel(_aliveUnitsQuery, state.Dependency);

            var sortJob = SortedSpatialData
                .GetSubArray(0, entityCount)
                .SortJob(new SortRequestsByEntityIndex())
                .Schedule(populateJob);

            state.Dependency = new BuildOffsetsAndHeatmapJob
            {
                EntityCount = entityCount,
                SpatialData = SortedSpatialData,
                MicroGridOffsets = MicroGridOffsets,
                Heatmap = Heatmap,
                HeatCellSize = battle.HeatCellSize,
                HeatGridSize = battle.GridSize,
                HeatGridOrigin = battle.GridOrigin
            }.Schedule(sortJob);
        }

        private struct SortRequestsByEntityIndex : IComparer<Demo3SpatialData>
        {
            public int Compare(Demo3SpatialData x, Demo3SpatialData y)
            {
                return x.CellIndex.CompareTo(y.CellIndex);
            }
        }

        [BurstCompile]
        private partial struct PopulateSpatialDataJob : IJobEntity
        {
            public float MicroCellSize;
            public int GridWidth;
            public int GridHeight;
            public float2 MapOffset;
            [NativeDisableParallelForRestriction] public NativeArray<Demo3SpatialData> SpatialData;

            private void Execute([EntityIndexInQuery] int index, Entity entity, in LocalTransform transform,
                in Demo3CombatData combat)
            {
                float2 pos = transform.Position.xz + MapOffset;
                int cellX = math.clamp((int) math.floor(pos.x / MicroCellSize), 0, GridWidth - 1);
                int cellY = math.clamp((int) math.floor(pos.y / MicroCellSize), 0, GridHeight - 1);

                SpatialData[index] = new Demo3SpatialData
                {
                    CellIndex = cellY * GridWidth + cellX,
                    Entity = entity,
                    Position = transform.Position.xz,
                    Team = combat.Team
                };
            }
        }

        [BurstCompile]
        private struct BuildOffsetsAndHeatmapJob : IJob
        {
            public int EntityCount;
            [ReadOnly] public NativeArray<Demo3SpatialData> SpatialData;
            public NativeArray<int2> MicroGridOffsets;
            public NativeArray<HeatmapCell> Heatmap;

            public float HeatCellSize;
            public int2 HeatGridSize;
            public float2 HeatGridOrigin;

            public void Execute()
            {
                unsafe
                {
                    UnsafeUtility.MemClear(
                        MicroGridOffsets.GetUnsafePtr(),
                        MicroGridOffsets.Length * sizeof(int2));
                }

                unsafe
                {
                    UnsafeUtility.MemClear(
                        Heatmap.GetUnsafePtr(),
                        Heatmap.Length * sizeof(HeatmapCell));
                }

                if (EntityCount == 0) return;

                int currentCell = SpatialData[0].CellIndex;
                int currentStart = 0;
                int currentCount = 0;

                for (int i = 0; i < EntityCount; i++)
                {
                    var data = SpatialData[i];
                    if (data.CellIndex != currentCell)
                    {
                        MicroGridOffsets[currentCell] = new int2(currentStart, currentCount);
                        currentCell = data.CellIndex;
                        currentStart = i;
                        currentCount = 0;
                    }

                    currentCount++;

                    float2 localPos = data.Position - HeatGridOrigin;
                    int hX = (int) math.floor(localPos.x / HeatCellSize);
                    int hY = (int) math.floor(localPos.y / HeatCellSize);

                    if (hX >= 0 && hX < HeatGridSize.x && hY >= 0 && hY < HeatGridSize.y)
                    {
                        int hIndex = hY * HeatGridSize.x + hX;
                        var cell = Heatmap[hIndex];
                        if (data.Team == Demo3Faction.Red) cell.RedCount++;
                        else cell.BlueCount++;
                        Heatmap[hIndex] = cell;
                    }
                }

                MicroGridOffsets[currentCell] = new int2(currentStart, currentCount);
            }
        }
    }
}
