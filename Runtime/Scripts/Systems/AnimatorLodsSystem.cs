using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
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

                float dot0 = math.dot(extents, math.abs(Planes.P0.xyz));
                bool isVisible = math.dot(Planes.P0.xyz, center) + Planes.P0.w >= -dot0;

                float dot1 = math.dot(extents, math.abs(Planes.P1.xyz));
                isVisible &= math.dot(Planes.P1.xyz, center) + Planes.P1.w >= -dot1;

                float dot2 = math.dot(extents, math.abs(Planes.P2.xyz));
                isVisible &= math.dot(Planes.P2.xyz, center) + Planes.P2.w >= -dot2;

                float dot3 = math.dot(extents, math.abs(Planes.P3.xyz));
                isVisible &= math.dot(Planes.P3.xyz, center) + Planes.P3.w >= -dot3;

                float dot4 = math.dot(extents, math.abs(Planes.P4.xyz));
                isVisible &= math.dot(Planes.P4.xyz, center) + Planes.P4.w >= -dot4;

                float dot5 = math.dot(extents, math.abs(Planes.P5.xyz));
                isVisible &= math.dot(Planes.P5.xyz, center) + Planes.P5.w >= -dot5;

                if (!isVisible) return;

                property.Value = RootOffsets[lod.Group].Value;
            }

            [ReadOnly] public AnimatorFrustumPlanes Planes;
            [ReadOnly] public ComponentLookup<AnimatorOffsetData> RootOffsets;
        }
    }
}
