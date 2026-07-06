using System.Runtime.InteropServices;
using Unity.Entities;

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

    public struct GpuInstanceAnimState
    {
        public uint FrameA0;
        public uint FrameA1;
        public float LerpA;
        public uint Padding;
    }

    public struct AnimatorGpuIndex : ICleanupComponentData
    {
        public ushort Value;
    }

    public struct AnimatorIndexState : IComponentData
    {
        public ushort Value;
    }

    [InternalBufferCapacity(8)]
    public struct AnimatorParameterData : IBufferElementData
    {
        public float Value;
    }

    public struct AnimatorLodTag : IComponentData, IEnableableComponent
    {
    }
}
