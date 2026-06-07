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
        public NativeParallelMultiHashMap<uint, Demo3SpatialData> MicroMap { get; private set; }
        public NativeArray<HeatmapCell> Heatmap;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            MicroMap = new NativeParallelMultiHashMap<uint, Demo3SpatialData>(100000, Allocator.Persistent);
            Heatmap = new NativeArray<HeatmapCell>(400, Allocator.Persistent);

            _aliveUnitsQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<LocalTransform, Demo3CombatData>()
                .WithNone<Demo3DeadData, Demo3SpawnerTag>()
                .Build(ref state);

            state.RequireForUpdate(_aliveUnitsQuery);
            state.RequireForUpdate<Demo3BattleData>();
        }


        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            if (MicroMap.IsCreated) MicroMap.Dispose();
            if (Heatmap.IsCreated) Heatmap.Dispose();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            MicroMap.Clear();
            var battle = SystemAPI.GetSingleton<Demo3BattleData>();

            state.Dependency = new ClearHeatmapJob
            {
                Heatmap = Heatmap
            }.Schedule(state.Dependency);

            state.Dependency = new BuildCombatHashJob
            {
                MicroCellSize = battle.MicroCellSize,
                HeatmapCellSize = battle.HeatCellSize,
                GridSize = battle.GridSize,
                GridOrigin = battle.GridOrigin,
                MicroMap = MicroMap.AsParallelWriter(),
                Heatmap = Heatmap
            }.ScheduleParallel(_aliveUnitsQuery, state.Dependency);
        }

        [BurstCompile]
        private struct ClearHeatmapJob : IJob
        {
            public void Execute()
            {
                for (int i = 0; i < Heatmap.Length; i++)
                {
                    Heatmap[i] = default;
                }
            }

            public NativeArray<HeatmapCell> Heatmap;
        }

        [BurstCompile]
        public unsafe partial struct BuildCombatHashJob : IJobEntity
        {
            private void Execute(Entity entity, in LocalTransform transform, in Demo3CombatData combat)
            {
                int2 microCell = new int2(math.floor(transform.Position.xz / MicroCellSize));
                MicroMap.Add(math.hash(microCell), new Demo3SpatialData
                {
                    Entity = entity,
                    Position = transform.Position.xz,
                    Team = combat.Team
                });

                float2 localPos = transform.Position.xz - GridOrigin;
                int2 cell = new int2(math.floor(localPos / HeatmapCellSize));
                if (cell.x >= 0 && cell.x < GridSize.x && cell.y >= 0 && cell.y < GridSize.y)
                {
                    int index = cell.y * GridSize.x + cell.x;

                    // todo: Scatter-Gather
                    if (combat.Team == Demo3Faction.Red)
                    {
                        System.Threading.Interlocked.Increment(
                            ref ((HeatmapCell*)Heatmap.GetUnsafePtr())[index].RedCount);
                    }
                    else
                    {
                        System.Threading.Interlocked.Increment(
                            ref ((HeatmapCell*) Heatmap.GetUnsafePtr())[index].BlueCount);
                    }
                }
            }

            [ReadOnly] public float MicroCellSize;
            public NativeParallelMultiHashMap<uint, Demo3SpatialData>.ParallelWriter MicroMap;
            [NativeDisableParallelForRestriction] public NativeArray<HeatmapCell> Heatmap;
            public float2 GridOrigin;
            public float HeatmapCellSize;
            public int2 GridSize;
        }
    }
}
