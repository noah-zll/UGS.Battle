using UGS.Battle.Units;
using UnityEngine;

namespace UGS.Battle.Skills
{
    /// <summary>
    /// 包内默认的 <see cref="ISkillState"/> 实现（测试、Samples、Editor 用）。
    /// 逻辑全部委托给 <see cref="SkillStateMachine"/>，本类只持字段。
    /// 由所属单位的会话负责 Tick（<see cref="Sessions.BattleSession.Skills"/>）。
    /// </summary>
    public sealed class SimpleSkillState : ISkillState
    {
        public SimpleSkillDefinition Definition { get; }

        public ICombatUnit Caster { get; private set; }

        public int Charges { get; private set; }

        public float CooldownRemaining { get; private set; }

        public bool ToggledOn { get; private set; }

        public int CastCount { get; private set; }

        public float CooldownMultiplier { get; private set; } = 1f;

        public int MaxCharges => SkillStateMachine.MaxCharges(Definition);

        public SimpleSkillState(SimpleSkillDefinition definition, ICombatUnit caster)
        {
            Definition = definition ?? throw new System.ArgumentNullException(nameof(definition));
            Caster = caster;
            Charges = MaxCharges;
        }

        // ---------------- 冷却 ----------------

        public void Tick(float deltaTime) => SkillStateMachine.Tick(this, Definition, deltaTime);

        public void ResetCooldown() => SkillStateMachine.ResetCooldown(this, Definition);

        /// <param name="seconds">&lt;0 表示使用有效冷却。</param>
        public void StartCooldown(float seconds = -1f) =>
            SkillStateMachine.StartCooldown(this, Definition, seconds);

        public void SetCooldown(float remaining) => SkillStateMachine.SetCooldown(this, remaining);

        public void SetToggled(bool on) => SkillStateMachine.SetToggled(this, on);

        public void SetCharges(int charges) => Charges = Mathf.Max(0, charges);

        public void AddCastCount(int delta) => CastCount += delta;

        public void SetCooldownMultiplier(float multiplier) =>
            CooldownMultiplier = multiplier > 0f ? multiplier : 1f;

        /// <summary>
        /// 替换施法者（召唤物代施、载具、心灵控制等场景）。
        /// 只影响存活/行动/资源/位置等校验，不重置冷却与开关状态。
        /// </summary>
        public void SetCaster(ICombatUnit caster)
        {
            if (caster != null) Caster = caster;
        }

        // ---------------- 校验与施法 ----------------

        public bool CanCast(out SkillCastFailure failure) =>
            SkillStateMachine.CanCast(this, Definition, Caster, out failure);

        public bool CanCast(ICombatUnit target, out SkillCastFailure failure) =>
            SkillStateMachine.CanCast(this, Definition, Caster, target, out failure);

        public SkillCastResult Cast(SkillCastContext context) =>
            SkillStateMachine.Cast(this, Definition, Caster, context);

        /// <summary>强制关闭开关类技能（例如被硬控/死亡时）。</summary>
        public void ForceStop() => SkillStateMachine.ForceStop(this);
    }
}
