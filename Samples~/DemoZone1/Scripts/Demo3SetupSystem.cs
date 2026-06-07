using SnivelerCode.GpuAnimation.Runtime.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace SnivelerCode.GpuAnimation.DemoZone3
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial struct Demo3SetupSystem : ISystem
    {
        private EntityQuery _query;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _query = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<Demo3SpawnerTag, Demo3CombatData, AnimatorLodsBuffer>()
                .WithAll<LocalTransform>()
                .Build(ref state);

            state.RequireForUpdate(_query);
            state.RequireForUpdate<BeginInitializationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            state.Dependency = new ProcessSetupJob
            {
                CommandBuffer = ecb.AsParallelWriter()
            }.ScheduleParallel(_query, state.Dependency);
        }

        [BurstCompile]
        public partial struct ProcessSetupJob : IJobEntity
        {
            private void Execute([EntityIndexInQuery] int sortKey, Entity entity, in Demo3CombatData combat,
                DynamicBuffer<AnimatorLodsBuffer> childBuffer, in LocalTransform transform)
            {
                int sortKeyIndex = 0;
                for (int i = 0; i < childBuffer.Length; i++)
                {
                    CommandBuffer.AddComponent(sortKeyIndex++, childBuffer[i].Value, new Demo3MaterialEmissionColor
                    {
                        Value = combat.Team switch
                        {
                            Demo3Faction.Red => new float4(4f, 0.1f, 0f, 1),
                            Demo3Faction.Blue => new float4(1f, 1f, 4f, 1),
                            _ => new float4(0f, 0.1f, 0f, 1)
                        }
                    });
                }

                CommandBuffer.RemoveComponent<Demo3SpawnerTag>(sortKey, entity);
            }

            public EntityCommandBuffer.ParallelWriter CommandBuffer;
        }
    }
}
