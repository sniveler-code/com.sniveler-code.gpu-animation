using System.Collections.Generic;
using SnivelerCode.GpuAnimation.Runtime.Components;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace SnivelerCode.GpuAnimation.Runtime.Authoring
{
    public sealed class AnimatorAuthoring : MonoBehaviour
    {
        public int BonesCount;
        public AnimatorMatricesAsset Matrices;
        public byte DefaultAnimation;
        public List<MonoBlobAnimator> Animations = new();
        public List<MonoBlobAnimatorParameter> Parameters;

        private sealed class MaterialAnimatorBaker : Baker<AnimatorAuthoring>
        {
            public override void Bake(AnimatorAuthoring data)
            {
                using var builder = new BlobBuilder(Allocator.Temp);
                ref BlobAnimatorAsset blobAsset = ref builder.ConstructRoot<BlobAnimatorAsset>();

                blobAsset.BoneCount = (byte) data.BonesCount;
                blobAsset.MatricesHash = data.Matrices.UniqueId;

                Entity entity = GetEntity(TransformUsageFlags.Dynamic);

                uint triggerMask = 0;
                var paramsBuffer = AddBuffer<AnimatorParameterData>(entity);
                for (int i = 0; i < data.Parameters.Count; i++)
                {
                    var parameter = data.Parameters[i];
                    paramsBuffer.Add(new AnimatorParameterData {Value = parameter.Value});
                    if (parameter.IsTrigger) triggerMask |= 1u << i;
                }

                blobAsset.TriggerMask = triggerMask;

                BlobBuilderArray<BlobAnimationAsset> animationArray =
                    builder.Allocate(ref blobAsset.Animations, data.Animations.Count);
                for (int i = 0; i < animationArray.Length; ++i)
                {
                    MonoBlobAnimator monoAnimation = data.Animations[i];
                    monoAnimation.ToBlobAsset(ref animationArray[i]);
                    if (monoAnimation.Transitions.Count <= 0)
                    {
                        continue;
                    }

                    BlobBuilderArray<BlobTransitionAsset> transitionArray = builder.Allocate(
                        ref animationArray[i].Transitions,
                        monoAnimation.Transitions.Count);

                    for (int k = 0; k < transitionArray.Length; ++k)
                    {
                        MonoBlobTransition monoTransition = data.Animations[i].Transitions[k];
                        monoTransition.ToBlobAsset(ref transitionArray[k]);

                        BlobBuilderArray<BlobConditionAsset> conditionsArray =
                            builder.Allocate(ref transitionArray[k].Conditions,
                                monoTransition.Conditions.Count);

                        for (int m = 0; m < conditionsArray.Length; ++m)
                        {
                            monoTransition.Conditions[m].ToBlobAsset(ref conditionsArray[m]);
                        }
                    }
                }

                BlobAssetReference<BlobAnimatorAsset> blobAssetReference =
                    builder.CreateBlobAssetReference<BlobAnimatorAsset>(Allocator.Persistent);

                AddBlobAsset(ref blobAssetReference, out _);
                AddComponent(entity, new BlobAnimatorData {Value = blobAssetReference});
                AddComponent(entity, new AnimatorGpuIndex {Value = 0});
                AddComponent(entity, new AnimatorData {Index = data.DefaultAnimation});
            }
        }
    }
}
