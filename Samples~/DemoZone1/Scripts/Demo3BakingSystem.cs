using SnivelerCode.GpuAnimation.Runtime.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;

namespace SnivelerCode.GpuAnimation.DemoZone3
{
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    [UpdateInGroup(typeof(PostBakingSystemGroup), OrderLast = true)]
    public partial struct Demo3BakingSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            using var ecb = new EntityCommandBuffer(Allocator.Temp);
            foreach ((RefRO<MeshLODComponent> _, RefRO<AnimatorLodTag> _, Entity entity) in
                     SystemAPI.Query<RefRO<MeshLODComponent>,RefRO<AnimatorLodTag>>()
                         .WithOptions(EntityQueryOptions.IncludePrefab | EntityQueryOptions.IncludeDisabledEntities)
                         .WithEntityAccess())
            {
                ecb.AddComponent<Demo3SpawnerTag>(entity);
                ecb.SetComponentEnabled<Demo3SpawnerTag>(entity, true);
            }

            ecb.Playback(state.EntityManager);
        }
    }
}
