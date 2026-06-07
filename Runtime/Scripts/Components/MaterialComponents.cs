using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;

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
        public List<RigidTransform> RootMotionFrames;

        public void ToBlobAsset(BlobBuilder builder, ref BlobAnimationAsset blobAnimation)
        {
            blobAnimation.Fps = FPS;
            blobAnimation.Frames = Frames;
            blobAnimation.Loop = Loop;
            blobAnimation.Speed = Speed;
            blobAnimation.Start = Start;

            if (RootMotionFrames is {Count: > 0})
            {
                var rootMotionArray = builder.Allocate(ref blobAnimation.RootMotionFrames, RootMotionFrames.Count);
                for (int i = 0; i < RootMotionFrames.Count; i++)
                {
                    rootMotionArray[i] = RootMotionFrames[i];
                }
            }
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
        public float Duration;
        public List<MonoBlobCondition> Conditions;

        public void ToBlobAsset(ref BlobTransitionAsset blob)
        {
            blob.Index = Index;
            blob.Start = Start;
            blob.Duration = Duration;
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
            blob.Mode = (byte) Mode;
            blob.Parameter = (byte) Parameter;
            blob.Threshold = Threshold;
        }
    }
}
