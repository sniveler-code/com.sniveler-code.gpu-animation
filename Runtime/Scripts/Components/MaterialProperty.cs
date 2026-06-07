using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

namespace SnivelerCode.GpuAnimation.Runtime.Components
{
    [MaterialProperty("_SnivelerRenderFrames")]
    public struct SnivelerMaterialFrames : IComponentData
    {
        public float4 Value;
    }

    [MaterialProperty("_SnivelerRenderFramesTarget")]
    public struct SnivelerMaterialFramesTarget : IComponentData
    {
        public float4 Value;
    }

    [MaterialProperty("_BaseColor")]
    public struct SnivelerMaterialBaseColor: IComponentData
    {
        public float4 Value;
    }
}
