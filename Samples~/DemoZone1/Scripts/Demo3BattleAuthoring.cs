using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace SnivelerCode.GpuAnimation.DemoZone3
{
    public sealed class Demo3BattleAuthoring : MonoBehaviour
    {
        [SerializeField] private float microCellSize = 2.0f;
        [SerializeField] private float heatCellSize = 10.0f;

        private sealed class Baker : Baker<Demo3BattleAuthoring>
        {
            public override void Bake(Demo3BattleAuthoring data)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new Demo3BattleData
                {
                    MicroCellSize = data.microCellSize,
                    HeatCellSize = data.heatCellSize,
                    GridSize = new int2(20, 20),
                    GridOrigin = new float2(-50, -50)
                });
            }
        }
    }
}
