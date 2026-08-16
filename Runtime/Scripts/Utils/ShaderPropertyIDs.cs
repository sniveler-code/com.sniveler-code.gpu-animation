using UnityEngine;

namespace SnivelerCode.GpuAnimation.Runtime.Utils
{
    /// <summary>
    /// Centralized shader property IDs for GPU Animation system.
    /// Using Shader.PropertyToID for performance (avoids string lookups at runtime).
    /// </summary>
    public static class ShaderPropertyIDs
    {
        // Animation Buffers
        public static readonly int AnimBufferLBS = Shader.PropertyToID("_SnivelerAnimBufferLBS");
        public static readonly int AnimBufferState = Shader.PropertyToID("_SnivelerInstanceAnimState");

        // Material Properties (MaterialPropertyBlock / Shader Graph)
        public static readonly int InstanceID = Shader.PropertyToID("_SnivelerInstanceID");
        public static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        public static readonly int BaseMap = Shader.PropertyToID("_BaseMap");
        public static readonly int MainTex = Shader.PropertyToID("_MainTex");

        // URP Surface Properties
        public static readonly int Surface = Shader.PropertyToID("_Surface");
        public static readonly int Blend = Shader.PropertyToID("_Blend");
        public static readonly int SrcBlend = Shader.PropertyToID("_SrcBlend");
        public static readonly int DstBlend = Shader.PropertyToID("_DstBlend");
        public static readonly int ZWrite = Shader.PropertyToID("_ZWrite");
        public static readonly int ZTest = Shader.PropertyToID("_ZTest");
        public static readonly int Color = Shader.PropertyToID("_Color");

        // Debug/Editor
        public static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        // String names for shader property lookup (e.g., Shader.FindPropertyIndex)
        public const string Str_InstanceID = "_SnivelerInstanceID";
        public const string Str_BaseColor = "_BaseColor";
        public const string Str_BaseMap = "_BaseMap";
        public const string Str_MainTex = "_MainTex";
    }
}