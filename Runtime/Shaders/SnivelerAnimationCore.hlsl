#ifndef SNIVELER_ANIMATION_CORE_INCLUDED
#define SNIVELER_ANIMATION_CORE_INCLUDED

StructuredBuffer<float3x4> _SnivelerAnimBufferLBS;

struct GpuInstanceAnimState
{
    uint FrameA0;
    uint FrameA1;
    float LerpA;
    uint Padding;
};

StructuredBuffer<GpuInstanceAnimState> _SnivelerInstanceAnimState;

// -----------------------------------------------------------------------------
// LBS UTILITIES
// -----------------------------------------------------------------------------
float3x4 SnivelerGetBlendedBoneMatrix(uint frame, float4 indices, float4 weights)
{
    float3x4 m = _SnivelerAnimBufferLBS[frame + (uint)indices.x] * weights.x;
    m += _SnivelerAnimBufferLBS[frame + (uint)indices.y] * weights.y;
    m += _SnivelerAnimBufferLBS[frame + (uint)indices.z] * weights.z;
    m += _SnivelerAnimBufferLBS[frame + (uint)indices.w] * weights.w;
    return m;
}

// -----------------------------------------------------------------------------
// MAIN ENTRY POINT
// -----------------------------------------------------------------------------
void ApplySnivelerGPUAnimation(
    inout float3 posOS,
    inout float3 normOS,
    inout float3 tangentOS,
    float4 indices,
    float4 weights,
    uint instanceID)
{
    GpuInstanceAnimState state = _SnivelerInstanceAnimState[instanceID];
    float3x4 matrixA = SnivelerGetBlendedBoneMatrix(state.FrameA0, indices, weights);

    float3x4 matrixA1 = SnivelerGetBlendedBoneMatrix(state.FrameA1, indices, weights);
    matrixA = matrixA + (matrixA1 - matrixA) * state.LerpA;

    float3x4 finalMatrix = matrixA;

    posOS = mul(finalMatrix, float4(posOS, 1.0));
    normOS = normalize(mul((float3x3)finalMatrix, normOS));
    tangentOS = normalize(mul((float3x3)finalMatrix, tangentOS));
}

#endif // SNIVELER_ANIMATION_CORE_INCLUDED
