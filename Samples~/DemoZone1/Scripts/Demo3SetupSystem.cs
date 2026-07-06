using SnivelerCode.GpuAnimation.Runtime.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

namespace SnivelerCode.GpuAnimation.DemoZone3
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial struct Demo3SetupSystem : ISystem
    {
        private EntityQuery _query;
        private ComponentLookup<Demo3CombatData> _configLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _query = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<MeshLODComponent, AnimatorLodTag, Demo3SpawnerTag>()
                .Build(ref state);

            _configLookup = state.GetComponentLookup<Demo3CombatData>(true);

            state.RequireForUpdate(_query);
            state.RequireForUpdate<BeginInitializationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if(_query.IsEmpty) return;
            var ecbSingleton = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            _configLookup.Update(ref state);

            state.Dependency = new ProcessSetupJob
            {
                CommandBuffer = ecb.AsParallelWriter(),
                Configs = _configLookup
            }.ScheduleParallel(_query, state.Dependency);
        }

        [BurstCompile]
        public partial struct ProcessSetupJob : IJobEntity
        {
            private void Execute([EntityIndexInQuery] int index, Entity entity, in MeshLODComponent lod)
            {
                if (!Configs.TryGetComponent(lod.Group, out var config)) return;
                CommandBuffer.SetComponentEnabled<Demo3SpawnerTag>(index, entity, false);
                CommandBuffer.AddComponent(index, entity, new Demo3MaterialEmissionColor
                {
                    Value = config.Team switch
                    {
                        Demo3Faction.Red => new float4(4f, 0.1f, 0f, 1),
                        Demo3Faction.Blue => new float4(1f, 1f, 4f, 1),
                        _ => new float4(0f, 0.1f, 0f, 1)
                    }
                });
            }

            public EntityCommandBuffer.ParallelWriter CommandBuffer;
            [ReadOnly] public ComponentLookup<Demo3CombatData> Configs;
        }
    }
}
