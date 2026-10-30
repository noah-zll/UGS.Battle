using UGS.Battle.Events;
using UGS.Battle.Sessions;
using UGS.Battle.Units;
using UnityEngine;

namespace UGS.Battle.Skills
{
    /// <summary>
    /// 技能状态机（纯逻辑，不持有状态）。冷却 / 充能 / 开关的推进与施法校验都在这里，
    /// 状态本身存在宿主的 <see cref="ISkillState"/> 上。
    /// </summary>
    /// <remarks>
    /// 调用方负责传入 <see cref="ISkillDefinition"/>、施法者与状态；包内不缓存这些引用。
    /// 冷却时长 = <see cref="ISkillDefinition.Cooldown"/> × <see cref="ISkillState.CooldownMultiplier"/>，
    /// 因此冷却缩减不会写回共享配置。
    /// </remarks>
    public static class SkillStateMachine
    {
        // ---------------- 冷却 ----------------

        public static void Tick(ISkillState state, ISkillDefinition definition, float deltaTime)
        {
            if (state == null || definition == null || deltaTime <= 0f) return;

            int max = MaxCharges(definition);
            float cooldown = EffectiveCooldown(state, definition);

            if (cooldown <= 0f)
            {
                if (state.Charges < max) state.SetCharges(max);
                state.SetCooldown(0f);
                return;
            }

            if (state.Charges >= max && state.CooldownRemaining <= 0f) return;

            float remaining = state.CooldownRemaining - deltaTime;
            if (remaining > 0f)
            {
                state.SetCooldown(remaining);
                return;
            }

            int charges = Mathf.Min(max, state.Charges + 1);
            state.SetCharges(charges);
            state.SetCooldown(charges < max ? cooldown : 0f);
        }

        public static void ResetCooldown(ISkillState state, ISkillDefinition definition)
        {
            if (state == null) return;

            state.SetCharges(MaxCharges(definition));
            state.SetCooldown(0f);
        }

        /// <summary>
        /// 主动进入冷却并消耗一次充能。
        /// </summary>
        /// <param name="seconds">&lt;0 表示使用有效冷却（配置冷却 × 实例倍率）。</param>
        public static void StartCooldown(ISkillState state, ISkillDefinition definition, float seconds = -1f)
        {
            if (state == null || definition == null) return;

            if (state.Charges > 0) state.SetCharges(state.Charges - 1);

            state.SetCooldown(seconds >= 0f ? seconds : EffectiveCooldown(state, definition));
        }

        /// <summary>直接设置剩余冷却（不改变充能），用于外部刷新/修正。</summary>
        public static void SetCooldown(ISkillState state, float remaining)
        {
            if (state == null) return;

            state.SetCooldown(Mathf.Max(0f, remaining));
        }

        /// <summary>直接设置开关状态（读档恢复），不触发任何事件。</summary>
        public static void SetToggled(ISkillState state, bool on)
        {
            state?.SetToggled(on);
        }

        public static float EffectiveCooldown(ISkillState state, ISkillDefinition definition)
        {
            if (definition == null || definition.Cooldown <= 0f) return 0f;

            float multiplier = state != null ? state.CooldownMultiplier : 1f;
            return definition.Cooldown * (multiplier > 0f ? multiplier : 1f);
        }

        public static int MaxCharges(ISkillDefinition definition)
        {
            return Mathf.Max(1, definition != null ? definition.MaxCharges : 1);
        }

        // ---------------- 校验 ----------------

        public static bool CanCast(ISkillState state, ISkillDefinition definition, ICombatUnit caster,
            out SkillCastFailure failure)
        {
            return CanCast(state, definition, caster, null, out failure);
        }

