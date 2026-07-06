using SnivelerCode.GpuAnimation.Runtime.Components;
using SnivelerCode.GpuAnimation.Runtime.Utils;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

namespace SnivelerCode.GpuAnimation.Runtime.Systems
{
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    [UpdateInGroup(typeof(PostBakingSystemGroup), OrderLast = true)]
    public partial struct AnimatorBakingSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SceneAnimatorConfigData>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var sceneConfig = SystemAPI.GetSingleton<SceneAnimatorConfigData>();
            if (!sceneConfig.Blob.IsCreated)
            {
                AnimatorLogger.ErrorManaged("Scene Renderer doesn't initialised");
                return;
            }

            using var ecb = new EntityCommandBuffer(Allocator.Temp);
            ref var blobData = ref sceneConfig.Blob.Value;
            var hashesLookup = new NativeHashMap<int, uint>(256, Allocator.Temp);
            for (int i = 0; i < blobData.Hashes.Length; i++)
            {
                hashesLookup[blobData.Hashes[i]] = blobData.Offsets[i];
            }

            int entityCount = 0;
            var updatedEntities = new NativeHashMap<Entity, uint>(256, Allocator.Temp);
            foreach ((RefRW<BlobAnimatorData> data, Entity entity) in SystemAPI.Query<RefRW<BlobAnimatorData>>()
                         .WithOptions(EntityQueryOptions.IncludePrefab | EntityQueryOptions.IncludeDisabledEntities)
                         .WithEntityAccess())
            {
                int matricesHash = data.ValueRO.Value.Value.MatricesHash;
                if (!hashesLookup.ContainsKey(matricesHash)) continue;
                data.ValueRW.Offset = hashesLookup[matricesHash];
                updatedEntities.Add(entity, hashesLookup[matricesHash]);
                entityCount++;
            }

            AnimatorLogger.LogManaged($"Matrices Count: {hashesLookup.Count}");
            AnimatorLogger.LogManaged($"Entity Updated: {entityCount}");

            int lodsCount = 0;
            foreach ((RefRO<MeshLODComponent> lod, Entity entity) in SystemAPI.Query<RefRO<MeshLODComponent>>()
                         .WithOptions(EntityQueryOptions.IncludePrefab | EntityQueryOptions.IncludeDisabledEntities)
                         .WithEntityAccess())
            {
                ecb.AddComponent(entity, new SnivelerMaterialBaseColor {Value = new float4(1, 1, 1, 1)});

                ecb.AddComponent<AnimatorLodTag>(entity);
                ecb.SetComponentEnabled<AnimatorLodTag>(entity, false);
                ecb.AddComponent<SnivelerInstanceID>(entity);

                lodsCount++;
            }

            AnimatorLogger.LogManaged($"Total LODs processed: {lodsCount}");

            ecb.Playback(state.EntityManager);
            hashesLookup.Dispose();
            updatedEntities.Dispose();
        }
    }
}
