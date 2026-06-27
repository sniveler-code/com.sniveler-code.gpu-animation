using System.Collections.Generic;
using System.Runtime.CompilerServices;
using SnivelerCode.GpuAnimation.Runtime.Components;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace SnivelerCode.GpuAnimation.Runtime.Utils
{
    public static class AnimationUtils
    {
        public static GraphicsBuffer DummyBuffer { get; private set; }

        public static int PropertyAnimBufferLbs => Shader.PropertyToID("_SnivelerAnimBufferLBS");

        public static float3x4 Compress(this Matrix4x4 matrix)
        {
            return new float3x4(
                matrix.m00, matrix.m01, matrix.m02, matrix.m03,
                matrix.m10, matrix.m11, matrix.m12, matrix.m13,
                matrix.m20, matrix.m21, matrix.m22, matrix.m23
            );
        }

        public static void InitDummyBuffer()
        {
            if (DummyBuffer != null && DummyBuffer.IsValid()) return;

            ReleaseDummyBuffer();
            var dummyBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 1, 48);
            dummyBuffer.SetData(new List<float3x4> {float3x4.zero});
            Shader.SetGlobalBuffer(PropertyAnimBufferLbs, dummyBuffer);
            DummyBuffer = dummyBuffer;
        }

        public static void ReleaseDummyBuffer()
        {
            if (DummyBuffer == null) return;
            Shader.SetGlobalBuffer(PropertyAnimBufferLbs, (GraphicsBuffer) null);
            if (DummyBuffer.IsValid()) DummyBuffer.Dispose();
            DummyBuffer = null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4x4 Decompress(this float3x4 matrix)
        {
            return new float4x4(
                new float4(matrix.c0, 0f),
                new float4(matrix.c1, 0f),
                new float4(matrix.c2, 0f),
                new float4(matrix.c3, 1f)
            );
        }

        public static AnimatorParamBuilder Value(this byte index, float value) => new(index, value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4x4 Lerp(this float4x4 a, float4x4 b, float t)
        {
            return new float4x4(
                math.lerp(a.c0, b.c0, t),
                math.lerp(a.c1, b.c1, t),
                math.lerp(a.c2, b.c2, t),
                math.lerp(a.c3, b.c3, t)
            );
        }

        public static void Play(this ref AnimatorData data, byte anim, float crossFade = 0.15f)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (crossFade > 0f)
                Debug.LogWarning(
                    "[GpuAnimationEntities] Smooth Crossfade - PRO Feature. " +
                    "LITE performs an instant snap. " +
                    "assetstore.unity.com/packages/tools/animation/gpu-animation-entities-pro-370150");
#endif

            data.Index = anim;
            data.Time = 0f;
            data.Frame = 0;
            data.PrevFrame = 0;
        }
    }

    public readonly struct AnimatorParamBuilder
    {
        private readonly byte _id;
        private readonly float _value;

        public AnimatorParamBuilder(byte id, float value)
        {
            _id = id;
            _value = value;
        }

        public void Apply(DynamicBuffer<AnimatorParameterData> buffer) =>
            buffer[_id] = new AnimatorParameterData {Value = _value};
    }
}
