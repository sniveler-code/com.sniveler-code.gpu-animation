using System.Runtime.InteropServices;
using Unity.Entities;
using Unity.Mathematics;

namespace SnivelerCode.GpuAnimation.Runtime.Components
{
    public struct SceneAnimatorConfigData : IComponentData
    {
        public BlobAssetReference<GpuBlobAnimationAsset> Blob;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct AnimatorData : IComponentData
    {
        public float Time;
        public ushort Frame;
        public ushort PrevFrame;
        public byte Index;
    }

    public struct AnimatorOffsetData : IComponentData
    {
        public float4 Value;
    }

    [InternalBufferCapacity(8)]
    public struct AnimatorParameterData : IBufferElementData
    {
        public float Value;
    }

    public struct AnimatorLodTag : IComponentData
    {
    }

    [TemporaryBakingType]
    public struct AnimatorBakeLodsData : IComponentData
    {
        public uint Frame;
    }
}
