using System.Runtime.InteropServices;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

namespace SnivelerCode.GpuAnimation.DemoZone3
{
    public struct Demo3AttackProfile
    {
        public byte AnimationIndex;
        public ushort DamageFrame;
        public float RangeSq;
        public float Damage;
        public float Cooldown;
    }

    public struct Demo3BattleData : IComponentData
    {
        public float MicroCellSize;
        public float HeatCellSize;
        public int2 GridSize;
        public float2 GridOrigin;
    }

    public struct HeatmapCell
    {
        public int RedCount;
        public int BlueCount;
    }

    public enum Demo3Faction : byte
    {
        Red = 0,
        Blue = 1
    }

    public struct Demo3UnitConfig : IComponentData
    {
        public float Radius;
        public byte ParamSpeedIndex;
        public byte AnimationHitIndex;
        public byte AnimationDeathIndex;
        public Demo3AttackProfile Attacks;
    }

    public struct Demo3SpawnerTag : IComponentData
    {
    }

    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    public struct Demo3CombatData : IComponentData
    {
        public float Health;
        public float CurrentCooldown;
        public byte CurrentAttackProfileIndex;
        public bool HasDealtDamage;
        public Entity CurrentTarget;
        public int2 LockedHeatmapCell;
        public Demo3Faction Team;
        public byte UnityType;
    }

    public struct Demo3DeadData : IComponentData, IEnableableComponent
    {
        public float Progress;
    }

    public struct Demo3SpatialData
    {
        public Entity Entity;
        public float2 Position;
        public Demo3Faction Team;
    }

    public struct Demo3DamageMessage
    {
        public float Amount;
    }

    [MaterialProperty("_EmissionColor")]
    public struct Demo3MaterialEmissionColor : IComponentData
    {
        public float4 Value;
    }
}
