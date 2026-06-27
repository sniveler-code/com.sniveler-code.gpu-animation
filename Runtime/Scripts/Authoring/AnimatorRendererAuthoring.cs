using System.Collections.Generic;
using System.Linq;
using SnivelerCode.GpuAnimation.Runtime.Components;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using Hash128 = Unity.Entities.Hash128;

namespace SnivelerCode.GpuAnimation.Runtime.Authoring
{
    public sealed class AnimatorRendererAuthoring : MonoBehaviour
    {
        [SerializeField] private AnimatorAuthoring[] animators;
        public AnimatorAuthoring[] Animators => animators;
    }

    public sealed class SceneAnimatorBaker : Baker<AnimatorRendererAuthoring>
    {
        public override void Bake(AnimatorRendererAuthoring rendererAuthoring)
        {
            if (rendererAuthoring.Animators == null) return;

            var hashes = new Dictionary<int, uint>();
            var validAnimators = rendererAuthoring.Animators
                .Where(a => a != null && a.Matrices != null).ToArray();

            if (validAnimators.Length == 0) return;

            int totalLbs = 0;
            foreach (var a in validAnimators)
            {
                DependsOn(a.Matrices);
                totalLbs += a.Matrices.MatricesLbs?.Length ?? 0;
            }

            using var builder = new BlobBuilder(Allocator.Temp);
            var entity = GetEntity(TransformUsageFlags.None);
            var prefabBuffer = AddBuffer<AnimatorPrefabBuffer>(entity);

            ref var root = ref builder.ConstructRoot<GpuBlobAnimationAsset>();
            var lbsArray = builder.Allocate(ref root.MatricesLbs, totalLbs);
            var offsets = builder.Allocate(ref root.Offsets, validAnimators.Length);

            uint currentOffsetLbs = 0;
            for (int i = 0; i < validAnimators.Length; i++)
            {
                var animator = validAnimators[i];
                var prefabEntity = GetEntity(animator, TransformUsageFlags.Dynamic);
                prefabBuffer.Add(new AnimatorPrefabBuffer {Value = prefabEntity});

                int hashInstance = animator.Matrices.GetInstanceID();
                if (hashes.TryGetValue(hashInstance, out uint offset))
                {
                    offsets[i] = offset;
                    continue;
                }

                uint currentOffset = currentOffsetLbs;
                offsets[i] = currentOffset;
                hashes[hashInstance] = currentOffset;

                var src = animator.Matrices.MatricesLbs;
                if (src == null) continue;

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
