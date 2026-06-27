using System;
using System.Collections.Generic;

namespace SnivelerCode.GpuAnimation.Runtime.Components
{
    [Serializable]
    public struct MonoBlobAnimator
    {
        public string Name;
        public byte FPS;
        public uint Start;
        public ushort Frames;
        public float Speed;
        public bool Loop;
        public List<MonoBlobTransition> Transitions;

        public void ToBlobAsset(ref BlobAnimationAsset blobAnimation)
        {
            blobAnimation.Fps = FPS;
            blobAnimation.Frames = Frames;
            blobAnimation.Loop = Loop;
            blobAnimation.Speed = Speed;
            blobAnimation.Start = Start;
        }
    }

    [Serializable]
    public struct MonoBlobAnimatorParameter
    {
        public string Name;
        public float Value;
        public bool IsTrigger;
    }

    [Serializable]
    public struct MonoBlobTransition
    {
        public byte Index;
        public ushort Start;
        public List<MonoBlobCondition> Conditions;

        public void ToBlobAsset(ref BlobTransitionAsset blob)
        {
            blob.Index = Index;
            blob.Start = Start;
        }
    }

    [Serializable]
    public struct MonoBlobCondition
    {
        public int Parameter;
        public byte Mode;
        public float Threshold;

        public void ToBlobAsset(ref BlobConditionAsset blob)
        {
            blob.Mode = Mode;
            blob.Parameter = (byte) Parameter;
            blob.Threshold = Threshold;
        }
    }
}
