using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

namespace SnivelerCode.GpuAnimation.Runtime.Components
{
    [MaterialProperty("_SnivelerInstanceID")]
    public struct SnivelerInstanceID : IComponentData
    {
        public float Value;
    }

    [MaterialProperty("_BaseColor")]
    public struct SnivelerMaterialBaseColor: IComponentData
    {
        public float4 Value;
    }
}
