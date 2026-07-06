using SnivelerCode.GpuAnimation.Runtime.Components;
using SnivelerCode.GpuAnimation.Runtime.Systems;
using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;

namespace SnivelerCode.GpuAnimation.DemoZone3
{
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    [UpdateInGroup(typeof(PostBakingSystemGroup), OrderLast = true)]
    [UpdateAfter(typeof(AnimatorBakingSystem))]
    public partial struct Demo3BakingSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            using var ecb = new EntityCommandBuffer(Allocator.Temp);
            foreach ((RefRO<MeshLODComponent> _, RefRO<AnimatorLodTag> tag, Entity entity) in
                     SystemAPI.Query<RefRO<MeshLODComponent>, RefRO<AnimatorLodTag>>()
                         .WithOptions(
                             EntityQueryOptions.IncludePrefab |
                             EntityQueryOptions.IncludeDisabledEntities |
                             EntityQueryOptions.IgnoreComponentEnabledState)
                         .WithEntityAccess())
            {
                ecb.AddComponent<Demo3SpawnerTag>(entity);
                ecb.SetComponentEnabled<Demo3SpawnerTag>(entity, true);
            }

            ecb.Playback(state.EntityManager);
        }
    }
}
