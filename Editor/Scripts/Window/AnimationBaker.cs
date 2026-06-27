#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Linq;
using SnivelerCode.GpuAnimation.Runtime.Components;
using SnivelerCode.GpuAnimation.Runtime.Utils;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Animations;

using UnityEngine.Playables;
using Object = UnityEngine.Object;

#if SNC_USE_RIGGING
using UnityEngine.Animations.Rigging;
#endif

namespace SnivelerCode.GpuAnimation.Editor.Window
{
    public sealed class AnimationBaker : IAnimationBaker
    {
        public AnimationBakeResult BakeAnimations(PrefabInstance instance)
        {
            var result = new AnimationBakeResult
            {
                BakedMatricesLbs = new List<float3x4>(),
                Animations = new List<MonoBlobAnimator>
                {
                    new()
                    {
                        Name = "TPose",
                        Frames = 1,
                        Start = 0,
                        Speed = 1
                    }
                },
                Bones = new MonoBlobBones(instance.MasterBones),
                MaxBounds = new Bounds(),
                Errors = new List<string>()
            };

            for (int boneIndex = 0; boneIndex < instance.MasterBones.Length; boneIndex++)
            {
                Transform bone = instance.MasterBones[boneIndex];
                Matrix4x4 bindpose = instance.MasterBindposes[boneIndex];
                Matrix4x4 localBoneMatrix = instance.Source.transform.worldToLocalMatrix * bone.localToWorldMatrix;
                Matrix4x4 finalMatrix = localBoneMatrix * bindpose;
                result.BakedMatricesLbs.Add(finalMatrix.Compress());
            }

            AnimatorStateMachine stateMachine = instance.Animator.layers[0].stateMachine;
            List<ClipInstance> enabledAnimations =
                (from clip in instance.Clips where clip.Enable select clip).ToList();

            GameObject clonedPrefab = Object.Instantiate(instance.Source);

            var clonedRenderers = clonedPrefab.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (var r in clonedRenderers) r.updateWhenOffscreen = true;
            if (clonedRenderers.Length > 0) result.MaxBounds = clonedRenderers[0].localBounds;

            // setup animator
            string lodAssetPath = AssetDatabase.GetAssetPath(instance.Lods[0].Skins[0].sharedMesh);
            if (!clonedPrefab.TryGetComponent(out Animator animatorComponent))
                animatorComponent = clonedPrefab.AddComponent<Animator>();

            Object[] allSubAssets = AssetDatabase.LoadAllAssetsAtPath(lodAssetPath);
            Avatar extractedAvatar = allSubAssets.OfType<Avatar>().FirstOrDefault();
            if (extractedAvatar == null)
            {
                ModelImporter modelImporter = AssetImporter.GetAtPath(lodAssetPath) as ModelImporter;
                if (modelImporter != null && modelImporter.avatarSetup == ModelImporterAvatarSetup.CopyFromOther)
                {
                    extractedAvatar = modelImporter.sourceAvatar;
                }
            }

            if (extractedAvatar == null) throw new Exception("Avatar not found in the mesh asset.");

            animatorComponent.avatar = extractedAvatar;
            animatorComponent.applyRootMotion = false;
            animatorComponent.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            var tempController = new AnimatorController();
            tempController.AddLayer("Base Layer");
            animatorComponent.runtimeAnimatorController = tempController;

            Transform[] clonedMasterBones = new Transform[instance.MasterBones.Length];
            Transform[] allClonedTransforms = clonedPrefab.GetComponentsInChildren<Transform>();
            for (int i = 0; i < instance.MasterBones.Length; i++)
            {
                clonedMasterBones[i] = allClonedTransforms
                    .FirstOrDefault(t => t.name == instance.MasterBones[i].name);
            }

            foreach (ClipInstance clip in enabledAnimations)
            {
                ChildAnimatorState animatorState = stateMachine.states
                    .FirstOrDefault(s => s.state.name == clip.StateName);

                var animationClip = (AnimationClip) animatorState.state.motion;
                ushort frameCount = (ushort) (animationClip.length * clip.Fps);
                var materialAnimation = new MonoBlobAnimator
                {
                    FPS = (byte) clip.Fps,
                    Frames = frameCount,
                    Start = (uint) result.BakedMatricesLbs.Count,
                    Loop = animationClip.isLooping,
                    Speed = animatorState.state.speed,
                    Transitions = new List<MonoBlobTransition>(),
                    Name = CodeGenerator.ToCamelCase(animationClip.name)
                };

                clonedPrefab.transform.position = Vector3.zero;
                clonedPrefab.transform.rotation = Quaternion.identity;

                animatorComponent.runtimeAnimatorController = null;
                animatorComponent.applyRootMotion = false;
                animatorComponent.cullingMode = AnimatorCullingMode.AlwaysAnimate;

                PlayableGraph graph = PlayableGraph.Create("BakeGraph");
                graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);

                var output = AnimationPlayableOutput.Create(graph, "Animation", animatorComponent);
                var clipPlayable = AnimationClipPlayable.Create(graph, animationClip);
                clipPlayable.Pause();
                Playable finalPlayable = clipPlayable;
#if SNC_USE_RIGGING
                var rigBuilder = clonedPrefab.GetComponent<RigBuilder>();

                if (rigBuilder != null && rigBuilder.enabled)
                {
                    rigBuilder.StartPreview();
                    finalPlayable = rigBuilder.BuildPreviewGraph(graph, clipPlayable);
                }
#endif
                output.SetSourcePlayable(finalPlayable);
                graph.Play();

                for (int frame = 0; frame < frameCount; frame++)
                {
                    float time = frame * (1f / clip.Fps);
                    float dt = frame == 0 ? 0f : 1f / clip.Fps;
                    clipPlayable.SetTime(time);
#if SNC_USE_RIGGING
                    if (rigBuilder != null && rigBuilder.enabled) rigBuilder.UpdatePreviewGraph(graph);
#endif
                    graph.Evaluate(dt);

                    foreach (var r in clonedRenderers) result.MaxBounds.Encapsulate(r.localBounds);
                    Vector3 lossyScale = clonedPrefab.transform.lossyScale;
                    Matrix4x4 scaleMatrix = ScaleMatrix(lossyScale);

                    Matrix4x4 rootMatrix = Matrix4x4.identity;
                    Matrix4x4 stripMatrix = rootMatrix.inverse;
                    for (int boneIndex = 0; boneIndex < clonedMasterBones.Length; boneIndex++)
                    {
                        Transform bone = clonedMasterBones[boneIndex];
                        Matrix4x4 bindpose = instance.MasterBindposes[boneIndex];

                        Matrix4x4 localBoneMatrix = stripMatrix * bone.localToWorldMatrix;
                        Matrix4x4 matrix = localBoneMatrix * bindpose * scaleMatrix;

                        result.BakedMatricesLbs.Add(matrix.Compress());
                    }

                    SceneView.RepaintAll();
                }
#if SNC_USE_RIGGING
                if (rigBuilder != null && rigBuilder.enabled) rigBuilder.StopPreview();
#endif
                if (graph.IsValid()) graph.Destroy();
                result.Animations.Add(materialAnimation);
            }

