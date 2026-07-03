using SnivelerCode.GpuAnimation.Runtime.Components;
using SnivelerCode.GpuAnimation.Runtime.Systems;
using SnivelerCode.GpuAnimation.Runtime.Utils;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace SnivelerCode.GpuAnimation.DemoZone3
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(AnimatorProcessSystem))]
    public partial struct Demo3DamageProcessSystem : ISystem
    {
        private EntityQuery _query;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _query = new EntityQueryBuilder(Allocator.Temp)
                .WithAllRW<Demo3CombatData, AnimatorData>()
                .WithAll<Demo3UnitConfig>()
                .WithNone<Demo3DeadData>()
                .Build(ref state);

            state.RequireForUpdate(_query);
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            var decisionSystem = state.WorldUnmanaged.GetExistingUnmanagedSystem<Demo3CombatDecisionSystem>();
            ref var combatSys = ref state.WorldUnmanaged.GetUnsafeSystemRef<Demo3CombatDecisionSystem>(decisionSystem);

            var mailbox = combatSys.DamageMailbox;
            var mailboxDependency = combatSys.MailboxWriterDependency;

            var combinedDependency = Unity.Jobs.JobHandle
                .CombineDependencies(state.Dependency, mailboxDependency);

            state.Dependency = new ProcessDamageJob
            {
                CommandBuffer = ecb.AsParallelWriter(),
                DamageMailbox = mailbox.AsReadOnly()
            }.ScheduleParallel(_query, combinedDependency);
        }

        [BurstCompile]
        [WithNone(typeof(Demo3DeadData))]
        public partial struct ProcessDamageJob : IJobEntity
        {
            private void Execute([EntityIndexInQuery] int sortKey, Entity entity,
                in Demo3UnitConfig config, ref Demo3CombatData combat, ref AnimatorData animator)
            {
                if (!DamageMailbox.TryGetFirstValue(entity, out Demo3DamageMessage msg, out var iterator)) return;

                float totalDamage = 0f;
                do
                {
                    totalDamage += msg.Amount;
                } while (DamageMailbox.TryGetNextValue(out msg, ref iterator));

                combat.Health -= totalDamage;
                ref readonly Demo3UnitConfigBlob staticData = ref config.Value.Value;

                if (combat.Health <= 0)
                {
                    CommandBuffer.SetComponentEnabled<Demo3DeadData>(sortKey, entity, true);
                    animator.Play(staticData.AnimationDeathIndex);
                }
                else
                {
                    if (animator.Index == 1)
                    {
                        animator.Play(staticData.AnimationHitIndex);
                    }
                }
            }

            public EntityCommandBuffer.ParallelWriter CommandBuffer;
            [ReadOnly] public NativeParallelMultiHashMap<Entity, Demo3DamageMessage>.ReadOnly DamageMailbox;
        }
    }
}
