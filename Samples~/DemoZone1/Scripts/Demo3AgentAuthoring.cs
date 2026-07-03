using System;
using SnivelerCode.GpuAnimation.Runtime.Authoring;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using Hash128 = Unity.Entities.Hash128;

namespace SnivelerCode.GpuAnimation.DemoZone3
{
    [RequireComponent(typeof(AnimatorAuthoring))]
    public sealed class Demo3AgentAuthoring : MonoBehaviour
    {
        [Serializable]
        public struct AttackSetup
        {
            [Demo3Animation] public byte AnimationIndex;
            public ushort DamageFrame;
            public float Range;
            public float Damage;
            public float Cooldown;
        }

        [SerializeField] private Demo3UnitType type;
        [SerializeField] private float radius = 0.5f;
        [SerializeField, Demo3Params] private byte paramSpeedIndex;
        [SerializeField, Demo3Animation] private byte animationHitIndex;
        [SerializeField, Demo3Animation] private byte animationDeathIndex;
        [SerializeField] private AttackSetup attacks;

        public Demo3UnitType Type => type;

        private sealed class Baker : Baker<Demo3AgentAuthoring>
        {
            public override void Bake(Demo3AgentAuthoring data)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                var builder = new BlobBuilder(Allocator.Temp);
                try
                {
                    ref Demo3UnitConfigBlob root = ref builder.ConstructRoot<Demo3UnitConfigBlob>();

                    root.Radius = data.radius;
                    root.Attacks = new Demo3AttackProfile
                    {
                        AnimationIndex = data.attacks.AnimationIndex,
                        DamageFrame = data.attacks.DamageFrame,
                        RangeSq = data.attacks.Range * data.attacks.Range,
                        Damage = data.attacks.Damage,
                        Cooldown = data.attacks.Cooldown
                    };
                    root.AnimationHitIndex = data.animationHitIndex;
                    root.ParamSpeedIndex = data.paramSpeedIndex;
                    root.AnimationDeathIndex = data.animationDeathIndex;
                    root.UnityType = data.type;

                    var blobRef = builder
                        .CreateBlobAssetReference<Demo3UnitConfigBlob>(Allocator.Persistent);

                    AddBlobAsset(ref blobRef, out Hash128 _);
                    AddComponent(entity, new Demo3UnitConfig {Value = blobRef});
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
                finally
                {
                    builder.Dispose();
                }

                AddComponent<Demo3CombatData>(entity, default);

                AddComponent<Demo3DeadData>(entity, default);
                SetComponentEnabled<Demo3DeadData>(entity, false);
            }
        }
    }
}
