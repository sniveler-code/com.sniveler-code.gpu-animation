#ifndef SNIVELER_SG_WRAPPER_INCLUDED
#define SNIVELER_SG_WRAPPER_INCLUDED

#include "SnivelerAnimationCore.hlsl"

void ApplySnivelerAnimation_float(
    float3 PositionOS,
    float3 NormalOS,
    float3 TangentOS,
    float4 BoneIndices,
    float4 BoneWeights,
    float4 FramesA,
    out float3 OutPositionOS,
    out float3 OutNormalOS,
    out float3 OutTangentOS)
{
    OutPositionOS = PositionOS;
    OutNormalOS = NormalOS;
    OutTangentOS = TangentOS;

    #if defined(SHADER_STAGE_VERTEX)

    #if !defined(SHADERGRAPH_PREVIEW)
    ApplySnivelerGPUAnimation(
        OutPositionOS,
        OutNormalOS,
        OutTangentOS,
        BoneIndices,
        BoneWeights,
        FramesA
    );
    #endif

    #endif
}

#endif // SNIVELER_SG_WRAPPER_INCLUDED
