using UnityEngine;
using SnivelerCode.GpuAnimation.Runtime.Utils;

namespace SnivelerCode.GpuAnimation.Editor.Utils
{
    public static class AnimatorStrings
    {
        public const string LitTemplateName = "Sniveler_Lit_Template";
        public const string UnlitTemplateName = "Sniveler_Unlit_Template";
    }

    public static class AnimatorShaderProperty
    {
        // Use centralized ShaderPropertyIDs
        public static readonly int SurfaceId = ShaderPropertyIDs.Surface;
        public static readonly int BlendId = ShaderPropertyIDs.Blend;
        public static readonly int SrcBlendId = ShaderPropertyIDs.SrcBlend;
        public static readonly int DstBlendId = ShaderPropertyIDs.DstBlend;
        public static readonly int ZWriteId = ShaderPropertyIDs.ZWrite;
        public static readonly int ZTestId = ShaderPropertyIDs.ZTest;
        public static readonly int BaseColorId = ShaderPropertyIDs.BaseColor;
        public static readonly int ColorId = ShaderPropertyIDs.Color;

        public static readonly int BaseMapId = ShaderPropertyIDs.BaseMap;
        public static readonly int MainTexId = ShaderPropertyIDs.MainTex;
        public static readonly int InstanceID = ShaderPropertyIDs.InstanceID;
    }

    public static class EditorAnimatorUtils
    {

        public static Material GetDebugMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Hidden/Internal-Colored");

            var result = new Material(shader) {hideFlags = HideFlags.HideAndDontSave};
            result.SetFloat(AnimatorShaderProperty.SurfaceId, 1);
            result.SetFloat(AnimatorShaderProperty.BlendId, 0);
            result.SetInt(AnimatorShaderProperty.SrcBlendId, (int) UnityEngine.Rendering.BlendMode.SrcAlpha);
            result.SetInt(AnimatorShaderProperty.DstBlendId, (int) UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            result.SetInt(AnimatorShaderProperty.ZWriteId, 0);
            result.SetInt(AnimatorShaderProperty.ZTestId, (int) UnityEngine.Rendering.CompareFunction.LessEqual);
            result.renderQueue = (int) UnityEngine.Rendering.RenderQueue.Transparent;
            result.SetColor(AnimatorShaderProperty.BaseColorId, new Color(0f, 1f, 0.2f, 0.6f));
            result.SetColor(AnimatorShaderProperty.ColorId, new Color(0f, 1f, 0.2f, 0.6f));

            return result;
        }
    }
}
