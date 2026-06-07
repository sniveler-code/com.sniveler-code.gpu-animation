#if UNITY_EDITOR

using System.Collections.Generic;
using System.Threading.Tasks;
using SnivelerCode.GpuAnimation.Runtime.Components;
using SnivelerCode.GpuAnimation.Runtime.Utils;
using Unity.Mathematics;
using UnityEngine;

namespace SnivelerCode.GpuAnimation.Editor.Window
{
    public struct AnimationBakeResult
    {
        public List<MonoBlobAnimator> Animations;
        public MonoBlobBones Bones;
        public Bounds MaxBounds;
        public List<float3x4> BakedMatricesLbs;
        public List<string> Errors;
    }

    public interface IAnimationBaker
    {
        public AnimationBakeResult BakeAnimations(PrefabInstance instance);
    }
}

#endif
