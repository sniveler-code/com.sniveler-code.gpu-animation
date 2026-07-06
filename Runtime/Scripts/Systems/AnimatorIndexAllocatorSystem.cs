using SnivelerCode.GpuAnimation.Runtime.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace SnivelerCode.GpuAnimation.Runtime.Systems
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [BurstCompile]
    public partial struct AnimatorIndexAllocatorSystem : ISystem
    {
        private NativeQueue<ushort> _freeIndices;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _freeIndices = new NativeQueue<ushort>(Allocator.Persistent);
            state.EntityManager.AddComponentData(state.SystemHandle,
                new AnimatorIndexState {Value = 0});

            state.RequireForUpdate<EndInitializationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            _freeIndices.Dispose();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            ref var indexState = ref SystemAPI.GetSingletonRW<AnimatorIndexState>().ValueRW;

            foreach ((RefRO<AnimatorGpuIndex> gpuIndex, Entity entity) in
                     SystemAPI.Query<RefRO<AnimatorGpuIndex>>().WithNone<AnimatorData>().WithEntityAccess())
            {
                _freeIndices.Enqueue(gpuIndex.ValueRO.Value);
                ecb.RemoveComponent<AnimatorGpuIndex>(entity);
            }

            foreach ((RefRO<AnimatorData> animData, Entity entity) in
                     SystemAPI.Query<RefRO<AnimatorData>>()
                         .WithNone<AnimatorGpuIndex>().WithEntityAccess())
            {
                ushort index = _freeIndices.IsEmpty() ? indexState.Value++ : _freeIndices.Dequeue();
                ecb.AddComponent(entity, new AnimatorGpuIndex {Value = index});
            }
        }
    }
}
