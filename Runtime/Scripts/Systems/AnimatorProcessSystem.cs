using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using SnivelerCode.GpuAnimation.Runtime.Components;
using Unity.Jobs;

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
                .WithAllRW<AnimatorData>()
                .WithAll<BlobAnimatorData>()
                .WithAll<AnimatorParameterData, AnimatorGpuIndex>()
                .Build(ref state);

            state.RequireForUpdate<DoubleBufferData>();
            state.RequireForUpdate<AnimatorIndexState>();
            state.RequireForUpdate(_query);

            state.EntityManager.AddComponentData(state.SystemHandle, new DoubleBufferData
            {
                WriteIndex = 0,
                Capacity = 0
            });
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            var bufferData = SystemAPI.GetSingleton<DoubleBufferData>();
            bufferData.Handle0.Complete();
            bufferData.Handle1.Complete();
            if (bufferData.Array0.IsCreated) bufferData.Array0.Dispose();
            if (bufferData.Array1.IsCreated) bufferData.Array1.Dispose();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var bufferData = SystemAPI.GetSingleton<DoubleBufferData>();
            var indexState = SystemAPI.GetSingleton<AnimatorIndexState>();

            int requiredCapacity = indexState.Value;
            if (requiredCapacity == 0) return;

            if (requiredCapacity > bufferData.Capacity)
            {
                bufferData.Handle0.Complete();
                bufferData.Handle1.Complete();

                int newCapacity = math.max(requiredCapacity, 256);
                newCapacity = (int) (newCapacity * 1.5f);

                var newArray0 = new NativeArray<GpuInstanceAnimState>(newCapacity, Allocator.Persistent);
                var newArray1 = new NativeArray<GpuInstanceAnimState>(newCapacity, Allocator.Persistent);

                if (bufferData.Array0.IsCreated)
                {
                    NativeArray<GpuInstanceAnimState>.Copy(bufferData.Array0, newArray0, bufferData.Capacity);
                    bufferData.Array0.Dispose();
                }

                if (bufferData.Array1.IsCreated)
                {
                    NativeArray<GpuInstanceAnimState>.Copy(bufferData.Array1, newArray1, bufferData.Capacity);
                    bufferData.Array1.Dispose();
                }

                bufferData.Capacity = newCapacity;
                bufferData.Array0 = newArray0;
                bufferData.Array1 = newArray1;
            }

            bufferData.WriteIndex = 1 - bufferData.WriteIndex;
            var currentArray = bufferData.WriteIndex == 0 ? bufferData.Array0 : bufferData.Array1;

            var job = new AnimatorUpdateJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime,
                GpuStateArray = currentArray
            };

            var handle = job.ScheduleParallel(_query, state.Dependency);
            state.Dependency = handle;

            if (bufferData.WriteIndex == 0) bufferData.Handle0 = handle;
            else bufferData.Handle1 = handle;

            SystemAPI.SetSingleton(bufferData);
        }

        [BurstCompile(OptimizeFor = OptimizeFor.Performance, FloatMode = FloatMode.Fast, DisableSafetyChecks = true)]
        private partial struct AnimatorUpdateJob : IJobEntity
        {
            public float DeltaTime;
            [NativeDisableParallelForRestriction] public NativeArray<GpuInstanceAnimState> GpuStateArray;

            private void Execute(in AnimatorGpuIndex gpuIndex, ref AnimatorData anim, in BlobAnimatorData blob,
                DynamicBuffer<AnimatorParameterData> @params)
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
                    int paramCount = @params.Length;
                    bool hasParams = paramCount > 0;
                    for (int i = 0; i < animA.Transitions.Length; i++)
                    {
                        ref var transition = ref animA.Transitions[i];
                        bool conditionsMet = anim.Frame >= transition.Start;
                        uint triggerResetMask = 0;

                        for (int c = 0; c < transition.Conditions.Length; c++)
                        {
                            ref var condition = ref transition.Conditions[c];
                            int paramIndex = condition.Parameter;
                            bool validParam = paramIndex < paramCount;
                            int safeIndex = math.select(0, paramIndex, validParam);
                            float pValue = hasParams ? @params[safeIndex].Value : 0f;

                            byte mode = condition.Mode;
                            float diff = math.abs(pValue - condition.Threshold);
                            bool isMet = validParam & (
                                (mode == 1 & pValue > 0.5f) |
                                (mode == 2 & pValue < 0.5f) |
                                (mode == 3 & pValue > condition.Threshold) |
                                (mode == 4 & pValue < condition.Threshold) |
                                (mode == 6 & diff < 1e-5f) |
                                (mode == 7 & diff >= 1e-5f)
                            );

                            conditionsMet &= isMet;
                            uint maskBit = 1u << paramIndex;
                            triggerResetMask |= math.select(0u, maskBit, mode == 1);
                        }

                        if (conditionsMet)
                        {
                            uint actualTriggers = triggerResetMask & blobAnimator.TriggerMask;
                            while (actualTriggers != 0)
                            {
                                int bitIndex = math.tzcnt(actualTriggers);
                                @params[bitIndex] = new AnimatorParameterData {Value = 0.0f};
                                actualTriggers &= ~(1u << bitIndex);
                            }

                            anim.Index = transition.Index;
                            anim.Frame = 0;
                            anim.PrevFrame = 0;
                            anim.Time = 0f;
                            animA = ref blobAnimator.Animations[anim.Index];

                            floatFrameA = 0f;
                            break;
                        }
                    }
                }

                // PHASE 2: VISUAL LOGIC
                float lerpFactor = floatFrameA - anim.Frame;
                bool isEnd = anim.Frame + 1 >= animA.Frames;
                ushort wrappedFrame = (ushort) math.select(anim.Frame, 0, animA.Loop);
                ushort nextFrameA = (ushort) math.select(anim.Frame + 1, wrappedFrame, isEnd);

                uint bCount = blobAnimator.BoneCount;
                uint offsetA0 = blob.Offset + animA.Start + anim.Frame * bCount;
                uint offsetA1 = blob.Offset + animA.Start + nextFrameA * bCount;

                GpuStateArray[gpuIndex.Value] = new GpuInstanceAnimState
                {
                    FrameA0 = offsetA0,
                    FrameA1 = offsetA1,
                    LerpA = lerpFactor,
                    Padding = 0
                };
            }
        }

        public struct DoubleBufferData : IComponentData
        {
            public int WriteIndex;
            public int Capacity;
            public NativeArray<GpuInstanceAnimState> Array0;
            public NativeArray<GpuInstanceAnimState> Array1;
            public JobHandle Handle0;
            public JobHandle Handle1;
        }
    }
}
