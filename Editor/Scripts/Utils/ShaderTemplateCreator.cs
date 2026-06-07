#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
using UnityEngine;

namespace SnivelerCode.GpuAnimation.Editor.Utils
{
    public sealed class CreateShaderGraphAction : EndNameEditAction
    {
        public override void Action(int instanceId, string pathName, string resourceFile)
        {
            File.Copy(resourceFile, pathName);
            AssetDatabase.ImportAsset(pathName);

            var obj = AssetDatabase.LoadAssetAtPath<Shader>(pathName);
            ProjectWindowUtil.ShowCreatedAsset(obj);
        }
    }

    public static class ShaderTemplateCreator
    {
        [MenuItem("Assets/Create/Shader Graph/Sniveler GPU Animation/Lit Shader", false, 200)]
        public static void CreateLitShader()
        {
            CreateFromTemplate(AnimatorStrings.LitTemplateName, "New Sniveler Lit Shader.shadergraph");
        }

        [MenuItem("Assets/Create/Shader Graph/Sniveler GPU Animation/Unlit Shader", false, 201)]
        public static void CreateUnlitShader()
        {
            CreateFromTemplate(AnimatorStrings.UnlitTemplateName, "New Sniveler Unlit Shader.shadergraph");
        }

        private static void CreateFromTemplate(string templateName, string defaultFileName)
        {
            string[] guids = AssetDatabase.FindAssets($"{templateName} t:Shader");
            if (guids.Length == 0)
            {
                Debug.LogError(
                    $"[SCGpuAnimation] Template '{templateName}' not found! " +
                    "Please ensure the package is imported correctly.");
                return;
            }

            string templatePath = AssetDatabase.GUIDToAssetPath(guids[0]);
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(
                0,
                ScriptableObject.CreateInstance<CreateShaderGraphAction>(),
                defaultFileName,
                null,
                templatePath
            );
        }
    }
}
#endif
