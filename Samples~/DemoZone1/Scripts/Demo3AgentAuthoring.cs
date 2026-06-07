using System;
using SnivelerCode.GpuAnimation.Runtime.Authoring;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

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

        [SerializeField] private float radius = 0.5f;
        [SerializeField, Demo3Params] private byte paramSpeedIndex;
        [SerializeField, Demo3Animation] private byte animationHitIndex;
        [SerializeField, Demo3Animation] private byte animationDeathIndex;

        [SerializeField] private AttackSetup attacks;

        private sealed class Baker : Baker<Demo3AgentAuthoring>
        {
            public override void Bake(Demo3AgentAuthoring data)
            {
                var builder = new BlobBuilder(Allocator.Temp);
                try
                {
                    Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                    AddComponent(entity, new Demo3UnitConfig
                    {
                        Radius = data.radius,
                        Attacks = new Demo3AttackProfile
                        {
                            AnimationIndex = data.attacks.AnimationIndex,
                            DamageFrame = data.attacks.DamageFrame,
                            RangeSq = data.attacks.Range * data.attacks.Range,
                            Damage = data.attacks.Damage,
                            Cooldown = data.attacks.Cooldown
                        },
                        AnimationHitIndex = data.animationHitIndex,
                        ParamSpeedIndex = data.paramSpeedIndex,
                        AnimationDeathIndex = data.animationDeathIndex
                    });

                    AddComponent<Demo3DeadData>(entity, default);
                    AddComponent<Demo3SpawnerTag>(entity, default);
                    AddComponent<Demo3CombatData>(entity, default);
                    SetComponentEnabled<Demo3DeadData>(entity, false);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
                finally
                {
                    builder.Dispose();
                }
            }
        }
    }
}
