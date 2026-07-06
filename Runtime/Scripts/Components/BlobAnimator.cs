using Unity.Entities;
using Unity.Mathematics;

namespace SnivelerCode.GpuAnimation.Runtime.Components
{
    public struct BlobAnimatorData : IComponentData
    {
        public BlobAssetReference<BlobAnimatorAsset> Value;
        public uint Offset;
    }

    public struct BlobAnimatorAsset
    {
        public ulong MatricesHash;
        public byte BoneCount;
        public BlobArray<BlobAnimationAsset> Animations;
        public uint TriggerMask;
    }

    public struct BlobAnimationAsset
    {
        public byte Fps;
        public uint Start;
        public ushort Frames;
        public float Speed;
        public bool Loop;
        public BlobArray<BlobTransitionAsset> Transitions;
    }

    public struct BlobTransitionAsset
    {
        public byte Index;
        public ushort Start;
        public BlobArray<BlobConditionAsset> Conditions;
    }

    public struct BlobConditionAsset
    {
        public byte Parameter;
        public byte Mode;
        public float Threshold;
    }

    public struct GpuBlobAnimationAsset
    {
        public BlobArray<float3x4> MatricesLbs;
        public BlobArray<uint> Offsets;
        public BlobArray<ulong> Hashes;
    }
}
