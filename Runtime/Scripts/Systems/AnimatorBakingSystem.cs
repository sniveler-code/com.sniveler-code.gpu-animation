using SnivelerCode.GpuAnimation.Runtime.Components;
using SnivelerCode.GpuAnimation.Runtime.Utils;
using Unity.Burst;
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
            state.RequireForUpdate<AnimatorPrefabBuffer>();
            state.RequireForUpdate<SceneAnimatorConfigData>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var prefabsBuffer = SystemAPI.GetSingletonBuffer<AnimatorPrefabBuffer>();
            var sceneConfig = SystemAPI.GetSingleton<SceneAnimatorConfigData>();

            if (!sceneConfig.Blob.IsCreated)
            {
                AnimatorLogger.ErrorManaged("Scene Renderer doesn't initialised");
                return;
            }

            using var ecb = new EntityCommandBuffer(Allocator.Temp);
            ref var blobData = ref sceneConfig.Blob.Value;

            // fill offsets
            var nativeHashMap = new NativeHashMap<Entity, uint>(256, Allocator.Temp);
            for (int i = 0; i < prefabsBuffer.Length; i++)
            {
                var prefabEntity = prefabsBuffer[i].Value;
                uint prefabOffset = blobData.Offsets[i];

                if (state.EntityManager.HasComponent<BlobAnimatorData>(prefabEntity))
                {
                    var blobAnimatorData = state.EntityManager.GetComponentData<BlobAnimatorData>(prefabEntity);
                    blobAnimatorData.Offset = prefabOffset;
                    ecb.SetComponent(prefabEntity, blobAnimatorData);
                    nativeHashMap.Add(prefabEntity, prefabOffset);
                }
            }

            AnimatorLogger.LogManaged($"Blob count after offsets filled: {nativeHashMap.Count}");

            int lodsCount = 0;
            foreach ((RefRO<MeshLODComponent> lod, Entity entity) in SystemAPI.Query<RefRO<MeshLODComponent>>()
                         .WithOptions(EntityQueryOptions.IncludePrefab | EntityQueryOptions.IncludeDisabledEntities)
                         .WithEntityAccess())
            {
                if (!nativeHashMap.ContainsKey(lod.ValueRO.Group)) continue;
                uint prefabOffset = nativeHashMap[lod.ValueRO.Group];
                if (!state.EntityManager.HasComponent<AnimatorBakeLodsData>(lod.ValueRO.Group)) continue;
                var lodsData = state.EntityManager.GetComponentData<AnimatorBakeLodsData>(lod.ValueRO.Group);

                uint startFrame = prefabOffset + lodsData.Frame;
                ecb.AddComponent(entity, new SnivelerMaterialFrames
                    {Value = new float4(startFrame, startFrame, 1, 1)});

                ecb.AddComponent(entity, new SnivelerMaterialFramesTarget
                    {Value = new float4(startFrame, startFrame, 1, 1)});

                ecb.AddComponent(entity, new SnivelerMaterialBaseColor
                    {Value = new float4(1, 1, 1, 1)});

                ecb.AddComponent<AnimatorLodTag>(entity);
                lodsCount++;
            }

            AnimatorLogger.LogManaged($"Total LODs processed: {lodsCount}");

            ecb.Playback(state.EntityManager);
            nativeHashMap.Dispose();
        }
    }
}
