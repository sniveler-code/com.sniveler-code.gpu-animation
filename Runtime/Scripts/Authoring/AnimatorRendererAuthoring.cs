using System.Collections.Generic;
using System.Linq;
using SnivelerCode.GpuAnimation.Runtime.Components;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using Hash128 = Unity.Entities.Hash128;

namespace SnivelerCode.GpuAnimation.Runtime.Authoring
{
    /// <summary>
    /// Authoring component for scene-level GPU animation configuration.
    /// Placed on a GameObject in the scene (or subscene) to register baked matrix assets.
    /// Creates a singleton <see cref="SceneAnimatorConfigData"/> entity with all bone matrices.
    /// </summary>
    public sealed class AnimatorRendererAuthoring : MonoBehaviour
    {
        /// <summary>Array of baked matrix assets to register for this scene.</summary>
        [SerializeField] private AnimatorMatricesAsset[] matrices;

        private sealed class SceneAnimatorBaker : Baker<AnimatorRendererAuthoring>
        {
            public override void Bake(AnimatorRendererAuthoring rendererAuthoring)
            {
                if (rendererAuthoring.matrices == null) return;

                var hashes = new Dictionary<ulong, uint>();
                var validMatrices = rendererAuthoring.matrices
                    .Where(a => a != null && a.MatricesLbs != null).ToArray();

                if (validMatrices.Length == 0) return;

                int totalLbs = 0;
                foreach (var a in validMatrices)
                {
                    DependsOn(a);
                    totalLbs += a.MatricesLbs?.Length ?? 0;
                }

                using var builder = new BlobBuilder(Allocator.Temp);
                var entity = GetEntity(TransformUsageFlags.None);

                ref var root = ref builder.ConstructRoot<GpuBlobAnimationAsset>();
                var lbsArray = builder.Allocate(ref root.MatricesLbs, totalLbs);
                var offsets = builder.Allocate(ref root.Offsets, validMatrices.Length);
                var blobHashes = builder.Allocate(ref root.Hashes, validMatrices.Length);

                uint currentOffsetLbs = 0;
                for (int i = 0; i < validMatrices.Length; i++)
                {
                    var matricesAsset = validMatrices[i];
                    if (hashes.ContainsKey(matricesAsset.UniqueId)) continue;

                    uint currentOffset = currentOffsetLbs;
                    offsets[i] = currentOffset;
                    blobHashes[i] = matricesAsset.UniqueId;
                    hashes[matricesAsset.UniqueId] = currentOffset;

                    var src = matricesAsset.MatricesLbs;
                    for (int m = 0; m < src.Length; m++)
                        lbsArray[(int) currentOffsetLbs + m] = src[m];

                    currentOffsetLbs += (uint) src.Length;
                }

                var blobRef =
                    builder.CreateBlobAssetReference<GpuBlobAnimationAsset>(Allocator.Persistent);

                AddBlobAsset(ref blobRef, out Hash128 _);
                AddComponent(entity, new SceneAnimatorConfigData {Blob = blobRef});
            }
        }
    }
}