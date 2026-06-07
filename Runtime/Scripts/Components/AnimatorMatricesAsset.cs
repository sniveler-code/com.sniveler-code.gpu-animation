using SnivelerCode.GpuAnimation.Runtime.Utils;
using Unity.Mathematics;
using UnityEngine;

namespace SnivelerCode.GpuAnimation.Runtime.Components
{
    public sealed class AnimatorMatricesAsset : ScriptableObject
    {
        public float3x4[] MatricesLbs;
    }
}
