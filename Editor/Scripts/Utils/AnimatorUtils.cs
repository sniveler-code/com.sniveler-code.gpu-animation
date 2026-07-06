using UnityEngine;

namespace SnivelerCode.GpuAnimation.Editor.Utils
{
    public static class AnimatorStrings
    {
        public const string RenderFrames = "_SnivelerInstanceID";
        public const string LitTemplateName = "Sniveler_Lit_Template";
        public const string UnlitTemplateName = "Sniveler_Unlit_Template";
    }

    public static class AnimatorShaderProperty
    {
        public static readonly int SurfaceId = Shader.PropertyToID("_Surface");
        public static readonly int BlendId = Shader.PropertyToID("_Blend");
        public static readonly int SrcBlendId = Shader.PropertyToID("_SrcBlend");
        public static readonly int DstBlendId = Shader.PropertyToID("_DstBlend");
        public static readonly int ZWriteId = Shader.PropertyToID("_ZWrite");
        public static readonly int ZTestId = Shader.PropertyToID("_ZTest");
        public static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        public static readonly int ColorId = Shader.PropertyToID("_Color");

        public static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        public static readonly int MainTexId = Shader.PropertyToID("_MainTex");
        public static int InstanceID => Shader.PropertyToID(AnimatorStrings.RenderFrames);
    }

    public static class AnimatorUtils
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
