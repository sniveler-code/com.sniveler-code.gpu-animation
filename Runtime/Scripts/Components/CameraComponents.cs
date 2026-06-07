using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace SnivelerCode.GpuAnimation.Runtime.Components
{
    public struct AnimatorCameraData : IComponentData
    {
        public float3 Position;
        public float3 Forward;
        public AnimatorFrustumPlanes Planes;
    }

    public struct AnimatorFrustumPlanes
    {
        public AnimatorFrustumPlanes(Plane[] planes)
        {
            P0 = new float4(planes[0].normal, planes[0].distance);
            P1 = new float4(planes[1].normal, planes[1].distance);
            P2 = new float4(planes[2].normal, planes[2].distance);
            P3 = new float4(planes[3].normal, planes[3].distance);
            P4 = new float4(planes[4].normal, planes[4].distance);
            P5 = new float4(planes[5].normal, planes[5].distance);
        }

        public readonly float4 P0, P1, P2, P3, P4, P5;
    }
}
