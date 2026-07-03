using Unity.Entities;
using UnityEngine;

namespace SnivelerCode.GpuAnimation.DemoZone3
{
    public struct Demo3SpawnerData : IComponentData
    {
        public Demo3Faction Faction;
        public Entity Prefab;
        public ushort Count;
        public float Progress;
        public float SpawnTime;
        public byte Columns;
        public float Spacing;
    }

    public struct Demo3DebugRedWarriorTag : IComponentData
    {
    }

    public struct Demo3DebugBlueWarriorTag : IComponentData
    {
    }

    public sealed class Demo3SpawnerAuthoring : MonoBehaviour
    {
        [SerializeField] private ushort count;
        [SerializeField] private Demo3Faction faction;
        [SerializeField] private Demo3AgentAuthoring prefab;
        [SerializeField] private float spawnTime;
        [SerializeField] private byte columns = 5;
        [SerializeField] private float spacing = 2.0f;

        private sealed class Baker : Baker<Demo3SpawnerAuthoring>
        {
            public override void Bake(Demo3SpawnerAuthoring data)
            {
                if (data.prefab == null) return;
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new Demo3SpawnerData
                {
                    Faction = data.faction,
                    Prefab = GetEntity(data.prefab, TransformUsageFlags.Dynamic),
                    Count = data.count,
                    Progress = data.spawnTime,
                    SpawnTime = data.spawnTime,
                    Columns = data.columns,
                    Spacing = data.spacing
                });

                switch (data.faction)
                {
                    case Demo3Faction.Red when data.prefab.Type == Demo3UnitType.Melee:
                        AddComponent<Demo3DebugRedWarriorTag>(entity);
                        break;

                    case Demo3Faction.Blue when data.prefab.Type == Demo3UnitType.Melee:
                        AddComponent<Demo3DebugBlueWarriorTag>(entity);
                        break;
                }
            }
        }
    }
}