            // cache params
            var parameterIndices = new Dictionary<string, int>();
            for (int p = 0; p < instance.Animator.parameters.Length; p++)
            {
                parameterIndices[instance.Animator.parameters[p].name] = p;
            }

            var anyStateTransitionsData =
                new List<(MonoBlobTransition blob, bool canTransitionToSelf, int targetIndex)>();
            foreach (AnimatorStateTransition anyTransition in stateMachine.anyStateTransitions)
            {
                ClipInstance targetInstance = enabledAnimations
                    .FirstOrDefault(x => x.StateName == anyTransition.destinationState.name);

                if (targetInstance == null) continue;
                int targetIndex = enabledAnimations.IndexOf(targetInstance) + 1;

                var conditions = new List<MonoBlobCondition>(anyTransition.conditions.Length);
                foreach (var cond in anyTransition.conditions)
                {
                    if (parameterIndices.TryGetValue(cond.parameter, out int paramIndex))
                    {
                        conditions.Add(new MonoBlobCondition
                        {
                            Mode = (byte) cond.mode,
                            Threshold = cond.threshold,
                            Parameter = paramIndex
                        });
                    }
                }

                var blobTransition = new MonoBlobTransition
                {
                    Index = (byte) targetIndex,
                    Start = 0,
                    Conditions = conditions
                };

                anyStateTransitionsData.Add((blobTransition, anyTransition.canTransitionToSelf, targetIndex));
            }

            // bake transitions
            for (int j = 0; j < enabledAnimations.Count; ++j)
            {
                ClipInstance clip = enabledAnimations[j];
                ChildAnimatorState animatorState = stateMachine.states
                    .FirstOrDefault(s => s.state.name == clip.StateName);

                var animationClip = (AnimationClip) animatorState.state.motion;
                ushort frameCount = (ushort) (animationClip.length * clip.Fps);

                int currentAnimIndex = j + 1;
                MonoBlobAnimator baseAnimation = result.Animations[currentAnimIndex];

                foreach (var anyData in anyStateTransitionsData)
                {
                    if (!anyData.canTransitionToSelf && anyData.targetIndex == currentAnimIndex)
                    {
                        continue;
                    }

                    baseAnimation.Transitions.Add(anyData.blob);
                }

                foreach (AnimatorStateTransition transition in animatorState.state.transitions)
                {
                    ClipInstance targetInstance =
                        enabledAnimations.FirstOrDefault(x => x.StateName == transition.destinationState.name);
                    if (targetInstance == null) continue;

                    int targetIndex = enabledAnimations.IndexOf(targetInstance) + 1;

                    // has exit time
                    ushort startFrame = 0;
                    if (transition.hasExitTime)
                    {
                        startFrame = (ushort) (frameCount - 3);
                    }

                    // collect conditions
                    var conditions = new List<MonoBlobCondition>(transition.conditions.Length);
                    foreach (var cond in transition.conditions)
                    {
                        if (parameterIndices.TryGetValue(cond.parameter, out int paramIndex))
                        {
                            conditions.Add(new MonoBlobCondition
                            {
                                Mode = (byte) cond.mode,
                                Threshold = cond.threshold,
                                Parameter = paramIndex
                            });
                        }
                    }

                    // add transition
                    result.Animations[j + 1].Transitions.Add(new MonoBlobTransition
                    {
                        Index = (byte) targetIndex,
                        Start = startFrame,
                        Conditions = conditions
                    });
                }
            }

            Object.DestroyImmediate(clonedPrefab);
            return result;
        }

        private static Matrix4x4 ScaleMatrix(Vector3 scale) =>
            Matrix4x4.Scale(new Vector3(1f / scale.x, 1f / scale.y, 1f / scale.z));
    }
}

#endif
