using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using System.Runtime.CompilerServices;
using SnivelerCode.GpuAnimation.Runtime.Components;

namespace SnivelerCode.GpuAnimation.Runtime.Systems
{
    [UpdateBefore(typeof(TransformSystemGroup))]
    public partial struct AnimatorProcessSystem : ISystem
    {
        private EntityQuery _query;
        private ComponentLookup<SnivelerMaterialFrames> _framesLookup;
        private ComponentLookup<WorldRenderBounds> _boundsLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _query = new EntityQueryBuilder(Allocator.Temp)
                .WithAllRW<AnimatorData>()
                .WithAll<BlobAnimatorData, AnimatorLodsBuffer>()
                .WithAll<AnimatorParameterData>()
                .Build(ref state);

            _framesLookup = state.GetComponentLookup<SnivelerMaterialFrames>();
            _boundsLookup = state.GetComponentLookup<WorldRenderBounds>(true);

            state.RequireForUpdate<AnimatorCameraData>();
            state.RequireForUpdate<SceneAnimatorConfigData>();
            state.RequireForUpdate(_query);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var cameraData = SystemAPI.GetSingleton<AnimatorCameraData>();

            _framesLookup.Update(ref state);
            _boundsLookup.Update(ref state);

            state.Dependency = new AnimatorUpdateJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime,
                FramesLookup = _framesLookup,
                BoundsLookup = _boundsLookup,
                Planes = cameraData.Planes
            }.ScheduleParallel(_query, state.Dependency);
        }
    }

    [BurstCompile]
    public partial struct AnimatorUpdateJob : IJobEntity
    {
        private void Execute(in Entity entity, ref AnimatorData anim, in BlobAnimatorData blob,
            DynamicBuffer<AnimatorLodsBuffer> lods, DynamicBuffer<AnimatorParameterData> @params)
        {
            // --- 0. CULLING CHECK
            bool isVisible = true;
            if (lods.Length > 0 && BoundsLookup.HasComponent(lods[0].Value))
            {
                AABB aabb = BoundsLookup[lods[0].Value].Value;
                float3 center = aabb.Center;
                float3 extents = aabb.Extents;

                isVisible =
                    CheckPlane(Planes.P0, center, extents) &&
                    CheckPlane(Planes.P1, center, extents) &&
                    CheckPlane(Planes.P2, center, extents) &&
                    CheckPlane(Planes.P3, center, extents) &&
                    CheckPlane(Planes.P4, center, extents) &&
                    CheckPlane(Planes.P5, center, extents);
            }

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

                        var paramData = @params[condition.Parameter];
                        if (!paramData.IsTrigger) continue;

                        paramData.Value = 0.0f;
                        @params[condition.Parameter] = paramData;
                    }

                    var random = Random.CreateFromIndex((uint)entity.Index);

                    anim.Index = transition.Index;
                    anim.Frame = 0;
                    anim.PrevFrame = 0;
                    animA = ref blobAnimator.Animations[anim.Index];
                    anim.Time = animA.Loop ? random.NextFloat(0f, animA.Frames / fpsA) : 0f;
                    return;
                }
            }

            // PHASE 2: CULLING
            if (!isVisible) return;

            // PHASE 3: VISUAL LOGIC
            float lerpFactor = floatFrameA - anim.Frame;
            ushort nextFrameA = (ushort)
                (anim.Frame + 1 >= animA.Frames ? animA.Loop ? 0 : anim.Frame : anim.Frame + 1);

            uint bCount = blobAnimator.BoneCount;
            uint offsetA0 = blob.Offset + animA.Start + anim.Frame * bCount;
            uint offsetA1 = blob.Offset + animA.Start + nextFrameA * bCount;

            float4 framesA = new float4(offsetA0, offsetA1, lerpFactor, 0f);
            for (int i = 0; i < lods.Length; i++)
            {
                Entity child = lods[i].Value;
                if (!FramesLookup.HasComponent(child)) continue;
                FramesLookup[child] = new SnivelerMaterialFrames {Value = framesA};
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool CheckPlane(float4 plane, float3 center, float3 extents)
        {
            float dot = math.dot(extents, math.abs(plane.xyz));
            return math.dot(plane.xyz, center) + plane.w >= -dot;
        }

        public float DeltaTime;
        [NativeDisableParallelForRestriction] public ComponentLookup<SnivelerMaterialFrames> FramesLookup;
        [ReadOnly] public ComponentLookup<WorldRenderBounds> BoundsLookup;
        [ReadOnly] public AnimatorFrustumPlanes Planes;
    }
}
