using SnivelerCode.GpuAnimation.Generated;
using SnivelerCode.GpuAnimation.Runtime.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace SnivelerCode.GpuAnimation.DemoZone3
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct Demo3SpawnerSystem : ISystem
    {
        private EntityQuery _query;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _query = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<LocalTransform>()
                .WithAllRW<Demo3SpawnerData>()
                .Build(ref state);

            state.RequireForUpdate(_query);
            state.RequireForUpdate<AnimatorPrefabBuffer>();
            state.RequireForUpdate<Demo3BattleData>();
            state.RequireForUpdate<EndInitializationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var buffer = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>();
            var commandBuffer = buffer.CreateCommandBuffer(state.WorldUnmanaged);
            var prefabBuffer = SystemAPI.GetSingletonBuffer<AnimatorPrefabBuffer>();

            state.Dependency = new ProgressJob
            {
                CommandBuffer = commandBuffer.AsParallelWriter(),
                PrefabBuffer = prefabBuffer,
                RandomIndex = UnityEngine.Random.Range(0, 99999),
                DeltaTime = SystemAPI.Time.DeltaTime
            }.ScheduleParallel(_query, state.Dependency);
        }

        [BurstCompile]
        public partial struct ProgressJob : IJobEntity
        {
            private void Execute([EntityIndexInQuery] int index, in LocalTransform transform,
                ref Demo3SpawnerData data)
            {
                data.Progress += DeltaTime;
                if (data.Progress < data.SpawnTime) return;
                data.Progress = 0;

                var entities = new NativeArray<Entity>(data.Count, Allocator.Temp);
                CommandBuffer.Instantiate(index, PrefabBuffer[data.PrefabIndex].Value, entities);

                int rows = (int) math.ceil((float) data.Count / data.Columns);
                float offsetX = (data.Columns - 1) * data.Spacing * 0.5f;
                float offsetZ = (rows - 1) * data.Spacing * 0.5f;

                for (int i = 0; i < entities.Length; i++)
                {
                    var entity = entities[i];
                    int column = i % data.Columns;
                    int row = i / data.Columns;

                    var testRandom = Random.CreateFromIndex((uint) RandomIndex++);
                    float3 localPos = new float3(
                        column * data.Spacing - offsetX,
                        0,
                        row * data.Spacing - offsetZ
                    );

                    float3 worldPos = transform.Position + math.mul(transform.Rotation, localPos);
                    CommandBuffer.SetComponent(index, entity,
                        LocalTransform.FromPositionRotation(worldPos, transform.Rotation));

                    CommandBuffer.SetComponent(index, entity, new Demo3CombatData
                    {
                        Health = 100,
                        Team = data.Faction,
                        CurrentCooldown = testRandom.NextFloat(0f, 1f),
                        CurrentAttackProfileIndex = 255,
                        HasDealtDamage = false,
                        LockedHeatmapCell = new int2(-1, -1),
                        UnityType = data.PrefabIndex
                    });

                    // offset trick
                    /*CommandBuffer.SetComponent(index, entity, new AnimatorData
                    {
                        Index = data.PrefabIndex == 0
                            ? AnimatorCastleGuard01.GreatSwordRun
                            : AnimatorCastleGuard01.GreatSwordRun,
                        Time = testRandom.NextFloat(0f, 2f)
                    });*/
                }
            }

            [ReadOnly] public int RandomIndex;
            public EntityCommandBuffer.ParallelWriter CommandBuffer;
            [ReadOnly] public DynamicBuffer<AnimatorPrefabBuffer> PrefabBuffer;
            public float DeltaTime;
        }
    }
}
