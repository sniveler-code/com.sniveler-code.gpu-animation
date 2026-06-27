using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using System.Runtime.CompilerServices;
using SnivelerCode.GpuAnimation.Runtime.Components;

namespace SnivelerCode.GpuAnimation.Runtime.Systems
{
    [UpdateAfter(typeof(AnimatorProcessSystem))]
    public partial struct AnimatorLodsSystem : ISystem
    {
        private EntityQuery _query;
        private ComponentLookup<AnimatorOffsetData> _offsetLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _query = new EntityQueryBuilder(Allocator.Temp)
                .WithAllRW<SnivelerMaterialFrames>()
                .WithAll<AnimatorLodTag, MeshLODComponent, WorldRenderBounds>()
                .Build(ref state);

            _offsetLookup = state.GetComponentLookup<AnimatorOffsetData>(true);

            state.RequireForUpdate<AnimatorCameraData>();
            state.RequireForUpdate(_query);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var cameraData = SystemAPI.GetSingleton<AnimatorCameraData>();

            _offsetLookup.Update(ref state);

            state.Dependency = new ProcessJob
            {
                Planes = cameraData.Planes,
                RootOffsets = _offsetLookup
            }.ScheduleParallel(_query, state.Dependency);
        }

        [BurstCompile]
        public partial struct ProcessJob : IJobEntity
        {
            private void Execute(in MeshLODComponent lod, in WorldRenderBounds bounds,
                ref SnivelerMaterialFrames property)
            {
                float3 center = bounds.Value.Center;
                float3 extents = bounds.Value.Extents;

                bool isVisible =
                    CheckPlane(Planes.P0, center, extents) &&
                    CheckPlane(Planes.P1, center, extents) &&
                    CheckPlane(Planes.P2, center, extents) &&
                    CheckPlane(Planes.P3, center, extents) &&
                    CheckPlane(Planes.P4, center, extents) &&
                    CheckPlane(Planes.P5, center, extents);

                if (!isVisible) return;

                if (RootOffsets.TryGetComponent(lod.Group, out var rootFrames))
                {
                    property.Value = rootFrames.Value;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static bool CheckPlane(float4 plane, float3 center, float3 extents)
            {
                float dot = math.dot(extents, math.abs(plane.xyz));
                return math.dot(plane.xyz, center) + plane.w >= -dot;
            }

            [ReadOnly] public AnimatorFrustumPlanes Planes;
            [ReadOnly] public ComponentLookup<AnimatorOffsetData> RootOffsets;
        }
    }
}
