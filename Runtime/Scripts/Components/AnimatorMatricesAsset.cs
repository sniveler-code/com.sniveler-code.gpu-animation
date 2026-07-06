using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace SnivelerCode.GpuAnimation.Runtime.Components
{
    [TemporaryBakingType]
    public sealed class AnimatorMatricesAsset : ScriptableObject
    {
        public float3x4[] MatricesLbs;
    }
}
