using SnivelerCode.GpuAnimation.Runtime.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;

namespace SnivelerCode.GpuAnimation.Runtime.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(AnimatorProcessSystem))]
    [BurstCompile]
    public partial struct AnimatorLodSyncSystem : ISystem
    {
        private EntityQuery _query;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<SnivelerInstanceID>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.Dependency = new SyncIdJob
            {
                RootIndices = SystemAPI.GetComponentLookup<AnimatorGpuIndex>(true),
                Ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter()
            }.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        [WithDisabled(typeof(AnimatorLodTag))]
        private partial struct SyncIdJob : IJobEntity
        {
            [ReadOnly] public ComponentLookup<AnimatorGpuIndex> RootIndices;
            public EntityCommandBuffer.ParallelWriter Ecb;

            private void Execute(Entity entity, [ChunkIndexInQuery] int chunkIndex,
                ref SnivelerInstanceID id, in MeshLODComponent lod)
            {
                if (!RootIndices.TryGetComponent(lod.Group, out var rootIndex)) return;
                id.Value = rootIndex.Value;
                Ecb.SetComponentEnabled<AnimatorLodTag>(chunkIndex, entity, true);
            }
        }
    }
}
