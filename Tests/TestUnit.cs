using UGS.Battle.Attributes;
using UGS.Battle.Combat;
using UGS.Battle.Effects;
using UGS.Battle.Sessions;
using UGS.Battle.Units;
using UnityEngine;

namespace UGS.Battle.Tests
{
    /// <summary>测试用最小单位实现。</summary>
    public sealed class TestUnit : ICombatUnit
    {
        public int UnitId { get; }
        public string UnitName { get; set; }
        public int FactionId { get; set; }
        public int UnitTypeId { get; set; }
        public bool IsAlive { get; set; } = true;
        public bool CanAct => IsAlive;
        public bool CanBeTargeted => IsAlive;
        public Vector2 Position { get; set; }
        public float Radius { get; set; } = 0.5f;
        public AttributeContainer Attributes { get; }
        public EffectContainer Effects { get; }
        public ResourceContainer Resources { get; }
        public int PrimaryResourceAttributeId { get; set; }
        public BattleSession Session { get; private set; }

        public TestUnit(int unitId, int factionId, int primaryResourceAttributeId)
        {
            UnitId = unitId;
            FactionId = factionId;
            UnitName = "unit-" + unitId;
            PrimaryResourceAttributeId = primaryResourceAttributeId;

            Attributes = new AttributeContainer(this);
            Effects = new EffectContainer(this);
            Resources = new ResourceContainer(Attributes);
        }

        public void Bind(BattleSession session)
        {
            Session = session;
        }

        public void ApplyDamage(in DamageResult result)
        {
            Resources.Change(PrimaryResourceAttributeId, -result.FinalAmount);
            if (Resources.IsDepleted(PrimaryResourceAttributeId)) IsAlive = false;
        }

        public void ApplyHealing(float amount)
        {
            Resources.Change(PrimaryResourceAttributeId, amount);
        }

        public bool TryRedirectDamage(in DamageRequest request) => false;

        public void OnEffectApplied(EffectInstance instance) { }

        public void OnEffectRemoved(EffectInstance instance, EffectRemoveReason reason) { }
    }

    /// <summary>测试用伤害载荷。</summary>
    public sealed class TestPayload : IDamagePayload
    {
        public float Hit = 1f;
        public float Crit = 0f;
        public float CritMultiplier = 2f;
        public float Mitigation = 0f;

        public float HitChance => Hit;
        public float CriticalChance => Crit;
        public float CriticalMultiplier => CritMultiplier;
        public float MitigationRate => Mitigation;
    }
}
