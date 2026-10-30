using System;
using UGS.Battle.Combat;
using UGS.Battle.Core;
using UGS.Battle.Effects;
using UGS.Battle.Events;
using UGS.Battle.Sessions;
using UGS.Battle.Units;

namespace UGS.Battle.Skills.Actions
{
    /// <summary>
    /// 伤害动作。基础数值来自 <see cref="AmountProvider"/>，未提供时取 <see cref="SkillCastContext.BaseAmount"/>。
    /// </summary>
    public sealed class DamageAction : ISkillAction
    {
        public float Multiplier = 1f;
        public DamageKind Kind = DamageKind.Physical;
        public DamageFlags Flags;

        public Func<SkillCastContext, ICombatUnit, float> AmountProvider;
        public Func<SkillCastContext, ICombatUnit, object> PayloadProvider;

        public void Execute(SkillCastContext context)
        {
            BattleSession session = context.Session;
            if (session == null) return;

            var targets = context.Targets;
            for (int i = 0; i < targets.Count; i++)
            {
                ICombatUnit target = targets[i];
                if (target == null || !target.IsAlive) continue;

                float baseAmount = AmountProvider != null
                    ? AmountProvider(context, target)
                    : context.BaseAmount;

                float amount = baseAmount * Multiplier;
                if (amount <= 0f) continue;

                session.Damage.Process(new DamageRequest
                {
                    Attacker = context.Caster,
                    Target = target,
                    Amount = amount,
                    Kind = Kind,
                    Flags = Flags,
                    SkillId = context.Skill != null ? context.Skill.Id : -1,
                    Payload = PayloadProvider != null ? PayloadProvider(context, target) : context.Payload,
                    SourceTag = context.Skill,
                }, session);
            }
        }
    }

    /// <summary>治疗动作。</summary>
    public sealed class HealAction : ISkillAction
    {
        public float Multiplier = 1f;

        public Func<SkillCastContext, ICombatUnit, float> AmountProvider;

        public void Execute(SkillCastContext context)
        {
            BattleSession session = context.Session;

            var targets = context.Targets;
            for (int i = 0; i < targets.Count; i++)
            {
                ICombatUnit target = targets[i];
                if (target == null || !target.IsAlive) continue;

                float baseAmount = AmountProvider != null
                    ? AmountProvider(context, target)
                    : context.BaseAmount;

                float amount = baseAmount * Multiplier;
                if (amount <= 0f) continue;

                target.ApplyHealing(amount);

                session?.Events.Publish(new UnitHealedEvent
                {
                    Unit = target,
                    Healer = context.Caster,
                    Amount = amount,
                });
            }
        }
    }

    /// <summary>施加效果动作。</summary>
    public sealed class ApplyEffectAction : ISkillAction
    {
        public IEffectDefinition Effect;
        public float? DurationOverride;
        public int Stacks = 1;

        public void Execute(SkillCastContext context)
        {
            if (Effect == null) return;

            var targets = context.Targets;
            for (int i = 0; i < targets.Count; i++)
            {
                ICombatUnit target = targets[i];
                if (target == null || !target.IsAlive) continue;

                EffectInstance instance = target.Effects.Apply(Effect, context.Caster, Stacks, DurationOverride);
                if (instance == null) continue;

                context.Session?.Events.Publish(new EffectAppliedEvent
                {
                    Unit = target,
                    Instance = instance,
                });
            }
        }
    }

    /// <summary>召唤动作。生成逻辑由上层 <see cref="IUnitSpawner"/> 实现。</summary>
    public sealed class SummonAction : ISkillAction
    {
        public string Key;
        public int Count = 1;

        public void Execute(SkillCastContext context)
        {
            BattleServiceRegistry services = context.Session != null
                ? context.Session.Services
                : BattleServiceRegistry.Default;

            IUnitSpawner spawner = services?.Spawner;
            if (spawner == null)
            {
                services?.Logger?.Warning("[UGS.Battle] summon action requires IUnitSpawner");
                return;
            }

            ICombatUnit caster = context.Caster;
            for (int i = 0; i < Count; i++)
            {
                spawner.Spawn(new SpawnRequest
                {
                    Key = Key,
                    OwnerUnitId = caster != null ? caster.UnitId : 0,
                    FactionId = caster != null ? caster.FactionId : 0,
                    Position = caster != null ? caster.Position : context.TargetPosition,
                    Payload = context.Payload,
                });
            }
        }
    }
}