        public static bool CanCast(ISkillState state, ISkillDefinition definition, ICombatUnit caster,
            ICombatUnit target, out SkillCastFailure failure)
        {
            if (state == null || definition == null)
            {
                failure = SkillCastFailure.Dead;
                return false;
            }

            // 施法者可以为空：宿主已经按自己的规则完成存活 / 行动校验时，这里只校验状态与消耗。
            if (caster != null)
            {
                if (!caster.IsAlive)
                {
                    failure = SkillCastFailure.Dead;
                    return false;
                }

                if (!caster.CanAct)
                {
                    failure = SkillCastFailure.Silenced;
                    return false;
                }
            }

            if (state.Charges <= 0)
            {
                failure = state.CooldownRemaining > 0f ? SkillCastFailure.OnCooldown : SkillCastFailure.NoCharges;
                return false;
            }

            for (int i = 0; i < definition.Costs.Count; i++)
            {
                ResourceCost cost = definition.Costs[i];
                if (caster?.Resources == null ||
                    caster.Resources.GetCurrent(cost.ResourceAttributeId) < cost.Amount)
                {
                    failure = SkillCastFailure.NotEnoughResource;
                    return false;
                }
            }

            if (definition.Range > 0f && target != null && caster != null)
            {
                float sqr = (target.Position - caster.Position).sqrMagnitude;
                if (sqr > definition.Range * definition.Range)
                {
                    failure = SkillCastFailure.OutOfRange;
                    return false;
                }
            }

            failure = SkillCastFailure.None;
            return true;
        }

        // ---------------- 施法 ----------------

        public static SkillCastResult Cast(ISkillState state, ISkillDefinition definition, ICombatUnit caster,
            SkillCastContext context)
        {
            if (state == null || definition == null || context == null)
                return new SkillCastResult { Failure = SkillCastFailure.NoValidTarget };

            if (context.Caster == null) context.Caster = caster;
            if (context.Skill == null) context.Skill = definition;
            if (context.Session == null) context.Session = caster?.Session;

            if (!CanCast(state, definition, caster, context.Target, out SkillCastFailure failure))
            {
                return new SkillCastResult { Success = false, Failure = failure };
            }

            BattleSession session = context.Session;

            // 资源消耗：开关类技能的"开"与"关"都消耗（由定义里的 Costs 决定消耗什么）
            for (int i = 0; i < definition.Costs.Count; i++)
            {
                ResourceCost cost = definition.Costs[i];
                caster?.Resources?.Change(cost.ResourceAttributeId, -cost.Amount);
            }

            // 开关翻转先于冷却判定：OnDeactivate 需要知道本次是打开还是关闭
            bool toggled = false;
            if (definition.IsToggle())
            {
                state.SetToggled(!state.ToggledOn);
                toggled = true;
            }

            if (ShouldStartCooldown(state, definition))
            {
                state.SetCharges(state.Charges - 1);
                state.SetCooldown(EffectiveCooldown(state, definition));
            }

            // 目标筛选
            context.Targets.Clear();
            definition.Selector?.Select(context, context.Targets);

            // 动作执行
            for (int i = 0; i < definition.Actions.Count; i++)
            {
                definition.Actions[i]?.Execute(context);
            }

            context.ToggledOn = toggled;
            state.AddCastCount(1);

            session?.Events.Publish(new SkillCastEvent
            {
                Caster = caster,
                SkillId = definition.Id,
                ToggledOn = toggled,
            });

            return new SkillCastResult
            {
                Success = true,
                Failure = SkillCastFailure.None,
                TargetCount = context.Targets.Count,
            };
        }

        /// <summary>强制关闭开关类技能（例如被硬控/死亡时）。</summary>
        public static void ForceStop(ISkillState state)
        {
            if (state == null || !state.ToggledOn) return;

            state.SetToggled(false);
        }

        /// <summary>按 <see cref="ISkillDefinition.CooldownMode"/> 判定本次施法是否消耗充能并进冷却。</summary>
        private static bool ShouldStartCooldown(ISkillState state, ISkillDefinition definition)
        {
            switch (definition.CooldownMode)
            {
                case SkillCooldownMode.Manual:
                    return false;
                case SkillCooldownMode.OnDeactivate:
                    // 非开关类技能没有"关闭"这一刻，退回每次施法
                    return !definition.IsToggle() || !state.ToggledOn;
                default:
                    return true;
            }
        }
    }
}
