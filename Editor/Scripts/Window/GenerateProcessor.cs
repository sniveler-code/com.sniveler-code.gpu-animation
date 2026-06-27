#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SnivelerCode.GpuAnimation.Runtime.Authoring;
using SnivelerCode.GpuAnimation.Runtime.Components;
using SnivelerCode.GpuAnimation.Runtime.Utils;
using Unity.Entities;
using Unity.Scenes;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SnivelerCode.GpuAnimation.Editor.Window
{
    public sealed class GenerateProcessor
    {
        private readonly Shader[] _shaders;
        private readonly IAnimationBaker _baker;
        private readonly ILodMeshGenerator _lodMeshGenerator;
        private readonly ICodeGenerator _codeGenerator;
        private readonly IAssetFileSystem _fileSystem;

        private const string _generatedNamespace = "SnivelerCode.GpuAnimation.Generated";

        public GenerateProcessor() : this(
            new AnimationBaker(),
            new LodMeshGenerator(),
            new CodeGenerator(),
            new AssetFileSystem())
        {
        }

        private GenerateProcessor(
            IAnimationBaker baker,
            ILodMeshGenerator lodMeshGenerator,
            ICodeGenerator codeGenerator,
            IAssetFileSystem fileSystem)
        {
            _baker = baker;
            _lodMeshGenerator = lodMeshGenerator;
            _codeGenerator = codeGenerator;
            _fileSystem = fileSystem;
        }

        public async Task GenerateAnimationAsset(PrefabInstance instance)
        {
            AnimationBakeResult bakeResult = default;
            try
            {
                if (instance.Source == null) return;

                var world = World.DefaultGameObjectInjectionWorld;
                if (world != null)
                {
                    SubScene[] subScenes = Object.FindObjectsByType<SubScene>
                        (FindObjectsInactive.Include, FindObjectsSortMode.None);

                    foreach (SubScene subScene in subScenes)
                    {
                        if (subScene.SceneGUID.Equals(default)) continue;
                        if (subScene.EditingScene.IsValid() && subScene.EditingScene.isLoaded)
                            EditorSceneManager.CloseScene(subScene.EditingScene, false);
                        SceneSystem.UnloadScene(world.Unmanaged, subScene.SceneGUID);
                    }
                }

                instance.Name = CodeGenerator.ToCamelCase(instance.Source.name);
                string folderResources = _fileSystem.GetGeneratedFolder(instance.Name);
                _fileSystem.ForceDirectory(folderResources);

                // 1. Bake Animations and Bones
                Shader.SetGlobalBuffer(AnimationUtils.PropertyAnimBufferLbs, AnimationUtils.DummyBuffer);
                bakeResult = _baker.BakeAnimations(instance);

                List<MonoBlobAnimator> animations = bakeResult.Animations;
                MonoBlobBones bakedBones = bakeResult.Bones;

                // 2. Generate Code
                string prefabsFolder = Path.Combine(folderResources, "Character");
                _fileSystem.ForceDirectory(prefabsFolder);
                string dataFolder = Path.Combine(folderResources, "Data");
                _fileSystem.ForceDirectory(dataFolder);
                string scriptsFolder = Path.Combine(folderResources, "Scripts");
                _fileSystem.ForceDirectory(scriptsFolder);

                string asmdefPath = Path.Combine(Directory.GetParent(folderResources).FullName,
                    $"{_generatedNamespace}.asmdef");
                await _fileSystem.CreateAsmDef(asmdefPath, _generatedNamespace);

                string paramsCode = _codeGenerator.GenerateParamsCode(instance, animations
                    .Select(a => a.Name).ToArray(), _generatedNamespace);

                await _fileSystem.WriteFile(Path.Combine(scriptsFolder, "GeneratedParams.cs"), paramsCode);

                // 3. Create ScriptableObject for Matrices
                var bufferAsset = ScriptableObject.CreateInstance<AnimatorMatricesAsset>();
                bufferAsset.MatricesLbs = bakeResult.BakedMatricesLbs.ToArray();
                _fileSystem.SaveAsset(bufferAsset, Path.Combine(dataFolder, "AnimatorMatrices.asset"));

                // 4. default animation
                var stateMachine = instance.Animator.layers[0].stateMachine;
                int defaultIndex = instance.Clips
                    .Where(c => c.Enable)
                    .Select(a => a.StateName)
                    .ToList()
                    .IndexOf(stateMachine.defaultState.name);

                // 5. Build LOD Mesh and Prefab
                GameObject rootObject = _lodMeshGenerator.BuildLodMeshProcess(instance, prefabsFolder);
                var animatorComponent = rootObject.AddComponent<AnimatorAuthoring>();
                animatorComponent.BonesCount = bakedBones.BonesNames.Count;
                animatorComponent.Matrices = bufferAsset;
                animatorComponent.Animations = animations;
                animatorComponent.Parameters = instance.Parameters;
                animatorComponent.DefaultAnimation = (byte) (defaultIndex + 1);

                string prefabPath = Path.Combine(prefabsFolder, $"{instance.Name}.prefab");
                PrefabUtility.SaveAsPrefabAsset(rootObject, prefabPath);
                Object.DestroyImmediate(rootObject);
            }
            catch (Exception e)
            {
                Debug.Log("Exception");
                Debug.LogException(e);
            }
            finally
            {
                if (bakeResult.Errors?.Count > 0)
                {
                    EditorUtility.DisplayDialog(
                        "Warning: Animation Settings",
                        string.Join("", bakeResult.Errors),
                        "Got it"
                    );
                }
            }
        }
    }
}

#endif
