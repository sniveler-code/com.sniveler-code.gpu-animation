using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using SnivelerCode.GpuAnimation.Runtime.Components;

namespace SnivelerCode.GpuAnimation.Runtime.Systems
{
    [UpdateBefore(typeof(TransformSystemGroup))]
    public partial struct AnimatorProcessSystem : ISystem
    {
        private EntityQuery _query;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _query = new EntityQueryBuilder(Allocator.Temp)
                .WithAllRW<AnimatorData, AnimatorOffsetData>()
                .WithAll<BlobAnimatorData>()
                .WithAll<AnimatorParameterData>()
                .Build(ref state);

            state.RequireForUpdate<AnimatorCameraData>();
            state.RequireForUpdate<SceneAnimatorConfigData>();
            state.RequireForUpdate(_query);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Dependency = new AnimatorUpdateJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime
            }.ScheduleParallel(_query, state.Dependency);
        }

        [BurstCompile]
        private partial struct AnimatorUpdateJob : IJobEntity
        {
            private void Execute(in Entity entity, ref AnimatorOffsetData offset, ref AnimatorData anim,
                in BlobAnimatorData blob, DynamicBuffer<AnimatorParameterData> @params)
            {
                // PHASE 1: GAMEPLAY LOGIC
                ref var blobAnimator = ref blob.Value.Value;
                ref var animA = ref blobAnimator.Animations[anim.Index];
                anim.Time += DeltaTime;

                float fpsA = animA.Fps * animA.Speed;
                float durationA = animA.Frames / fpsA;

                anim.Time = animA.Loop
                    ? math.fmod(anim.Time, durationA)
                    : math.min(anim.Time, durationA - 0.001f);

                float floatFrameA = anim.Time * fpsA;
                anim.PrevFrame = anim.Frame;
                anim.Frame = (ushort) floatFrameA;

                if (animA.Transitions.Length > 0)
                {
                    ref var transitions = ref animA.Transitions;
                    for (int i = 0; i < transitions.Length; i++)
                    {
                        ref var transition = ref transitions[i];
                        if (anim.Frame < transition.Start) continue;

                        bool conditionsMet = true;
                        for (int c = 0; c < transition.Conditions.Length; c++)
                        {
                            ref var condition = ref transition.Conditions[c];
                            if (condition.Parameter >= @params.Length)
                            {
                                conditionsMet = false;
                                break;
                            }

                            float pValue = @params[condition.Parameter].Value;
                            const float tolerance = 1e-5f;
                            conditionsMet = condition.Mode switch
                            {
                                1 => pValue > 0.5f,
                                2 => pValue < 0.5f,
                                3 => pValue > condition.Threshold,
                                4 => pValue < condition.Threshold,
                                6 => math.abs(pValue - condition.Threshold) < tolerance,
                                7 => math.abs(pValue - condition.Threshold) >= tolerance,
                                _ => false
                            };

                            if (!conditionsMet) break;
                        }

                        if (!conditionsMet) continue;

                        for (int c = 0; c < transition.Conditions.Length; c++)
                        {
                            ref var condition = ref transition.Conditions[c];
                            if (condition.Mode != 1) continue;

                            bool isTrigger = (blobAnimator.TriggerMask & (1u << condition.Parameter)) != 0;
                            if (!isTrigger) continue;

                            var paramData = @params[condition.Parameter];
                            paramData.Value = 0.0f;
                            @params[condition.Parameter] = paramData;
                        }

                        anim.Index = transition.Index;
                        anim.Frame = 0;
                        anim.PrevFrame = 0;
                        animA = ref blobAnimator.Animations[anim.Index];
                        anim.Time = 0f;
                        return;
                    }
                }

                // PHASE 2: VISUAL LOGIC
                float lerpFactor = floatFrameA - anim.Frame;
                ushort nextFrameA = (ushort)
                    (anim.Frame + 1 >= animA.Frames ? animA.Loop ? 0 : anim.Frame : anim.Frame + 1);

                uint bCount = blobAnimator.BoneCount;
                uint offsetA0 = blob.Offset + animA.Start + anim.Frame * bCount;
                uint offsetA1 = blob.Offset + animA.Start + nextFrameA * bCount;

                offset.Value = new float4(offsetA0, offsetA1, lerpFactor, 0f);
            }

            public float DeltaTime;
        }
    }
}
