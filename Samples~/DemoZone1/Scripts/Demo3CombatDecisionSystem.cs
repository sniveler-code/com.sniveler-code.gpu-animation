using SnivelerCode.GpuAnimation.Runtime.Components;
using SnivelerCode.GpuAnimation.Runtime.Systems;
using SnivelerCode.GpuAnimation.Runtime.Utils;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace SnivelerCode.GpuAnimation.DemoZone3
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(AnimatorProcessSystem))]
    public partial struct Demo3CombatDecisionSystem : ISystem
    {
        private EntityQuery _aliveUnitsQuery;
        public NativeParallelMultiHashMap<Entity, Demo3DamageMessage> DamageMailbox;
        public JobHandle MailboxWriterDependency;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _aliveUnitsQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAllRW<LocalTransform, AnimatorData>()
                .WithAllRW<Demo3CombatData>()
                .WithAll<Demo3UnitConfig, AnimatorParameterData>()
                .WithNone<Demo3DeadData, Demo3SpawnerTag>()
                .Build(ref state);

            DamageMailbox = new NativeParallelMultiHashMap<Entity, Demo3DamageMessage>
                (20000, Allocator.Persistent);

            state.RequireForUpdate(_aliveUnitsQuery);
            state.RequireForUpdate<Demo3BattleData>();
            state.RequireForUpdate<BeginInitializationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            if (DamageMailbox.IsCreated) DamageMailbox.Dispose();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var hashSystem = state.WorldUnmanaged.GetExistingUnmanagedSystem<Demo3HashSystem>();
            var hashSystemRef = state.WorldUnmanaged.GetUnsafeSystemRef<Demo3HashSystem>(hashSystem);
            var battleData = SystemAPI.GetSingleton<Demo3BattleData>();
            var ecbSingleton = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            state.Dependency = new ClearMailboxJob
            {
                Mailbox = DamageMailbox
            }.Schedule(state.Dependency);

            state.Dependency = new CombatDecisionJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime,
                MicroMap = hashSystemRef.MicroMap.AsReadOnly(),
                DamageWriter = DamageMailbox.AsParallelWriter(),
                BattleConfig = battleData,
                Heatmap = hashSystemRef.Heatmap.AsReadOnly()
            }.ScheduleParallel(_aliveUnitsQuery, state.Dependency);

            MailboxWriterDependency = state.Dependency;
        }

        [BurstCompile]
        private struct ClearMailboxJob : IJob
        {
            public NativeParallelMultiHashMap<Entity, Demo3DamageMessage> Mailbox;
            public void Execute() => Mailbox.Clear();
        }

        [BurstCompile]
        private partial struct CombatDecisionJob : IJobEntity
        {
            private void Execute(Entity myEntity, ref LocalTransform transform,
                in Demo3UnitConfig config, ref Demo3CombatData combat, ref AnimatorData animData,
                ref DynamicBuffer<AnimatorParameterData> animParams)
            {
                float2 myPosXz = transform.Position.xz;

                float2 separationForce = float2.zero;
                int alliesCount = 0;
                float trafficJamFactor = 0f;

                Entity closestEnemy = Entity.Null;
                float closestEnemyDistSq = float.MaxValue;
                float2 closestEnemyPosXz = float2.zero;

                bool hasLockedTarget = combat.CurrentTarget != Entity.Null;
                bool lockedTargetFound = false;

                if (hasLockedTarget && combat.CurrentTarget == myEntity)
                {
                    int lockedIndex = combat.LockedHeatmapCell.y * BattleConfig.GridSize.x + combat.LockedHeatmapCell.x;
                    HeatmapCell lockedHeat = Heatmap[lockedIndex];
                    int lockedEnemyCount = combat.Team == Demo3Faction.Red ? lockedHeat.BlueCount : lockedHeat.RedCount;

                    if (lockedEnemyCount > 0)
                    {
                        lockedTargetFound = true;
                        closestEnemy = myEntity;
                        closestEnemyPosXz = BattleConfig.GridOrigin + new float2(
                            combat.LockedHeatmapCell.x * BattleConfig.HeatCellSize + BattleConfig.HeatCellSize * 0.5f,
                            combat.LockedHeatmapCell.y * BattleConfig.HeatCellSize + BattleConfig.HeatCellSize * 0.5f);

                        closestEnemyDistSq = math.distancesq(myPosXz, closestEnemyPosXz);
                    }
                    else
                    {
                        combat.CurrentTarget = Entity.Null;
                        combat.LockedHeatmapCell = new int2(-1, -1);
                        hasLockedTarget = false;
                    }
                }

                float2 myForwardXz = combat.Team == Demo3Faction.Red ? new float2(1, 0) : new float2(-1, 0);
                if (hasLockedTarget) myForwardXz = math.normalizesafe(closestEnemyPosXz - myPosXz);

                int2 microCell = new int2(math.floor(myPosXz / BattleConfig.MicroCellSize));
                var rnd = Random.CreateFromIndex((uint) myEntity.Index + (uint) (animData.Time * 1000));
                int enemiesFoundCount = 0;
                for (int x = -1; x <= 1; x++)
                {
                    for (int y = -1; y <= 1; y++)
                    {
                        uint hash = math.hash(microCell + new int2(x, y));
                        if (!MicroMap.TryGetFirstValue(hash, out var otherData, out var iterator)) continue;

                        do
                        {
                            if (myEntity == otherData.Entity) continue;

                            float2 diff = myPosXz - otherData.Position;
                            float distSq = math.lengthsq(diff);

                            if (otherData.Team == combat.Team)
                            {
                                float minDistance = config.Radius * 2.0f;
                                if (!(distSq > 0.0001f) || !(distSq < minDistance * minDistance)) continue;
                                float dist = math.sqrt(distSq);
                                float2 dirFromAlly = diff / dist;
                                float force = (minDistance - dist) / minDistance;
                                separationForce += dirFromAlly * force;
                                alliesCount++;

                                float2 dirToAlly = -dirFromAlly;
                                float dotForward = math.dot(myForwardXz, dirToAlly);
                                if (dotForward > 0.3f)
                                {
                                    trafficJamFactor = math.max(trafficJamFactor, force);
                                }
                            }
                            else
                            {
                                CheckEnemy(otherData, distSq, hasLockedTarget, combat.CurrentTarget,
                                    ref closestEnemy, ref closestEnemyPosXz, ref closestEnemyDistSq,
                                    ref lockedTargetFound, ref rnd, ref enemiesFoundCount);
                            }
                        } while (MicroMap.TryGetNextValue(out otherData, ref iterator));
                    }
                }

                float dropDistanceSq = 25.0f;
                if (hasLockedTarget && (!lockedTargetFound || closestEnemyDistSq > dropDistanceSq))
                {
                    combat.CurrentTarget = Entity.Null;
                    combat.CurrentAttackProfileIndex = 255;
                    if (lockedTargetFound)
                    {
                        closestEnemy = Entity.Null;
                    }
                }

                float2 desiredDirXz;
                bool isAttacking = false;

                if (combat.CurrentCooldown > 0) combat.CurrentCooldown -= DeltaTime;

                if (closestEnemy != Entity.Null)
                {
                    float2 dirToEnemy = closestEnemyPosXz - myPosXz;
                    if (combat.CurrentAttackProfileIndex != 255)
                    {
                        desiredDirXz = math.normalizesafe(dirToEnemy);
                        isAttacking = true;

                        if (!combat.HasDealtDamage &&
                            animData.Index == config.Attacks.AnimationIndex &&
                            animData.Frame >= config.Attacks.DamageFrame)
                        {
                            DamageWriter.Add(closestEnemy, new Demo3DamageMessage
                            {
                                Amount = config.Attacks.Damage
                            });

                            combat.HasDealtDamage = true;
                        }

                        if (combat.CurrentCooldown <= 0)
                        {
                            combat.CurrentAttackProfileIndex = 255;
                        }
                    }
                    else
                    {
                        int selectedAttackIndex = -1;
                        if (combat.CurrentCooldown <= 0)
                        {

                            if (closestEnemyDistSq <= config.Attacks.RangeSq)
                            {
                                selectedAttackIndex = 0;
                            }
                        }

                        desiredDirXz = math.normalizesafe(dirToEnemy);
                        if (selectedAttackIndex >= 0)
                        {
                            isAttacking = true;
                            animData.Play(config.Attacks.AnimationIndex);
                            config.ParamSpeedIndex.Value(0f).Apply(animParams);

                            combat.CurrentCooldown = config.Attacks.Cooldown + rnd.NextFloat(0.1f, 0.4f);
                            combat.CurrentAttackProfileIndex = (byte) selectedAttackIndex;
                            combat.HasDealtDamage = false;

                            combat.CurrentTarget = closestEnemy;
                        }
                        else
                        {
                            desiredDirXz = math.normalizesafe(dirToEnemy);
                            bool inRangeOfAnyAttack = closestEnemyDistSq <= config.Attacks.RangeSq;
                            if (inRangeOfAnyAttack)
                            {
                                isAttacking = true;
                                combat.CurrentTarget = closestEnemy;
                            }
                        }
                    }
                }
                else
                {
                    desiredDirXz = combat.Team == Demo3Faction.Red ? new float2(1, 0) : new float2(-1, 0);
                    combat.CurrentAttackProfileIndex = 255;
                    combat.CurrentTarget = Entity.Null;
                }

                float2 finalDirXZ = desiredDirXz;
                if (alliesCount > 0)
                {
                    finalDirXZ = math.normalizesafe(desiredDirXz + separationForce * 0.2f);
                    transform.Position.xz += separationForce * DeltaTime * 1.5f;
                }

                float3 lookDirection3D;
                if (isAttacking && closestEnemy != Entity.Null)
                {
                    float2 dirToEnemy = closestEnemyPosXz - myPosXz;
                    lookDirection3D = new float3(dirToEnemy.x, 0, dirToEnemy.y);
                    if (math.lengthsq(lookDirection3D) > 0.01f)
                    {
                        quaternion targetRot = quaternion.LookRotationSafe(math.normalize(lookDirection3D), math.up());
                        transform.Rotation = math.slerp(transform.Rotation, targetRot, DeltaTime * 25.0f);
                    }
                }
                else
                {
                    lookDirection3D = new float3(finalDirXZ.x, 0, finalDirXZ.y);
                    if (math.lengthsq(lookDirection3D) > 0.01f)
                    {
                        quaternion targetRot = quaternion.LookRotationSafe(lookDirection3D, math.up());
                        transform.Rotation = math.slerp(transform.Rotation, targetRot, DeltaTime * 4.0f);
                    }
                }

                float forwardProgress = isAttacking ? 0f : math.dot(desiredDirXz, finalDirXZ);
                float targetAnimSpeed = math.saturate(forwardProgress);
                targetAnimSpeed = math.max(0f, targetAnimSpeed - trafficJamFactor * 1.5f);

                config.ParamSpeedIndex
                    .Value(math.lerp(animParams[config.ParamSpeedIndex].Value, targetAnimSpeed, DeltaTime * 10f))
                    .Apply(animParams);
            }

            private static void CheckEnemy(Demo3SpatialData otherData, float distSq, bool hasLockedTarget,
                Entity currentTarget, ref Entity closestEnemy, ref float2 closestEnemyPosXZ,
                ref float closestEnemyDistSq, ref bool lockedTargetFound, ref Random rnd, ref int enemiesFoundCount)
            {
                if (hasLockedTarget && otherData.Entity == currentTarget)
                {
                    closestEnemy = otherData.Entity;
                    closestEnemyPosXZ = otherData.Position;
                    closestEnemyDistSq = distSq;
                    lockedTargetFound = true;
                    return;
                }

                if (!lockedTargetFound)
                {
                    enemiesFoundCount++;
                    bool shouldReplace = false;
                    if (distSq < 9.0f)
                    {
                        if (distSq < closestEnemyDistSq) shouldReplace = true;
                    }
                    else
                    {
                        if (rnd.NextFloat() < 1.0f / enemiesFoundCount) shouldReplace = true;
                    }

                    if (shouldReplace)
                    {
                        closestEnemyDistSq = distSq;
                        closestEnemy = otherData.Entity;
                        closestEnemyPosXZ = otherData.Position;
                    }
                }
            }

            public float DeltaTime;
            [ReadOnly] public NativeParallelMultiHashMap<uint, Demo3SpatialData>.ReadOnly MicroMap;
            public NativeParallelMultiHashMap<Entity, Demo3DamageMessage>.ParallelWriter DamageWriter;
            [ReadOnly] public Demo3BattleData BattleConfig;
            public NativeArray<HeatmapCell>.ReadOnly Heatmap;
        }
    }
}
