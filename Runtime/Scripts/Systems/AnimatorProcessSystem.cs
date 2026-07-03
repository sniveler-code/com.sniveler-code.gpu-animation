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

        [BurstCompile(OptimizeFor = OptimizeFor.Performance, FloatMode = FloatMode.Fast, DisableSafetyChecks = true)]
        private partial struct AnimatorUpdateJob : IJobEntity
        {
            private void Execute(ref AnimatorOffsetData offset, ref AnimatorData anim,
                in BlobAnimatorData blob, DynamicBuffer<AnimatorParameterData> @params)
            {
                // PHASE 1: GAMEPLAY LOGIC
                ref var blobAnimator = ref blob.Value.Value;
                ref var animA = ref blobAnimator.Animations[anim.Index];
                anim.Time += DeltaTime;

                float fpsA = animA.Fps * animA.Speed;
                float durationA = animA.Frames / fpsA;

                float loopedTime = math.fmod(anim.Time, durationA);
                float clampedTime = math.min(anim.Time, durationA - 0.001f);
                anim.Time = math.select(clampedTime, loopedTime, animA.Loop);

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
                        uint triggerResetMask = 0;

                        for (int c = 0; c < transition.Conditions.Length; c++)
                        {
                            ref var condition = ref transition.Conditions[c];
                            int paramIndex = condition.Parameter;

                            if (paramIndex >= @params.Length)
                            {
                                conditionsMet = false;
                                break;
                            }

                            float pValue = @params[paramIndex].Value;
                            const float tolerance = 1e-5f;
                            byte mode = condition.Mode;

                            float diff = math.abs(pValue - condition.Threshold);

                            bool isMet =
                                (mode == 1 & pValue > 0.5f) |
                                (mode == 2 & pValue < 0.5f) |
                                (mode == 3 & pValue > condition.Threshold) |
                                (mode == 4 & pValue < condition.Threshold) |
                                (mode == 6 & diff < tolerance) |
                                (mode == 7 & diff >= tolerance);

                            if (!isMet)
                            {
                                conditionsMet = false;
                                break;
                            }

                            uint maskBit = 1u << paramIndex;
                            triggerResetMask |= math.select(0u, maskBit, mode == 1);
                        }

                        if (!conditionsMet) continue;

                        uint actualTriggers = triggerResetMask & blobAnimator.TriggerMask;
                        while (actualTriggers != 0)
                        {
                            int bitIndex = math.tzcnt(actualTriggers);
                            var paramData = @params[bitIndex];
                            paramData.Value = 0.0f;
                            @params[bitIndex] = paramData;
                            actualTriggers &= ~(1u << bitIndex);
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
                bool isEnd = anim.Frame + 1 >= animA.Frames;
                int wrappedFrame = math.select(anim.Frame, 0, animA.Loop);
                ushort nextFrameA = (ushort)math.select(anim.Frame + 1, wrappedFrame, isEnd);

                uint bCount = blobAnimator.BoneCount;
                uint offsetA0 = blob.Offset + animA.Start + anim.Frame * bCount;
                uint offsetA1 = blob.Offset + animA.Start + nextFrameA * bCount;

                offset.Value = new float4(offsetA0, offsetA1, lerpFactor, 0f);
            }

            public float DeltaTime;
        }
    }
}
