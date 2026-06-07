using System;
using System.Collections.Generic;
using System.Linq;
using SnivelerCode.GpuAnimation.Runtime.Utils;
using Unity.Mathematics;
using UnityEngine;

namespace SnivelerCode.GpuAnimation.Runtime.Components
{
    [Serializable]
    public class MonoBlobBones
    {
        public List<string> BonesNames;
        public List<MonoBlobBone> BlobBones;

        public MonoBlobBones(Transform[] bones)
        {
            BonesNames = new List<string>();
            BlobBones = new List<MonoBlobBone>();
            Array.ForEach(bones, bone => BonesNames.Add(bone.name));
        }

        public void Add(int boneIndex, int animationsCount, Matrix4x4 matrix)
        {
            var monoBone = BlobBones.FirstOrDefault(bone => bone.Index == boneIndex);
            if (monoBone is null)
            {
                monoBone = new MonoBlobBone
                {
                    Index = boneIndex,
                    Animations = new List<MonoBlobBoneAnimation>()
                };
                BlobBones.Add(monoBone);
            }

            var monoBoneAnimation = monoBone.Animations.FirstOrDefault(anim => anim.Index == animationsCount);
            if (monoBoneAnimation is null)
            {
                monoBoneAnimation = new MonoBlobBoneAnimation
                {
                    Index = animationsCount,
                    Frames = new List<float3x4>()
                };
                monoBone.Animations.Add(monoBoneAnimation);
            }

            monoBoneAnimation.Frames.Add(matrix.Compress());
        }
    }

    [Serializable]
    public class MonoBlobBone
    {
        public int Index;
        public List<MonoBlobBoneAnimation> Animations;
    }

    [Serializable]
    public class MonoBlobBoneAnimation
    {
        public int Index;
        public List<float3x4> Frames;
    }
}
