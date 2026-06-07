using System.Runtime.InteropServices;
using Unity.Entities;

namespace SnivelerCode.GpuAnimation.Runtime.Components
{
    public struct SceneAnimatorConfigData : IComponentData
    {
        public BlobAssetReference<GpuBlobAnimationAsset> Blob;
    }

    [InternalBufferCapacity(16)]
    public struct AnimatorPrefabBuffer : IBufferElementData
    {
        public Entity Value;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct AnimatorData : IComponentData
    {
        public float Time;
        public ushort Frame;
        public ushort PrevFrame;
        public byte Index;
    }

    [InternalBufferCapacity(8)]
    public struct AnimatorParameterData : IBufferElementData
    {
        public float Value;
        public bool IsTrigger;
    }

    [InternalBufferCapacity(8)]
    public struct AnimatorLodsBuffer : IBufferElementData
    {
        public Entity Value;
    }

    [TemporaryBakingType]
    [InternalBufferCapacity(8)]
    public struct AnimatorBakeLodsBuffer : IBufferElementData
    {
        public Entity Value;
        public uint Frame;
    }

    [TemporaryBakingType]
    public struct AnimatorBakeLodsData : IComponentData
    {
        public uint Frame;
    }
}
