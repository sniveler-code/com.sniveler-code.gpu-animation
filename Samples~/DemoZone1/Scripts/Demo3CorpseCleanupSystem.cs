using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace SnivelerCode.GpuAnimation.DemoZone3
{
    public partial struct Demo3CorpseCleanupSystem : ISystem
    {
        private EntityQuery _query;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _query = new EntityQueryBuilder(Allocator.Temp)
                .WithAllRW<Demo3DeadData, LocalTransform>()
                .Build(ref state);

            state.RequireForUpdate(_query);
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            state.Dependency = new CleanupJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime,
                CommandBuffer = ecb.AsParallelWriter()
            }.ScheduleParallel(_query, state.Dependency);
        }

        [BurstCompile]
        public partial struct CleanupJob : IJobEntity
        {
            public float DeltaTime;
            public EntityCommandBuffer.ParallelWriter CommandBuffer;

            private void Execute([EntityIndexInQuery] int sortKey, Entity entity,
                ref Demo3DeadData data, ref LocalTransform transform)
            {
                data.Progress += DeltaTime;
                if (data.Progress < 5.0f) return;

                float sinkProgress = (data.Progress - 5.0f) / 2f;
                transform.Position.y -= DeltaTime * 1.0f;
                if (sinkProgress >= 1.0f)
                {
                    CommandBuffer.DestroyEntity(sortKey, entity);
                }
            }
        }
    }
}
