using SnivelerCode.GpuAnimation.Generated;
using SnivelerCode.GpuAnimation.Runtime.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace SnivelerCode.GpuAnimation.DemoZone3
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(Demo3CombatDecisionSystem))]
    [UpdateBefore(typeof(Demo3DamageProcessSystem))]
    public partial struct Demo3MovementSystem : ISystem
    {
        private EntityQuery _query;

        public void OnCreate(ref SystemState state)
        {
            _query = new EntityQueryBuilder(Allocator.Temp)
                .WithAllRW<LocalTransform>()
                .WithAll<AnimatorData, Demo3UnitConfig, AnimatorParameterData>()
                .WithNone<Demo3DeadData, Demo3SpawnerTag>()
                .Build(ref state);

            state.RequireForUpdate(_query);
            state.RequireForUpdate<Demo3BattleData>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Dependency = new MovementJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime
            }.ScheduleParallel(_query, state.Dependency);
        }

        [BurstCompile]
        public partial struct MovementJob : IJobEntity
        {
            public float DeltaTime;

            private void Execute(Entity entity, ref LocalTransform transform, in AnimatorData data,
                in DynamicBuffer<AnimatorParameterData> @params)
            {
                if (data.Index != AnimatorGuardWithSword.GreatSwordRun &&
                    data.Index != AnimatorGuardWithSword.GreatSwordWalk)
                {
                    return;
                }

                var r1 = Random.CreateFromIndex((uint) entity.Index);
                float3 forwardDirection = math.forward(transform.Rotation);
                transform.Position +=
                    forwardDirection *
                    @params[AnimatorParams.GuardWithSword.Speed].Value *
                    (4f + r1.NextFloat(0.1f, 0.4f)) *
                    DeltaTime;
            }
        }
    }
}
