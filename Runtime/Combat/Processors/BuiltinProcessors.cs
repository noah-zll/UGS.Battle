using System.Collections.Generic;
using UGS.Battle.Effects;
using UGS.Battle.Effects.Behaviors;
using UGS.Battle.Units;
using UnityEngine;

namespace UGS.Battle.Combat.Processors
{
    /// <summary>命中判定。命中率由 <see cref="IDamagePayload.HitChance"/> 提供（&lt;0 跳过）。</summary>
    public sealed class AccuracyProcessor : IDamageProcessor
    {
        public int Order => 0;
        public bool Enabled { get; set; } = true;
        public string Name => "Accuracy";

        public void Process(DamageContext context)
        {
            if (!(context.Request.Payload is IDamagePayload payload)) return;

            float chance = payload.HitChance;
            if (chance < 0f || chance >= 1f) return;

            if (chance <= 0f || !context.Session.Random.Chance(chance))
            {
                context.Missed = true;
                context.Blocked = true;
                context.Amount = 0f;
                context.Halted = true;
                context.AddStep(Name, 0f, 0f, "Miss");
            }
        }
    }

    /// <summary>暴击判定。参数由 <see cref="IDamagePayload"/> 提供。</summary>
    public sealed class CriticalProcessor : IDamageProcessor
    {
        public int Order => 10;
        public bool Enabled { get; set; } = true;
        public string Name => "Critical";

        public void Process(DamageContext context)
        {
            if ((context.Request.Flags & DamageFlags.CannotCrit) != 0) return;
            if (!(context.Request.Payload is IDamagePayload payload)) return;

            float chance = payload.CriticalChance;
            if (chance <= 0f) return;

            if (context.Session.Random.Chance(chance))
            {
                float before = context.Amount;
                float multiplier = payload.CriticalMultiplier > 0f ? payload.CriticalMultiplier : 1f;

                context.Amount *= multiplier;
                context.Critical = true;
                context.AddStep(Name, before, context.Amount, $"x{multiplier:F2}");
            }
        }
    }

    /// <summary>通用减伤。减伤率由 <see cref="IDamagePayload.MitigationRate"/> 提供。</summary>
    public sealed class MitigationProcessor : IDamageProcessor
    {
        public int Order => 20;
        public bool Enabled { get; set; } = true;
        public string Name => "Mitigation";

        public void Process(DamageContext context)
        {
            if ((context.Request.Flags & DamageFlags.IgnoreMitigation) != 0) return;
            if (context.Request.Kind == DamageKind.True) return;
            if (!(context.Request.Payload is IDamagePayload payload)) return;

            float rate = Mathf.Clamp01(payload.MitigationRate);
            if (rate <= 0f) return;

            float before = context.Amount;
            context.Amount *= 1f - rate;
            context.AddStep(Name, before, context.Amount, $"rate={rate:F3}");
        }
    }

    /// <summary>护盾吸收。按效果上的 <see cref="IShieldProvider"/> 依次吸收。</summary>
    public sealed class ShieldAbsorbProcessor : IDamageProcessor
    {
        public int Order => 30;
        public bool Enabled { get; set; } = true;
        public string Name => "Shield";

        public void Process(DamageContext context)
        {
            if ((context.Request.Flags & DamageFlags.IgnoreShield) != 0) return;
            if (context.Amount <= 0f) return;

            ICombatUnit target = context.Request.Target;
            if (target == null) return;

            float totalAbsorbed = 0f;
            IReadOnlyList<EffectInstance> effects = target.Effects.Effects;

            for (int i = 0; i < effects.Count && context.Amount > 0f; i++)
            {
                EffectInstance instance = effects[i];
                if (instance.IsRemoved) continue;

                IEffectBehavior[] behaviors = instance.Behaviors;
                for (int b = 0; b < behaviors.Length && context.Amount > 0f; b++)
                {
                    if (!(behaviors[b] is IShieldProvider shield)) continue;
                    if (shield.Remaining <= 0f) continue;

                    float absorbed = shield.Absorb(context.Amount);
                    context.Amount -= absorbed;
                    totalAbsorbed += absorbed;
                }
            }

            if (totalAbsorbed <= 0f) return;

            context.Absorbed += totalAbsorbed;

            if (context.Amount <= 0f)
            {
                context.Amount = 0f;
                context.Blocked = true;
            }

            context.AddStep(Name, context.Amount + totalAbsorbed, context.Amount, $"absorb={totalAbsorbed:F1}");
        }
    }

    /// <summary>伤害上下限（由 <see cref="Sessions.BattleRules"/> 配置）。</summary>
    public sealed class ClampProcessor : IDamageProcessor
    {
        public int Order => 50;
        public bool Enabled { get; set; } = true;
        public string Name => "Clamp";

        public void Process(DamageContext context)
        {
            if (context.Amount <= 0f) return;

            Sessions.BattleRules rules = context.Session.Rules;
            float before = context.Amount;

            if (context.Amount < rules.MinDamage) context.Amount = rules.MinDamage;
            if (rules.MaxDamage > 0f && context.Amount > rules.MaxDamage) context.Amount = rules.MaxDamage;

            if (!Mathf.Approximately(before, context.Amount))
            {
                context.AddStep(Name, before, context.Amount);
            }
        }
    }

    /// <summary>
    /// 把最终伤害写入目标资源并询问存活状态。
    /// 包内**不判断**"血量是否归零"，只依赖 <see cref="ICombatUnit.IsAlive"/>。
    /// </summary>
    public sealed class ApplyDamageProcessor : IDamageProcessor
    {
        public int Order => 100;
        public bool Enabled { get; set; } = true;
        public string Name => "ApplyDamage";

        public void Process(DamageContext context)
        {
            ICombatUnit target = context.Request.Target;
            if (target == null || context.Missed || context.Amount <= 0f)
            {
                context.Halted = true;
                return;
            }

            float before = context.Amount;
            DamageResult result = context.ToResult();

            target.ApplyDamage(result);

            context.Killed = !target.IsAlive;
            context.Halted = true;
            context.AddStep(Name, before, before, context.Killed ? "Killed" : null);
        }
    }
}
